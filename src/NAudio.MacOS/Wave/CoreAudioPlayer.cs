
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;

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
public sealed class CoreAudioPlayer : IWavePlayer, IWavePosition, IWaveLatency
{
    private AudioStream selectedStream;
    private readonly AudioDevice device;
    private IWaveProvider originalProvider;
    private bool isInvalid, resamplerAttached;
    private CoreAudioOutOnlyIOProc ioProcedure;
    private PropertyListenerHandle streamsChanged;
    private PropertyListenerHandle virtFormatChanged;
    private readonly PropertyListenerHandle deviceWillGoAway;
    // This function, if defined by the user, can use it
    // to modify the selection of the stream. By default,
    // the one completely matching the input format will
    // be used. If not found, falls back to the resampler and uses the first one it finds.
    // If the user cannot match the format he needs, he should return -1.
    // Note, however, that the resampler is always attached in such case.
    // This is provided because there are edge cases that
    // the driver could provide a better format that with
    // some resampling could produce much more quality results.
    // During my testing, I found out that custom specified formats are 
    // routed through a hilarious downsampling down to
    // 16000 kHz (even if i.e. specifying 52000 kHz), 
    // which it does have unpleasant playback effects.
    // Unless the audio which you want to play back is
    // in that frequency range, you generally want to have 
    // the optimal quality.
    private readonly Func<AudioStream[], int> streamSelector;

    /// <summary>
    /// Initializes a new Core Audio player instance that renders
    /// audio to the system's default output device. 
    /// </summary>
    public CoreAudioPlayer() : this(AudioSystemObject.Instance.DefaultOutputDevice) { }

    /// <summary>
    /// Initializes a new Core Audio player instance that renders audio
    /// to the specified Core Audio <see cref="AudioDevice"/> instance.
    /// </summary>
    /// <param name="dev">The audio device where the audio will be rendered to</param>
    /// <exception cref="ArgumentNullException"><paramref name="dev"/> is <see langword="null"/>.</exception>
    public CoreAudioPlayer(AudioDevice dev) : this(dev, null) { }

    /// <summary>
    /// Initializes a new Core Audio player instance that renders audio
    /// to the specified Core Audio <see cref="AudioDevice"/> instance,
    /// as well as a stream selector method reference, where the 
    /// audio will be rendered to. 
    /// </summary>
    /// <param name="dev">The audio device where the audio will be rendered to</param>
    /// <param name="streamSelector">The stream selector method reference that the player will use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dev"/> is <see langword="null"/>.</exception>
    public CoreAudioPlayer(AudioDevice dev, [MaybeNull] Func<AudioStream[], int> streamSelector)
    {
        ArgumentNullException.ThrowIfNull(dev);
        if (!dev.IsAlive)
        {
            throw new InvalidOperationException("This device will be shortly removed, and as such cannot be used as a capture device.");
        }
        device = dev;
        isInvalid = false;
        ioProcedure = null;
        selectedStream = null;
        resamplerAttached = false;
        this.streamSelector = streamSelector;
        deviceWillGoAway = device.ConstructIsAliveChangedEvent();
        deviceWillGoAway.Event += OnDeviceWillBeDestroyed;
    }

    #region Private support code

    private (IWaveProvider initProvider, int streamCount, int selectedStreamIndex) InitializePlayback()
    {
        MacUtils.EnsureDisposableObjectsDisposed(virtFormatChanged);
        IWaveProvider retProvider;
        AudioStream[] streams = device.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            throw new InvalidOperationException("Could not find any stream to render to");
        }
        // Try to match as close as possible the virtual format in the streams.
        // If we cannot do that, we attach the resampler on the first stream.
        int selectedStreamIndex = -1;
        Speakers requiredChannelMask = device.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output, out resamplerAttached, out var needs_ext);
        var sourceFormat = originalProvider.WaveFormat;
        if (!resamplerAttached)
        {
            if (streamSelector is not null)
            {
                selectedStreamIndex = streamSelector.Invoke(streams);
                // We will play safe here and assume that the user does 
                // whatever he wants - so we will attach the resampler.
                resamplerAttached = true;
            }
            else
            {
                int I = 0;
                foreach (var sm in streams)
                {
                    var vf = sm.VirtualFormat;
                    if (
                        (vf.SampleRate == sourceFormat.SampleRate) &&
                        vf.BlockAlign == sourceFormat.BlockAlign &&
                        vf.BitsPerSample == sourceFormat.BitsPerSample &&
                        vf.Channels == sourceFormat.Channels &&
                        vf.Encoding == sourceFormat.Encoding
                    )
                    {
                        // We have a possible format. Check speakers.
                        // If we do not have a value for it, we are OK and we can continue using it;
                        // otherwise, we need the resampler.
                        if (needs_ext || requiredChannelMask != Speakers.None)
                        {
                            // OK, we need to match against speakers if so we have
                            if (sourceFormat is WaveFormatExtensible ext && ext.ChannelMask == (int)requiredChannelMask)
                            {
                                selectedStreamIndex = I;
                                break;
                            }
                        }
                        else
                        {
                            selectedStreamIndex = I;
                            break;
                        }
                    }
                    I++;
                }
            }
        }
        if (selectedStreamIndex < 0)
        {
            // We couldn't find a suitable stream, we will inject the resampler to the first one.
            selectedStreamIndex = 0;
            resamplerAttached = true;
        }
        // OK, pick now the selected stream, register it to this instance
        selectedStream = streams[selectedStreamIndex];
        virtFormatChanged = selectedStream.ConstructVirtualFormatChangedEvent();
        virtFormatChanged.Event += OnStreamVirtualFormatChanged;
        if (resamplerAttached)
        {
            // We need to attach the resampler.
            retProvider = new MacAudioConverter(originalProvider, ConstructOutputFormat(out _));
        }
        else
        {
            retProvider = originalProvider;
        }
        return (retProvider, streams.Length, selectedStreamIndex);
    }

    private void InitIOProcedure(IWaveProvider provider, int streamCount, int selectedStreamIndex)
    {
        // OK, we are now ready to initialize the HAL.
        // Do it.
        try
        {
            ioProcedure = new(device);
            ioProcedure.Provider = provider;
            ioProcedure.PlaybackStopped += FirePlaybackStopped;
            // Now, enable only the stream we need.
            bool[] enabledStreams = new bool[streamCount];
            for (int I = 0; I < streamCount; I++)
            {
                enabledStreams[I] = I == selectedStreamIndex;
            }
            device.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Output, enabledStreams);
            // Finally, set up required events to listen to.
            // After that, we are ready for playback.
            streamsChanged = device.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Output);
            streamsChanged.Event += OnStreamsChanged;
        }
        catch
        {
            // Undo all of our progress so far if we do not made it.
            if (resamplerAttached)
            {
                ((MacAudioConverter)provider).Dispose();
            }
            throw;
        }
    }

    private void OnStreamsChanged(AudioObject _)
    {
        bool oldState = ioProcedure.IsRunning;
        ioProcedure.Stop();
        // Do not fire playback stopped event here.
        // Dispose the resampler, if we have one.
        if (resamplerAttached)
        {
            ((MacAudioConverter)ioProcedure.Provider).Dispose();
        }
        var (initProvider, streamCount, selectedStreamIndex) = InitializePlayback();
        ioProcedure.Provider = initProvider;
        bool[] enabledStreams = new bool[streamCount];
        for (int I = 0; I < streamCount; I++)
        {
            enabledStreams[I] = I == selectedStreamIndex;
        }
        device.SetStreamUsage(ioProcedure, AudioObjectPropertyScopeConstants.Output, enabledStreams);
        if (oldState) { ioProcedure.Start(); }
    }

    private void OnStreamVirtualFormatChanged(AudioObject _)
    {
        bool oldState = ioProcedure.IsRunning;
        ioProcedure.Stop();
        // Do not fire playback stopped event here.
        // Dispose the resampler, if we have one.
        if (resamplerAttached)
        {
            ((MacAudioConverter)ioProcedure.Provider).Dispose();
        }
        // We won't call the InitializePlayback method as it is not needed,
        // but we will inject the resampler to resample to the new format.
        var converter = new MacAudioConverter(originalProvider, selectedStream.VirtualFormat);
        try
        {
            resamplerAttached = true;
            ioProcedure.Provider = converter;
            if (oldState) { ioProcedure.Start(); }
        }
        catch
        {
            converter.Dispose();
            throw;
        }
    }

    private void OnDeviceWillBeDestroyed(AudioObject _)
    {
        isInvalid = true;
        // Remove stream.
        selectedStream = null;

        // If we have an I/O procedure attached, do the following.
        if (ioProcedure is not null && ioProcedure.IsRunning)
        {
            // Now, dispatch the playback stopped event with the event args containing
            // a Core Audio exception saying the device is no longer available.
            FirePlaybackStopped(null, new(
                new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError)
            ));
        }
    }

    private WaveFormat ConstructOutputFormat(out bool needsResampling)
    {
        var vf = selectedStream.VirtualFormat;
        Speakers spk = device.GetPreferredChannelLayout(AudioObjectPropertyScopeConstants.Output, out needsResampling, out var needsExtensible);
        if (needsExtensible)
        {
            return vf is WaveFormatExtensible ext
                ? new WaveFormatExtensible(
                    ext.SampleRate,
                    ext.BitsPerSample,
                    ext.Channels,
                    ext.SubFormat,
                    ext.ValidBitsPerSample,
                    spk
                )
                : new WaveFormatExtensible(
                    vf.SampleRate,
                    vf.BitsPerSample,
                    vf.Channels,
                    vf.Encoding == WaveFormatEncoding.IeeeFloat,
                    vf.BitsPerSample,
                    spk
                );
        }
        return vf;
    }

    private void FirePlaybackStopped(object sender, StoppedEventArgs e) => PlaybackStopped?.Invoke(this, e);

    [StackTraceHidden]
    [DebuggerStepThrough]
    private void ThrowIfInvalid()
    {
        if (isInvalid)
        {
            throw new CoreAudioException("The device used to perform I/O is now gone.", MacOS.CoreAudio.Interop.ErrorConstants.kAudioHardwareBadDeviceError);
        }
        else if (selectedStream is null)
        {
            throw new InvalidOperationException("Player not yet initialized!");
        }
    }

    #endregion

    #region Public surface

    /// <inheritdoc />
    public float Volume
    {
        get
        {
            ThrowIfInvalid();
            float max = 0f;
            foreach (var c in ioProcedure.Device.ControlList)
            {
                if (c is not null && c.Kind == AudioControlKind.VolumeControl)
                {
                    var (minimum, maximum) = ((AudioLevelControl)c).DecibelRange;
                    var v = ((AudioLevelControl)c).DecibelValue;
                    float vol = (float)((v - minimum) / (maximum - minimum));
                    if (vol > max) { max = vol; }
                }
            }
            return max;
        }
        set
        {
            ThrowIfInvalid();
            foreach (var c in ioProcedure.Device.ControlList)
            {
                if (c is not null && c.Kind == AudioControlKind.VolumeControl)
                {
                    ((AudioLevelControl)c).ScalarValue = value;
                }
            }
        }
    }

    /// <inheritdoc />
    public PlaybackState PlaybackState
    {
        get
        {
            ThrowIfInvalid();
            return ioProcedure.IsRunning ? PlaybackState.Playing : PlaybackState.Stopped;
        }
    }

    /// <summary>
    /// Gets the <see cref="WaveFormat"/> that is the virtual HAL format
    /// that the current instance uses to provide audio data to the HAL.
    /// </summary>
    public WaveFormat OutputWaveFormat
    {
        get
        {
            ThrowIfInvalid();
            return ConstructOutputFormat(out _);
        }
    }

    /// <inheritdoc />
    public TimeSpan AverageLatency
    {
        get
        {
            ThrowIfInvalid();
            double latencyFrames = device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output) + selectedStream.Latency;
            return TimeSpan.FromSeconds(latencyFrames / selectedStream.VirtualFormat.SampleRate);
        }
    }

    /// <inheritdoc />
    public TimeSpan CurrentLatency
    {
        get
        {
            ThrowIfInvalid();
            var vf = selectedStream.VirtualFormat;
            double timeInSeconds = ioProcedure.CurrentLatencyInSeconds(vf);
            if (timeInSeconds == -1d)
            {
                timeInSeconds =
                    (device.GetDeviceLatency(AudioObjectPropertyScopeConstants.Output)
                        + selectedStream.Latency) / (double)vf.SampleRate;
            }
            return TimeSpan.FromSeconds(timeInSeconds);
        }
    }

    /// <summary>
    /// Gets the selected audio device where the player renders data to.
    /// </summary>
    public AudioDevice Device => device;

    /// <inheritdoc />
    public long GetPosition()
    {
        ThrowIfInvalid();
        try
        {
            var vf = selectedStream.VirtualFormat;
            return (long)(CoreAudioFunctions.GetCurrentDeviceTime(device, vf) * vf.AverageBytesPerSecond);
        }
        catch (InvalidOperationException) // Device not running
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    /// <summary>
    /// Initializes this <see cref="CoreAudioPlayer"/>
    /// from the specified audio provider.
    /// </summary>
    /// <param name="waveProvider">The audio provider to initialize from.</param>
    /// <exception cref="InvalidOperationException">The instance has been successfully initialized before.</exception>
    public void Init(IWaveProvider waveProvider)
    {
        ArgumentNullException.ThrowIfNull(waveProvider);
        Monitor.Enter(this);
        try
        {
            if (originalProvider is not null)
            {
                throw new InvalidOperationException("This instance has already been initialized before.");
            }
            originalProvider = waveProvider;
            var p = InitializePlayback();
            InitIOProcedure(p.initProvider, p.streamCount, p.selectedStreamIndex);
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    /// <summary>Pause Playback</summary>
    /// <remarks>
    /// Note - there is not a 'pause' state in HAL;
    /// this is true due to it's I/O procedure model, where
    /// the HAL calls each procedure as required. As such,
    /// no special pause functionality exists, so this
    /// method hardwires to the <see cref="Stop"/>
    /// method.
    /// Also, there is not a <see cref="PlaybackState.Paused"/>
    /// state, instead only playing or stopped can be returned from
    /// the <see cref="PlaybackState"/> property.
    /// </remarks>
    public void Pause() => Stop();

    /// <inheritdoc />
    public void Play()
    {
        ThrowIfInvalid();
        ioProcedure.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        ThrowIfInvalid();
        ioProcedure.Stop();
    }

    /// <summary>
    /// Disposes this <see cref="CoreAudioPlayer"/> instance. <br />
    /// Thread-safe.
    /// </summary>
    public void Dispose()
    {
        Monitor.Enter(this);
        try
        {
            if (isInvalid) { return; }
            isInvalid = true;
            ioProcedure.Stop();
            MacUtils.EnsureDisposableObjectsDisposed(
                ioProcedure,
                streamsChanged,
                deviceWillGoAway,
                virtFormatChanged,
                resamplerAttached ? (MacAudioConverter)ioProcedure.Provider : null
            );
            ioProcedure = null;
            streamsChanged = null;
            selectedStream = null;
            virtFormatChanged = null;
        }
        finally
        {
            Monitor.Exit(this);
        }
    }

    #endregion
}