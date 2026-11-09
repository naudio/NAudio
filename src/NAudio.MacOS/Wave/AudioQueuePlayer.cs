
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS;
using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.CoreFoundationApi;
using NAudio.MacOS.AudioToolbox.Interop;

using static NAudio.MacOS.AudioToolbox.Interop.NativeMethods;

namespace NAudio.Wave;

/// <summary>
/// An audio player based on Audio Queue Services. Create it with <see cref="AudioQueuePlayerBuilder"/>. <br />
/// The queue keeps several buffers ahead of the device and refills them on its own thread, so a
/// garbage collection or a slow provider does not interrupt playback unless it takes longer than
/// the queued audio. The queue also converts the format of the provider to the format of the device.
/// </summary>
/// <seealso cref="AudioQueuePlayerBuilder"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueuePlayer : IWavePlayer, IWaveLatency, IWavePosition, IAsyncDisposable
{
    private readonly Lock lockObject;
    private readonly SynchronizationContext? syncContext;
    private readonly PropertyListenerHandle? deviceWillGoAway;
    private PlayerFlags flags;
    private PlaybackState state;
    private IWaveProvider? provider;
    private IntPtr queue;
    private GCHandle queueHandle;
    private IntPtr[] buffers;
    private int blockAlign;
    private int runId;
    private long submittedFrames;
    private long playedFrames;
    private float volume;
    // These are written under the lock or by the queue, and are read by the callbacks of the queue.
    private volatile bool stopping;
    private volatile bool sourceEnded;
    private volatile bool draining;
    private volatile Exception? failure;

    internal AudioQueuePlayer(
        int latencyMilliseconds,
        int numberOfBuffers,
        AudioDevice? device,
        bool levelMeteringEnabled,
        TimePitchAlgorithm timePitchAlgorithm,
        SynchronizationContext? syncContext
    )
    {
        VersioningVerifier.VerifyWeAreInSupportedVersion();
        if (device is not null && !device.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a playback device.");
        }

        LatencyMilliseconds = latencyMilliseconds;
        NumberOfBuffers = numberOfBuffers;
        Device = device;
        LevelMeteringEnabled = levelMeteringEnabled;
        TimePitchAlgorithm = timePitchAlgorithm;
        this.syncContext = syncContext;
        lockObject = new();
        state = PlaybackState.Stopped;
        buffers = [];
        volume = 1f;
        if (device is not null)
        {
            deviceWillGoAway = device.ConstructIsAliveChangedEvent();
            deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
        }
    }

    #region Internal support code

    /// <summary>
    /// Behavioral flags that affect the state of the player.
    /// </summary>
    [Flags]
    private enum PlayerFlags : byte
    {
        Disposed = 1 << 0,
        Initialized = 1 << 1,
        // The device has been removed.
        Invalidated = 1 << 2
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasFlagFast(PlayerFlags flag) => (flags & flag) != 0;

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalidOrDisposed()
    {
        ObjectDisposedException.ThrowIf(HasFlagFast(PlayerFlags.Disposed), this);
        if (HasFlagFast(PlayerFlags.Invalidated))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
        else if (!HasFlagFast(PlayerFlags.Initialized))
        {
            throw new InvalidOperationException("The player has not been initialized yet.");
        }
    }

    private void FirePlaybackStopped(Exception? exception)
    {
        var handler = PlaybackStopped;
        if (handler is null) { return; }
        var args = new StoppedEventArgs(exception);
        if (syncContext is null)
        {
            handler.Invoke(this, args);
        }
        else
        {
            syncContext.Post(_ => handler.Invoke(this, args), null);
        }
    }

    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        lockObject.Enter();
        try
        {
            flags |= PlayerFlags.Invalidated;
            if (state == PlaybackState.Stopped || HasFlagFast(PlayerFlags.Disposed)) { return; }
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        FirePlaybackStopped(new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError));
    }

    private unsafe void SetUInt32Property(AudioQueuePropertyID property, uint value)
        => AudioQueueException.ThrowIfError(AudioQueueSetProperty(queue, property, (IntPtr)(&value), sizeof(uint)));

    private unsafe void CreateQueue(WaveFormat format)
    {
        AudioStreamBasicDescription description = MacUtils.ConstructASBDFromWaveFormat(format);
        long frames = Math.Max(1L, format.SampleRate * (long)LatencyMilliseconds / (1000L * NumberOfBuffers));
        uint bufferBytes = checked((uint)(frames * format.BlockAlign));

        queueHandle = GCHandle.Alloc(this);
        AudioQueueException.ThrowIfError(AudioQueueNewOutput(in description, &OnBufferCompleted, GCHandle.ToIntPtr(queueHandle), IntPtr.Zero, IntPtr.Zero, 0, out queue));
        AudioQueueException.ThrowIfError(AudioQueueAddPropertyListener(queue, AudioQueueProperties.kAudioQueueProperty_IsRunning, &OnIsRunningChanged, GCHandle.ToIntPtr(queueHandle)));

        if (Device is not null)
        {
            using CFString uid = CFString.CreateFromString(Device.DeviceUID);
            IntPtr uidRef = uid.NativeObject;
            AudioQueueException.ThrowIfError(AudioQueueSetProperty(queue, AudioQueueProperties.kAudioQueueProperty_CurrentDevice, (IntPtr)(&uidRef), (uint)sizeof(IntPtr)));
        }

        if (format is WaveFormatExtensible extensible && extensible.ChannelMask != 0)
        {
            AudioChannelLayout layout = MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)extensible.ChannelMask);
            AudioQueueException.ThrowIfError(AudioQueueSetProperty(queue, AudioQueueProperties.kAudioQueueProperty_ChannelLayout, (IntPtr)(&layout), (uint)sizeof(AudioChannelLayout)));
        }

        if (LevelMeteringEnabled)
        {
            SetUInt32Property(AudioQueueProperties.kAudioQueueProperty_EnableLevelMetering, 1U);
        }

        // Both of these properties may only be set while the queue is stopped.
        if (TimePitchAlgorithm != TimePitchAlgorithm.Default)
        {
            SetUInt32Property(AudioQueueProperties.kAudioQueueProperty_EnableTimePitch, 1U);
            SetUInt32Property(AudioQueueProperties.kAudioQueueProperty_TimePitchAlgorithm, (uint)TimePitchAlgorithm);
        }

        AudioQueueException.ThrowIfError(AudioQueueSetParameter(queue, AudioQueueParameterID.kAudioQueueParam_Volume, volume));

        buffers = new IntPtr[NumberOfBuffers];
        for (int I = 0; I < buffers.Length; I++)
        {
            AudioQueueException.ThrowIfError(AudioQueueAllocateBuffer(queue, bufferBytes, out AudioQueueBuffer* buffer));
            buffers[I] = (IntPtr)buffer;
        }
    }

    private void DestroyQueue()
    {
        if (queue != IntPtr.Zero)
        {
            // An immediate disposal waits for a running callback and guarantees that no further ones occur.
            _ = AudioQueueDispose(queue, MacBoolean.True);
            queue = IntPtr.Zero;
        }

        if (queueHandle.IsAllocated) { queueHandle.Free(); }
    }

    /// <summary>
    /// Fills a buffer from the provider and hands it to the queue.
    /// </summary>
    /// <returns><see langword="true"/> when the buffer was enqueued.</returns>
    private unsafe bool FillAndEnqueue(AudioQueueBuffer* buffer)
    {
        int read;
        try
        {
            read = provider!.Read(new Span<byte>(buffer->mAudioData, (int)buffer->mAudioDataBytesCapacity));
            read = Math.Clamp(read, 0, (int)buffer->mAudioDataBytesCapacity);
            read -= read % blockAlign;
        }
        catch (Exception ex)
        {
            failure = ex;
            sourceEnded = true;
            return false;
        }

        if (read == 0)
        {
            sourceEnded = true;
            return false;
        }

        buffer->mAudioDataByteSize = (uint)read;
        _ = Interlocked.Add(ref submittedFrames, read / blockAlign);
        return AudioQueueEnqueueBuffer(queue, buffer, 0, null) == 0;
    }

    private void ScheduleCompletion()
        => _ = ThreadPool.UnsafeQueueUserWorkItem(static state => state.player.CompleteRun(state.run), (player: this, run: runId), false);

    // Neither of the callbacks may take the lock, because stopping or disposing the queue under it waits for them.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnBufferCompleted(IntPtr userData, IntPtr queue, AudioQueueBuffer* buffer)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueuePlayer player || player.stopping || player.sourceEnded) { return; }

        if (player.FillAndEnqueue(buffer) || !player.sourceEnded) { return; }

        if (player.failure is not null)
        {
            player.ScheduleCompletion();
        }
        else
        {
            // The buffers that are still queued play out, and the property listener reports the end.
            player.draining = true;
            _ = AudioQueueStop(queue, MacBoolean.False);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnIsRunningChanged(IntPtr userData, IntPtr queue, AudioQueuePropertyID propertyId)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueuePlayer player || !player.draining) { return; }

        uint isRunning = 1;
        uint size = sizeof(uint);
        if (AudioQueueGetProperty(queue, AudioQueueProperties.kAudioQueueProperty_IsRunning, (IntPtr)(&isRunning), ref size) == 0 && isRunning == 0)
        {
            player.ScheduleCompletion();
        }
    }

    private unsafe void UpdatePlayedFrames()
    {
        AudioTimeStamp stamp = default;
        if (queue != IntPtr.Zero &&
            AudioQueueGetCurrentTime(queue, IntPtr.Zero, &stamp, null) == 0 &&
            stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid)
        )
        {
            playedFrames = Math.Clamp((long)stamp.mSampleTime, playedFrames, Interlocked.Read(ref submittedFrames));
        }
    }

    /// <summary>
    /// Ends a playback that finished by itself or failed. Runs outside of the threads of the queue.
    /// </summary>
    private void CompleteRun(int run)
    {
        Exception? exception;
        lockObject.Enter();
        try
        {
            if (run != runId || state == PlaybackState.Stopped || HasFlagFast(PlayerFlags.Disposed)) { return; }

            exception = failure;
            UpdatePlayedFrames();
            if (exception is null) { playedFrames = Interlocked.Read(ref submittedFrames); }
            StopQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        FirePlaybackStopped(exception);
    }

    private void StopQueue()
    {
        stopping = true;
        _ = AudioQueueStop(queue, MacBoolean.True);
        state = PlaybackState.Stopped;
    }

    private AudioDevice GetEffectiveDevice()
        => Device ?? AudioSystemObject.Instance.DefaultOutputDevice ?? throw new InvalidOperationException("No default output device is available.");

    private double GetDeviceLatencyMilliseconds()
    {
        AudioDevice device = GetEffectiveDevice();
        return 1000d * device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output) / device.NominalSampleRate;
    }

    #endregion

    #region Public API

    /// <summary>The total amount of audio queued ahead of the device, in milliseconds.</summary>
    public int LatencyMilliseconds { get; }

    /// <summary>The number of buffers that the latency is divided into.</summary>
    public int NumberOfBuffers { get; }

    /// <summary>The device to play to, or <see langword="null"/> when following the default output device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>Whether level metering is enabled.</summary>
    public bool LevelMeteringEnabled { get; }

    /// <summary>The time-pitch algorithm of the queue.</summary>
    public TimePitchAlgorithm TimePitchAlgorithm { get; }

    /// <summary>
    /// The gain of this player, from 0 to 1. It is applied by the queue
    /// and does not modify the volume of the device.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 0, or more than 1.</exception>
    public float Volume
    {
        get => volume;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            lockObject.Enter();
            try
            {
                ObjectDisposedException.ThrowIf(HasFlagFast(PlayerFlags.Disposed), this);
                if (queue != IntPtr.Zero)
                {
                    AudioQueueException.ThrowIfError(AudioQueueSetParameter(queue, AudioQueueParameterID.kAudioQueueParam_Volume, value));
                }
                volume = value;
            }
            finally
            {
                lockObject.Exit();
            }
        }
    }

    /// <inheritdoc />
    public PlaybackState PlaybackState => state;

    /// <summary>The format passed to the queue, which is the format of the provider.</summary>
    public WaveFormat OutputWaveFormat
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return provider!.WaveFormat;
        }
    }

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimeSpan.FromMilliseconds(GetDeviceLatencyMilliseconds() + LatencyMilliseconds);
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            lockObject.Enter();
            try
            {
                UpdatePlayedFrames();
                long queuedFrames = Interlocked.Read(ref submittedFrames) - playedFrames;
                return TimeSpan.FromMilliseconds(GetDeviceLatencyMilliseconds() + (1000d * queuedFrames / provider!.WaveFormat.SampleRate));
            }
            finally
            {
                lockObject.Exit();
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    /// <inheritdoc />
    public long GetPosition()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            UpdatePlayedFrames();
            return playedFrames * blockAlign;
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <summary>Initializes this player from the specified audio provider.</summary>
    /// <param name="waveProvider">The audio provider to initialize from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="waveProvider"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The instance has been successfully initialized before.</exception>
    public void Init(IWaveProvider waveProvider)
    {
        ArgumentNullException.ThrowIfNull(waveProvider);
        lockObject.Enter();
        try
        {
            ObjectDisposedException.ThrowIf(HasFlagFast(PlayerFlags.Disposed), this);
            if (HasFlagFast(PlayerFlags.Initialized))
            {
                throw new InvalidOperationException("This instance has already been initialized before.");
            }

            try
            {
                provider = waveProvider;
                blockAlign = waveProvider.WaveFormat.BlockAlign;
                CreateQueue(waveProvider.WaveFormat);
                flags |= PlayerFlags.Initialized;
            }
            catch
            {
                DestroyQueue();
                throw;
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <inheritdoc />
    public unsafe void Play()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state == PlaybackState.Playing) { return; }

            if (state == PlaybackState.Paused)
            {
                AudioQueueException.ThrowIfError(AudioQueueStart(queue, null));
                state = PlaybackState.Playing;
                return;
            }

            // This is a new playback. Nothing can be in a callback here because the queue is stopped.
            runId++;
            stopping = false;
            sourceEnded = false;
            draining = false;
            failure = null;
            playedFrames = 0;
            _ = Interlocked.Exchange(ref submittedFrames, 0);

            int enqueued = 0;
            foreach (IntPtr buffer in buffers)
            {
                if (!FillAndEnqueue((AudioQueueBuffer*)buffer)) { break; }
                enqueued++;
            }

            state = PlaybackState.Playing;

            if (enqueued == 0 || failure is not null)
            {
                ScheduleCompletion();
                return;
            }

            int status = AudioQueueStart(queue, null);
            if (status != 0)
            {
                StopQueue();
                AudioQueueException.ThrowIfError(status);
            }

            if (sourceEnded)
            {
                draining = true;
                _ = AudioQueueStop(queue, MacBoolean.False);
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state != PlaybackState.Playing) { return; }
            AudioQueueException.ThrowIfError(AudioQueuePause(queue));
            state = PlaybackState.Paused;
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state == PlaybackState.Stopped) { return; }
            StopQueue();
            playedFrames = 0;
            _ = Interlocked.Exchange(ref submittedFrames, 0);
        }
        finally
        {
            lockObject.Exit();
        }

        FirePlaybackStopped(null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        bool wasPlaying;
        lockObject.Enter();
        try
        {
            if (HasFlagFast(PlayerFlags.Disposed)) { return; }
            flags |= PlayerFlags.Disposed;
            wasPlaying = state != PlaybackState.Stopped;
            stopping = true;
            state = PlaybackState.Stopped;
            DestroyQueue();
        }
        finally
        {
            lockObject.Exit();
        }

        // The listener is removed outside of the lock, because
        // removing it waits for a running notification, which itself takes the lock.
        deviceWillGoAway?.Dispose();
        if (wasPlaying) { FirePlaybackStopped(null); }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(Task.Run(Dispose));

    #endregion
}
