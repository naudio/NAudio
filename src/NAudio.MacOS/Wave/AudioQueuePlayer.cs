
using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.AudioToolbox.Interop;

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
public sealed class AudioQueuePlayer : AudioQueueBase, IWavePlayer, IWaveLatency, IWavePosition
{
    private int runId;
    // Volume value kept before the player is initialized.
    // After the player is initialized, the volume is kept
    // updated with the queue's volume control.
    private float volume;
    private int blockAlign;
    private long playedFrames;
    private PlayerFlags flags;
    private PlaybackState state;
    private long submittedFrames;
    private IWaveProvider? provider;
    private GCHandle thisQueueHandle;
    private readonly Lock lockObject;
    private readonly IntPtr[] buffers;
    private readonly SynchronizationContext? syncContext;
    private readonly PropertyListenerHandle? deviceWillGoAway;

    // The below four fields are written under the lock or by the queue, and are read by the callbacks of the queue.
    private volatile bool stopping;
    private volatile bool draining;
    private volatile bool sourceEnded;
    private volatile Exception? failure;

    internal AudioQueuePlayer(
        int latencyMilliseconds,
        int numberOfBuffers,
        AudioDevice? device,
        TimePitchAlgorithm? timePitchAlgorithm,
        SynchronizationContext? syncContext
    )
    {
        if (device is not null && !device.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a playback device.");
        }

        volume = 1f;
        Device = device;
        state = PlaybackState.Stopped;
        this.syncContext = syncContext;
        TimePitchAlgorithm = timePitchAlgorithm;
        LatencyMilliseconds = latencyMilliseconds;

        lockObject = new();
        buffers = new IntPtr[numberOfBuffers];

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

    private unsafe void CreateQueue(WaveFormat format)
    {
        thisQueueHandle = GCHandle.Alloc(this);

        AudioQueueException.ThrowIfError(
            NativeMethods.AudioQueueNewOutput(
                MacUtils.ConstructASBDFromWaveFormat(format),
                &OnBufferCompleted,
                GCHandle.ToIntPtr(thisQueueHandle),
                IntPtr.Zero,
                IntPtr.Zero,
                0U,
                out queueHandle
            )
        );

        RegisterPropertyListener(
            AudioQueueProperties.kAudioQueueProperty_IsRunning,
            &OnIsRunningChanged,
            GCHandle.ToIntPtr(thisQueueHandle)
        );

        long frames = Math.Max(1L, format.SampleRate * (long)LatencyMilliseconds / (TimeSpan.MillisecondsPerSecond * NumberOfBuffers));
        int bufferBytes = checked((int)(frames * format.BlockAlign));

        if (Device is not null)
        {
            SetStringProperty(AudioQueueProperties.kAudioQueueProperty_CurrentDevice, Device.DeviceUID);
        }

        if (format is WaveFormatExtensible extensible && extensible.ChannelMask != 0)
        {
            SetChannelLayoutProperty(
                AudioQueueProperties.kAudioQueueProperty_ChannelLayout,
                MacUtils.ConstructAudioChannelLayoutFromSpeakers((Speakers)extensible.ChannelMask)
            );
        }

        // Both of these properties may only be set while the queue is stopped.
        if (TimePitchAlgorithm is not null)
        {
            SetUIntProperty(AudioQueueProperties.kAudioQueueProperty_EnableTimePitch, 1U);
            SetUIntProperty(AudioQueueProperties.kAudioQueueProperty_TimePitchAlgorithm, (uint)TimePitchAlgorithm);
        }

        SetParameter(AudioQueueParameterID.kAudioQueueParam_Volume, volume);

        for (int I = 0; I < buffers.Length; I++)
        {
            buffers[I] = (IntPtr)AllocateBuffer(bufferBytes);
        }
    }

    /// <summary>
    /// Fills a buffer from the provider and hands it to the queue.
    /// </summary>
    /// <returns><see langword="true"/> when the buffer was enqueued.</returns>
    private unsafe bool FillAndEnqueue(AudioQueueBuffer* buffer)
    {
        uint read;
        try
        {
            read = Math.Clamp(
                (uint)provider!.Read(buffer->GetSpanToWriteBuffer()),
                0U,
                buffer->mAudioDataBytesCapacity
            );
            // The queue can handle non-block-aligned audio data.
            // read -= read % (uint)blockAlign;
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

        buffer->mAudioDataByteSize = read;
        _ = Interlocked.Add(ref submittedFrames, read / blockAlign);
        try
        {
            EnqueueBuffer(buffer);
            return true;
        }
        catch
        {
            return false;
        }
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
            try { player.Stop(false); } catch { }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnIsRunningChanged(IntPtr userData, IntPtr queue, AudioQueuePropertyID propertyId)
    {
        if (GCHandle.FromIntPtr(userData).Target is not AudioQueuePlayer player || !player.draining) { return; }

        uint isRunning = 1;
        uint size = sizeof(uint);
        if (NativeMethods.AudioQueueGetProperty(
                queue,
                AudioQueueProperties.kAudioQueueProperty_IsRunning,
                (IntPtr)(&isRunning),
                ref size
            ) == 0 && isRunning == 0)
        {
            player.ScheduleCompletion();
        }
    }

    private void UpdatePlayedFrames()
    {
        AudioTimeStamp stamp;
        if (queueHandle != IntPtr.Zero &&
            ObtainAudioTimeStamp(out stamp)
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
        try { Stop(true); } catch { }
        state = PlaybackState.Stopped;
    }

    private AudioDevice GetEffectiveDevice()
        => Device ?? AudioSystemObject.Instance.DefaultOutputDevice ?? throw new InvalidOperationException("No default output device is available.");

    private double GetDeviceLatencyMilliseconds()
    {
        AudioDevice device = GetEffectiveDevice();
        return TimeSpan.MillisecondsPerSecond * device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output) / device.NominalSampleRate;
    }

    #endregion

    #region Public API

    /// <summary>The total amount of audio queued ahead of the device, in milliseconds.</summary>
    public int LatencyMilliseconds { get; }

    /// <summary>The number of buffers that the latency is divided into.</summary>
    public int NumberOfBuffers => buffers.Length;

    /// <summary>The device to play to, or <see langword="null"/> when following the default output device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>The time-pitch algorithm of the queue. Returns <see langword="null"/> if the time-pitch feature is disabled.</summary>
    public TimePitchAlgorithm? TimePitchAlgorithm { get; }

    /// <summary>
    /// The gain of this player, from 0 to 1. It is applied by the queue
    /// and does not modify the volume of the device.
    /// </summary>
    /// <remarks>
    /// This property is only effective after the queue has been successfully
    /// initialized.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 0, or more than 1.</exception>
    public float Volume
    {
        get => HasFlagFast(PlayerFlags.Initialized)
                ? queueHandle == IntPtr.Zero ? 0f : QueryParameter(AudioQueueParameterID.kAudioQueueParam_Volume)
                : volume;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            lockObject.Enter();
            try
            {
                ObjectDisposedException.ThrowIf(HasFlagFast(PlayerFlags.Disposed), this);
                if (HasFlagFast(PlayerFlags.Initialized))
                {
                    if (queueHandle != IntPtr.Zero)
                    {
                        SetParameter(AudioQueueParameterID.kAudioQueueParam_Volume, value);
                    }
                }
                else
                {
                    volume = value;
                }
            }
            finally
            {
                lockObject.Exit();
            }
        }
    }

    /// <summary>
    /// A value ranging from 0.5 to 2.0 indicating the rate that the audio plays. <br />
    /// Only applicable when the <see cref="TimePitchAlgorithm"/> property is not <see langword="null"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">During setting the property: The value is less than 0.5, or more than 2.0 .</exception>
    /// <exception cref="InvalidOperationException">
    /// The time/pitch processor is inactive for this audio queue instance. <br /> <br />
    ///
    /// -or- <br /> <br />
    ///
    /// The player has not yet been initialized.
    /// </exception>
    [SupportedOSPlatform("macos10.6")]
    public float PlayRate
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimePitchAlgorithm is null
                ? throw new InvalidOperationException("The time/pitch processor has not been enabled!")
                : QueryParameter(AudioQueueParameterID.kAudioQueueParam_PlayRate);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.5f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2.0f);
            if (TimePitchAlgorithm is null)
            {
                throw new InvalidOperationException("The time/pitch processor has not been enabled!");
            }
            else
            {
                SetParameter(AudioQueueParameterID.kAudioQueueParam_PlayRate, value);
            }
        }
    }

    /// <summary>
    /// A value ranging from -2400 to 2400 indicating the number of cents to pitch-shift the queue's playback
    /// (1200 cents is one octave).
    /// </summary>
    /// <remarks>
    /// Note that this requires the time-pitch algorithm to have been set to
    /// <see cref="TimePitchAlgorithmConstants.Spectral"/> during building.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">During setting the property: The value is less than -2400, or more than 2400 .</exception>
    /// <exception cref="InvalidOperationException">
    /// The time/pitch processor is inactive for this audio queue instance. <br /> <br />
    ///
    /// -or- <br /> <br />
    ///
    /// The time/pitch processor was not initialized with the <see cref="TimePitchAlgorithmConstants.Spectral"/> algorithm. <br /> <br />
    /// 
    /// -or- <br /> <br />
    /// 
    /// The player has not yet been initialized.
    /// </exception>
    [SupportedOSPlatform("macos10.6")]
    public float Pitch
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimePitchAlgorithm == TimePitchAlgorithmConstants.Spectral
                ? QueryParameter(AudioQueueParameterID.kAudioQueueParam_Pitch)
                : throw new InvalidOperationException("The time/pitch processor has not been initialized with the spectral algorithm!");
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -2400f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2400f);
            if (TimePitchAlgorithm == TimePitchAlgorithmConstants.Spectral)
            {
                SetParameter(AudioQueueParameterID.kAudioQueueParam_Pitch, value);
            }
            else
            {
                throw new InvalidOperationException("The time/pitch processor has not been enabled!");
            }
        }
    }

    /// <summary>
    /// A value from -1 to 1 indicating the pan position of a mono source (-1 = hard left, 0 = center, 1 = hard right). <br />
    /// For a stereo source this parameter affects left/right balance. <br />
    /// For multi-channel sources, this parameter has no effect.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">During setting the property: The value is less than -1.0, or more than 1.0 .</exception>
    /// <exception cref="InvalidOperationException">The player has not yet been initialized.</exception>
    public float Pan
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return QueryParameter(AudioQueueParameterID.kAudioQueueParam_Pan);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1f);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);
            ThrowIfInvalidOrDisposed();
            SetParameter(AudioQueueParameterID.kAudioQueueParam_Pan, value);
        }
    }

    /// <summary>
    /// A value of type <see cref="TimeSpan"/> over which subsequent volume changes will be ramped. <br />
    /// For example, to fade out from full unity gain to silence over the course of 1
    /// second, set the <see cref="VolumeRampTime"/> property to 1 second then set 
    /// the <see cref="Volume"/> property to 0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">During setting the property: The represented time is a negative value.</exception>
    /// <exception cref="InvalidOperationException">The player has not yet been initialized.</exception>
    public TimeSpan VolumeRampTime
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimeSpan.FromSeconds(QueryParameter(AudioQueueParameterID.kAudioQueueParam_VolumeRampTime));
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.Ticks);
            ThrowIfInvalidOrDisposed();
            SetParameter(AudioQueueParameterID.kAudioQueueParam_VolumeRampTime, (float)value.TotalSeconds);
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
                return TimeSpan.FromMilliseconds(GetDeviceLatencyMilliseconds() + (TimeSpan.MillisecondsPerSecond * queuedFrames / provider!.WaveFormat.SampleRate));
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
                DisposeInternal(true);
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
                Start();
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

            try
            {
                Start();
            }
            catch
            {
                StopQueue();
                throw;
            }

            if (sourceEnded)
            {
                draining = true;
                try { Stop(false); } catch { }
            }
        }
        finally
        {
            lockObject.Exit();
        }
    }

    /// <inheritdoc />
    public new void Pause()
    {
        ThrowIfInvalidOrDisposed();
        lockObject.Enter();
        try
        {
            if (state != PlaybackState.Playing) { return; }
            base.Pause();
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

    #endregion

    private void DisposeInternal(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing && thisQueueHandle.IsAllocated) { thisQueueHandle.Free(); }
        }
    }

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2215:Dispose methods should call base class dispose",
        Justification = "Called via DisposeInternal"
    )]
    protected override void Dispose(bool disposing)
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
            DisposeInternal(disposing);
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
}
