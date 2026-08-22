
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Wave;

/// <summary>
/// Provides an audio player based on the Apple's Audio HAL framework library, 
/// namely the Core Audio Framework. <br />
/// The user provides the audio device to perform playback upon, 
/// the audio provider to do playback for, and the rest are managed by this class.
/// This class does also manage cases where the streams may be invalidated,
/// or when the selected stream's virtual format has been changed. <br />
/// There are also several properties that can be configured by the provided <see cref="AudioDevice"/>
/// object during construction (applying to all the attached players of the device).
/// See the <see cref="AudioDevice"/> properties and methods to see what can be configured.
/// </summary>
/// <seealso cref="MacOS.CoreAudio"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed partial class CoreAudioPlayer : IWavePlayer, IWaveLatency, IWavePosition, IAsyncDisposable
{
    private readonly object lockObject;
    private PlayerProcedure ioProcedure;
    private IPlayerSource selectedSource;
    private IWaveProvider originalProvider;
    private CoreAudioPlayerStateFlags flags;
    /// <summary>
    /// The <see cref="AudioDevice"/> where audio data are rendered to.
    /// </summary>
    private readonly AudioDevice selectedDevice;
    private PropertyListenerHandle streamsChanged;
    private readonly SynchronizationContext syncContext;
    private readonly PropertyListenerHandle deviceWillGoAway;

    /// <summary>
    /// Initializes a new Core Audio player instance that renders
    /// audio to the system's default output device. 
    /// </summary>
    public CoreAudioPlayer() : this(AudioSystemObject.Instance.DefaultOutputDevice) { }

    /// <summary>
    /// Initializes a new Core Audio player instance that renders audio
    /// to the specified Core Audio <see cref="AudioDevice"/> instance. <br />
    /// If a syncrhonization context is assigned for the thread where this instance
    /// is created to, it is used during playback stopped events.
    /// </summary>
    /// <param name="device">The audio device where the audio will be rendered to</param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public CoreAudioPlayer(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a capture device.");
        }
        lockObject = new();
        selectedSource = null;
        originalProvider = null;
        selectedDevice = device;
        syncContext = SynchronizationContext.Current;
        deviceWillGoAway = device.ConstructIsAliveChangedEvent();
        deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
    }

    #region Internal support code

    // Provides required data and methods
    // for directly interacting with Core Audio
    // I/O procedures.
    private interface IPlayerSource : IDisposable
    {
        // Reads from the source.
        int Read(Span<byte> halBuffer);

        // The format of the audio that will be written from the source provider during reads.
        AudioStreamBasicDescription ReinterpretedFormat { get; }

        // Reported stream latency computed during the player's initialization.
        uint StreamsLatency { get; }
    }

    /// <summary>
    /// Behavioral flags that affect the player's state.
    /// </summary>
    [Flags]
    private enum CoreAudioPlayerStateFlags : byte
    {
        Disposed = 1 << 0,
        Initialized = 1 << 1,
        // When the device has been removed.
        Invalidated = 1 << 2,
        NonInterleaved = 1 << 3
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

        public static void UnwrapContextAndDispatch(object state)
            => ((SyncContextStoppedEventData)state).PostEvent();
    }

    private void FirePlaybackStopped(StoppedEventArgs e) => FirePlaybackStopped(null, e);

    // Fires the PlaybackStopped event.
    // If required, the SynchronizationContext is unwrapped
    // and the event dispatch happens on the context instead.
    private void FirePlaybackStopped(object sender, StoppedEventArgs e)
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

    // After full initialization of the procedure, 
    // implementers must call this to finalize initialization
    // and register any crucial playback events.
    private void OnInitializationComplete()
    {
        streamsChanged?.Dispose();
        streamsChanged = selectedDevice.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Output);
        streamsChanged.Event += OnStreamsChanged;
        flags |= CoreAudioPlayerStateFlags.Initialized;
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
            foreach (var c in selectedDevice.ControlList)
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
                foreach (var c in selectedDevice.ControlList)
                {
                    if (c is AudioLevelControl lc && lc.Kind == AudioControlKind.VolumeControl)
                    {
                        lc.ScalarValue = value;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Note that this property can only
    /// return <see cref="PlaybackState.Stopped"/> or
    /// <see cref="PlaybackState.Playing"/> states;
    /// See remarks section in <see cref="Pause"/>
    /// to get an explanation why this happens.
    /// </remarks>
    public PlaybackState PlaybackState => ioProcedure is null ?
        PlaybackState.Stopped :
        ioProcedure.IsRunning ? PlaybackState.Playing : PlaybackState.Stopped;

    /// <summary>
    /// Gets the <see cref="WaveFormat"/> that is the virtual HAL format
    /// that the current instance uses to provide audio data to the HAL.
    /// </summary>
    public WaveFormat OutputWaveFormat
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            return MacUtils.ConstructWaveFormatFromASBD(selectedSource.ReinterpretedFormat);
        }
    }

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            double latencyFrames = selectedDevice.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output) + selectedSource.StreamsLatency;
            return TimeSpan.FromSeconds(latencyFrames / selectedSource.ReinterpretedFormat.mSampleRate);
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalidOrDisposed();
            var timeInSeconds = ioProcedure.CurrentLatencyInSeconds();
            return timeInSeconds == -1d ? AverageLatency : TimeSpan.FromSeconds(timeInSeconds);
        }
    }

    /// <inheritdoc />
    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    /// <summary>
    /// Gets the selected audio device where the player renders data to.
    /// </summary>
    [NotNull]
    public AudioDevice Device => selectedDevice;

    /// <inheritdoc />
    public long GetPosition()
    {
        // The below function call is not required; it is implicitly 
        // handled by the call of OutputWaveFormat property.
        // ThrowIfInvalidOrDisposed();
        var streamFormat = OutputWaveFormat;
        try
        {
            long position = (long)(CoreAudioFunctions.GetCurrentDeviceTime(selectedDevice, streamFormat) * streamFormat.AverageBytesPerSecond);
            long drift = position % streamFormat.BlockAlign;
            if (drift > 0L) { position += streamFormat.BlockAlign - drift; }
            return position;
        }
        catch (InvalidOperationException) // Device not running
        {
            return 0L;
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
        Monitor.Enter(lockObject);
        try
        {
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Initialized))
            {
                throw new InvalidOperationException("This instance has already been initialized before.");
            }
            originalProvider = waveProvider;
            Initialize(true);
            // The below exception is thrown if the we forget to
            // call the OnInitializationComplete method inside Initialize().
            if (!HasStateFlagFast(CoreAudioPlayerStateFlags.Initialized))
            {
                throw new InvalidOperationException("The player instance was not initialized successfully!");
            }
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
    }

    /// <summary>Pause Playback</summary>
    /// <remarks>
    /// Note - there is not a 'pause' state in HAL;
    /// this is true due to it's I/O procedure model, where
    /// the HAL calls each procedure as required. As such,
    /// no special pause functionality exists, so this
    /// method hardwires to the <see cref="Stop()"/>
    /// method.
    /// Also, there is not a <see cref="PlaybackState.Paused"/>
    /// state, instead only playing or stopped can be returned from
    /// the <see cref="PlaybackState"/> property.
    /// </remarks>
    public void Pause()
    {
        ThrowIfInvalidOrDisposed();
        ioProcedure.Stop();
    }

    /// <inheritdoc />
    public void Play()
    {
        ThrowIfInvalidOrDisposed();
        ioProcedure.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfInvalidOrDisposed();
        // One PlaybackStopped per playback, matching the rest of NAudio: WaveOut
        // guards the same way, and AsioOut goes further and suppresses the event
        // its Pause would otherwise raise. Without this, the ordinary
        // Play(); ... Stop(); shape raises the event twice whenever the source
        // reached its end first, and every further Stop() raises another.
        if (PlaybackState == PlaybackState.Stopped) { return; }
        ioProcedure.Stop();
        FirePlaybackStopped(new());
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioPlayer"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    /// <remarks>
    /// Note that this method will probably throw when the device
    /// used to initialize the player is gone; so, expect to catch
    /// <see cref="AggregateException"/> for this particular case.
    /// </remarks>
    /// <exception cref="AggregateException">One or more native objects were failed to be disposed of.</exception>
    public void Dispose()
    {
        Monitor.Enter(lockObject);
        try
        {
            if (HasStateFlagFast(CoreAudioPlayerStateFlags.Disposed)) { return; }
            // Make sure that no other threads modify the disposal state.
            flags |= CoreAudioPlayerStateFlags.Disposed;
            // It has to be noted that if the player is destroyed after
            // the device was removed, there is no need to dispatch
            // a playback stopped event.
            if ((!HasStateFlagFast(CoreAudioPlayerStateFlags.Invalidated)) && ioProcedure is not null && ioProcedure.IsRunning)
            {
                FirePlaybackStopped(new());
            }
            // Dispose of any objects we can dispose of
            MacUtils.EnsureDisposableObjectsDisposed(
                deviceWillGoAway,
                ioProcedure,
                selectedSource,
                streamsChanged,
                virtFormatChanged
            );

            ioProcedure = null;
            streamsChanged = null;
            selectedSource = null;
            virtFormatChanged = null;
        }
        finally
        {
            Monitor.Exit(lockObject);
        }
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioPlayer"/> instance asynchronously. <br />
    /// Disposal is thread-safe, because the returned task puts <see cref="Dispose()"/> in the thread pool.
    /// </summary>
    public ValueTask DisposeAsync() => new(Task.Run(new Action(Dispose)));

    #endregion
}