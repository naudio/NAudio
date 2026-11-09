
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using NAudio.MacOS;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.AudioToolbox.Interop;

using static NAudio.MacOS.AudioToolbox.Interop.NativeMethods;

namespace NAudio.Wave;

/// <summary>
/// Provides an audio player based on Apple's Audio Queue Services. <br />
/// The queue keeps several buffers ahead of the device and refills them on its own
/// thread, so a garbage collection or a slow provider does not interrupt playback
/// unless it takes longer than the queued audio. The queue also converts the
/// provider's format to the format of the device. <br />
/// Use <see cref="DesiredLatency"/> to trade latency against the risk of dropouts.
/// </summary>
/// <seealso cref="MacOS.CoreAudio"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed partial class CoreAudioPlayer : IWavePlayer, IWaveLatency, IWavePosition, IAsyncDisposable
{
    private readonly Lock lockObject;
    private IWaveProvider? originalProvider;
    private CoreAudioPlayerStateFlags flags;
    // The AudioDevice where audio data are rendered to.
    private readonly AudioDevice selectedDevice;
    // The default-device constructor leaves routing to the queue, which follows the system default.
    private readonly bool followsDefaultDevice;
    private readonly SynchronizationContext? syncContext;
    private readonly PropertyListenerHandle? deviceWillGoAway;
    private IntPtr queue;
    private GCHandle queueHandle;
    private IntPtr[] buffers = [];
    private int blockAlign;
    private int runId;
    private long submittedFrames;
    private long playedFrames;
    private PlaybackState state;
    // Written under lockObject or by the queue, read by the queue's callbacks.
    private volatile bool stopping;
    private volatile bool sourceEnded;
    private volatile bool draining;
    private volatile Exception? failure;

    /// <summary>
    /// Initializes a new Core Audio player instance that renders
    /// audio to the system's default output device. <br />
    /// If a synchronization context is assigned for the thread where this instance
    /// is created to, it is used when a playback stopped event is dispatching. <br />
    /// Playback follows the system's default output device when it changes.
    /// </summary>
    public CoreAudioPlayer() : this(AudioSystemObject.Instance.DefaultOutputDevice ?? throw new InvalidOperationException("No default output device is available."), true) { }

    /// <summary>
    /// Initializes a new Core Audio player instance that renders audio
    /// to the specified Core Audio <see cref="AudioDevice"/> instance. <br />
    /// If a synchronization context is assigned for the thread where this instance
    /// is created to, it is used when a playback stopped event is dispatching.
    /// </summary>
    /// <param name="device">The audio device where the audio will be rendered to</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public CoreAudioPlayer(AudioDevice device) : this(device, false) { }

    private CoreAudioPlayer(AudioDevice device, bool followsDefaultDevice)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a playback device.");
        }
        lockObject = new();
        originalProvider = null;
        selectedDevice = device;
        this.followsDefaultDevice = followsDefaultDevice;
        syncContext = SynchronizationContext.Current;
        if (!followsDefaultDevice)
        {
            deviceWillGoAway = device.ConstructIsAliveChangedEvent();
            deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
        }
    }

    #region Internal support code

    /// <summary>
    /// Behavioral flags that affect the player's state.
    /// </summary>
    [Flags]
    private enum CoreAudioPlayerStateFlags : byte
    {
        Disposed = 1 << 0,
        Initialized = 1 << 1,
        // When the device has been removed.
        Invalidated = 1 << 2
    }

    /// <summary>
    /// Provides the data required to invoke the
    /// playback stopped event from a SynchronizationContext.
    /// </summary>
    /// <param name="Player">The player to pass to the handler as the 'sender'</param>
    /// <param name="Handler">The handler to invoke.</param>
    /// <param name="Args">The StoppedEventArgs that describe the playback state.</param>
    private record SyncContextStoppedEventData(
        CoreAudioPlayer Player,
        EventHandler<StoppedEventArgs> Handler,
        StoppedEventArgs Args
    )
    {
        private void PostEvent() => Handler.Invoke(Player, Args);

        public static void UnwrapContextAndDispatch(object? state)
            => ((SyncContextStoppedEventData)state!).PostEvent();
    }

    private void FirePlaybackStopped(StoppedEventArgs e) => FirePlaybackStopped(null, e);

    // Fires the PlaybackStopped event.
    // If required, the SynchronizationContext is unwrapped
    // and the event dispatch happens on the context instead.
    private void FirePlaybackStopped(object? sender, StoppedEventArgs e)
    {
        var handler = PlaybackStopped;
        if (handler is null) { return; }
        if (syncContext is null)
        {
            handler.Invoke(this, e);
        }
        else
        {
            syncContext.Post(
                new(SyncContextStoppedEventData.UnwrapContextAndDispatch),
                new SyncContextStoppedEventData(this, handler, e)
            );
        }
    }

    private void OnDeviceWillBeDestroyed(AudioObject audioDevice)
    {
        flags |= CoreAudioPlayerStateFlags.Invalidated;

        lock (lockObject)
        {
            if (state == PlaybackState.Stopped || HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed))
            {
                return;
            }

            StopQueue();
        }

        FirePlaybackStopped(new(
            new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError)
        ));
    }

    // Tests whether a specified state flag is defined.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasStateFlagFast(CoreAudioPlayerStateFlags flag) => (flags & flag) != 0;

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalidOrDisposed()
    {
        ObjectDisposedException.ThrowIf(HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed), this);
        if (HasStateFlagFast(CoreAudioPlayerStateFlags.Invalidated))
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
        else if (!HasStateFlagFast(CoreAudioPlayerStateFlags.Initialized))
        {
            throw new InvalidOperationException("Player not yet initialized!");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Modifies the volume of the selected playback device <br />
    /// To individually modify channels, obtain the control list of the
    /// device and modify the value for each of it's volume controls.
    /// </summary>
    /// <remarks>
    /// Note that modifying the volume of the audio device can be in
    /// general disruptive as the user sets this to a desired value.
    /// If you want to modify the gain of the audio provider that 
    /// you use to provide data to the player without modifying this, 
    /// inject a <see cref="SampleProviders.VolumeSampleProvider"/>. <br /> <br />
    /// 
    /// When retrieveing the value of this property, the value is deduced
    /// by iterating through the device's volume controls and getting 
    /// the volume value. It has been observed that in some cases 
    /// the HAL fails to properly update the properties of the device
    /// and may return 1 while that is not true.
    /// However, assigning this property does the intended result
    /// (of changing the device's volume) without any error(s).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When setting the property: The selected volume is less than 0,
    /// or more than 1.
    /// </exception>
    public float Volume
    {
        get
        {
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Invalidated))
            {
                throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
            }
            // Note - getting the AudioLevelControl's ScalarValue
            // is a very attractive option, however getting it
            // returns a stale value of 1, which does not 
            // work for us for getting the gain value,
            // so we use DecibelValue and min-max it 
            // by each control's decibel range.
            // Finally, we test the produced value
            // whether is less than the current presumed volume,
            // which if it is, is replaced.
            float volume = 1f;
            foreach (var c in Device.ControlList)
            {
                if (c is AudioLevelControl lc && lc.Kind == AudioControlKind.VolumeControl)
                {
                    var (minimum, maximum) = lc.DecibelRange;
                    float vol = (float)((lc.DecibelValue - minimum) / (maximum - minimum));
                    if (vol < volume) { volume = vol; }
                }
            }
            return volume;
        }
        set
        {
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Invalidated))
            {
                throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
            }
            else if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Desired volume value cannot be less than 0 and more than 1.");
            }
            else if (HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed))
            {
                // Do not modify the volume if this object was disposed of
                return;
            }
            else
            {
                // Note that setting the scalar value here actually
                // works; it does the intended result.
                foreach (var c in Device.ControlList)
                {
                    if (c is AudioLevelControl lc && lc.Kind == AudioControlKind.VolumeControl)
                    {
                        lc.ScalarValue = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// The total amount of audio queued ahead of the device, in milliseconds.
    /// Larger values tolerate longer pauses of the runtime or the provider.
    /// Read when <see cref="Init"/> is called. The default is 100.
    /// </summary>
    public int DesiredLatency { get; set; } = 100;

    /// <summary>
    /// The number of buffers that <see cref="DesiredLatency"/> is divided into.
    /// Read when <see cref="Init"/> is called. The default is 4.
    /// </summary>
    public int NumberOfBuffers { get; set; } = 4;

    /// <inheritdoc />
    public PlaybackState PlaybackState => state;

    /// <summary>
    /// Gets the <see cref="WaveFormat"/> that the player passes to the Audio Queue,
    /// which is the format of the provider.
    /// </summary>
    public WaveFormat OutputWaveFormat
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return originalProvider!.WaveFormat;
        }
    }

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return TimeSpan.FromMilliseconds(DeviceLatencyMilliseconds + DesiredLatency);
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            lock (lockObject)
            {
                UpdatePlayedFrames();
                long queuedFrames = Interlocked.Read(ref submittedFrames) - playedFrames;
                return TimeSpan.FromMilliseconds(DeviceLatencyMilliseconds + (1000d * queuedFrames / originalProvider!.WaveFormat.SampleRate));
            }
        }
    }

    private double DeviceLatencyMilliseconds =>
        1000d * Device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output) / Device.NominalSampleRate;

    /// <inheritdoc />
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    /// <summary>
    /// Gets the audio device where the player renders data to.
    /// With the parameterless constructor this is the current default output device.
    /// </summary>
    public AudioDevice Device => followsDefaultDevice ? AudioSystemObject.Instance.DefaultOutputDevice ?? selectedDevice : selectedDevice;

    /// <inheritdoc />
    public long GetPosition()
    {
        ThrowIfInvalidOrDisposed();
        lock (lockObject)
        {
            UpdatePlayedFrames();
            return playedFrames * blockAlign;
        }
    }

    /// <summary>
    /// Initializes this <see cref="CoreAudioPlayer"/>
    /// from the specified audio provider.
    /// </summary>
    /// <param name="waveProvider">The audio provider to initialize from.</param>
    /// <exception cref="InvalidOperationException">The instance has been successfully initialized before.</exception>
    public void Init(IWaveProvider waveProvider)
    {
        ArgumentNullException.ThrowIfNull(waveProvider);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(DesiredLatency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(NumberOfBuffers);
        lock (lockObject)
        {
            ObjectDisposedException.ThrowIf(HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed), this);
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Initialized))
            {
                throw new InvalidOperationException("This instance has already been initialized before.");
            }

            try
            {
                originalProvider = waveProvider;
                blockAlign = waveProvider.WaveFormat.BlockAlign;
                CreateQueue(waveProvider.WaveFormat);
                flags |= CoreAudioPlayerStateFlags.Initialized;
            }
            catch
            {
                DestroyQueue();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        ThrowIfInvalidOrDisposed();
        lock (lockObject)
        {
            if (state == PlaybackState.Playing)
            {
                ThrowIfQueueError(AudioQueuePause(queue), "AudioQueuePause");
                state = PlaybackState.Paused;
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Play()
    {
        ThrowIfInvalidOrDisposed();
        lock (lockObject)
        {
            if (state == PlaybackState.Playing) { return; }

            if (state == PlaybackState.Paused)
            {
                ThrowIfQueueError(AudioQueueStart(queue, null), "AudioQueueStart");
                state = PlaybackState.Playing;
                return;
            }

            // A new run. Nothing can be in a callback here because the queue is stopped.
            runId++;
            stopping = false;
            sourceEnded = false;
            draining = false;
            failure = null;
            submittedFrames = 0;
            playedFrames = 0;

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
                ThrowIfQueueError(status, "AudioQueueStart");
            }

            if (sourceEnded)
            {
                draining = true;
                _ = AudioQueueStop(queue, MacBoolean.False);
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfInvalidOrDisposed();
        lock (lockObject)
        {
            // One PlaybackStopped per playback, matching the rest of NAudio.
            if (state == PlaybackState.Stopped) { return; }
            StopQueue();
            playedFrames = 0;
            Interlocked.Exchange(ref submittedFrames, 0);
        }
        FirePlaybackStopped(new());
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioPlayer"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        bool wasPlaying;
        lock (lockObject)
        {
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed)) { return; }
            flags |= CoreAudioPlayerStateFlags.Disposed;
            wasPlaying = state != PlaybackState.Stopped && !HasStateFlagFast(CoreAudioPlayerStateFlags.Invalidated);
            stopping = true;
            state = PlaybackState.Stopped;
            DestroyQueue();
            deviceWillGoAway?.Dispose();
        }

        if (wasPlaying)
        {
            FirePlaybackStopped(new());
        }
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioPlayer"/> instance asynchronously. <br />
    /// Disposal is thread-safe, because the returned task puts <see cref="Dispose()"/> in the thread pool.
    /// </summary>
    public ValueTask DisposeAsync() => new(Task.Run(new Action(Dispose)));

    #endregion
}
