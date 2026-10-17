using System;
using NAudio.Wave.Asio;
using System.Threading;

namespace NAudio.Wave;

/// <summary>
/// ASIO Out Player — the original NAudio 2.x entry point for ASIO playback, recording, and duplex.
/// Implemented in NAudio 3 as a thin facade over <see cref="AsioDevice"/>: lifecycle, capabilities,
/// drain-on-dispose, and synchronization-context dispatch all flow through that class, while AsioOut
/// keeps its raw <c>IntPtr</c>-based <see cref="AudioAvailable"/> contract for back-compat consumers.
/// </summary>
/// <remarks>
/// <para>
/// New code should target <see cref="AsioDevice"/> directly. It exposes per-channel <c>Span&lt;float&gt;</c>
/// for recording and duplex, an explicit <c>Init*</c> mode selection, non-contiguous channel-array routing,
/// and a supported <see cref="AsioDevice.Reinitialize"/> path for handling
/// <see cref="AsioDevice.DriverResetRequest"/>.
/// </para>
/// <para>
/// Legacy translations:
/// <list type="bullet">
///   <item><description><see cref="Init"/> → <see cref="AsioDevice.InitPlayback"/></description></item>
///   <item><description><see cref="InitRecordAndPlayback"/> with a non-null provider → <see cref="AsioDevice.InitDuplex"/></description></item>
///   <item><description><see cref="InitRecordAndPlayback"/> with a null provider → <see cref="AsioDevice.InitRecording"/></description></item>
/// </list>
/// </para>
/// </remarks>
public class AsioOut : IWavePlayer, IWaveLatency
{
    private AsioDevice? device;
    private IWaveProvider? sourceStream;
    private PlaybackState playbackState;
    private int nbSamples;
    private byte[]? waveBuffer;
    private AsioSampleConvertor.SampleConvertor? convertor;
    private WaveFormat? outputWaveFormat;
    private bool isInitialized;

    private AsioDevice Device => device ?? throw new ObjectDisposedException(nameof(AsioOut));
    // Pause performs a silent driver stop. We forward AsioDevice.Stopped as PlaybackStopped, but Pause
    // historically did not raise PlaybackStopped — this flag lets the next Stopped event bypass the forward.
    private bool suppressNextStopped;

    /// <summary>
    /// Playback Stopped
    /// </summary>
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    /// <summary>
    /// When recording, fires whenever recorded audio is available
    /// </summary>
    [Obsolete("Use AsioDevice.InitRecording or AsioDevice.InitDuplex instead (see Docs/AsioMigration.md). AsioOut's recording support will be removed in a future major version.")]
    public event EventHandler<AsioAudioAvailableEventArgs>? AudioAvailable;

    /// <summary>
    /// Occurs when the driver settings are changed by the user, e.g. in the control panel.
    /// </summary>
    public event EventHandler? DriverResetRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsioOut"/> class with the first
    /// available ASIO Driver.
    /// </summary>
    public AsioOut()
        : this(0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsioOut"/> class with the driver name.
    /// </summary>
    /// <param name="driverName">Name of the Device.</param>
    public AsioOut(string driverName)
    {
        device = AsioDevice.Open(driverName);
        WireDeviceEvents();
    }

    /// <summary>
    /// Opens an ASIO output device
    /// </summary>
    /// <param name="driverIndex">Device number (zero based)</param>
    public AsioOut(int driverIndex)
    {
        string[] names = GetDriverNames();
        if (names.Length == 0)
        {
            throw new ArgumentException("There is no ASIO Driver installed on your system");
        }
        if (driverIndex < 0 || driverIndex >= names.Length)
        {
            throw new ArgumentException($"Invalid device number. Must be in the range [0,{names.Length - 1}]");
        }
        device = AsioDevice.Open(names[driverIndex]);
        WireDeviceEvents();
    }

    /// <summary>
    /// Releases unmanaged resources and performs other cleanup operations before the
    /// <see cref="AsioOut"/> is reclaimed by garbage collection.
    /// </summary>
    ~AsioOut()
    {
        Dispose();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        device?.Dispose();
        device = null;
    }

    /// <summary>
    /// Gets the names of the installed ASIO Driver.
    /// </summary>
    /// <returns>an array of driver names</returns>
    public static string[] GetDriverNames() => AsioDevice.GetDriverNames();

    /// <summary>
    /// Determines whether ASIO is supported.
    /// </summary>
    /// <returns>
    ///     <c>true</c> if ASIO is supported; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSupported() => GetDriverNames().Length > 0;

    /// <summary>
    /// Determines whether ASIO is supported.
    /// </summary>
    [Obsolete("Use IsSupported() instead. isSupported() will be removed in a future major version.")]
    public static bool isSupported() => IsSupported();

    /// <summary>
    /// Determines whether this driver supports the specified sample rate.
    /// </summary>
    /// <param name="sampleRate">The samplerate to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified sample rate is supported otherwise, <c>false</c>.
    /// </returns>
    public bool IsSampleRateSupported(int sampleRate) => Device.IsSampleRateSupported(sampleRate);

    private void WireDeviceEvents()
    {
        Device.Stopped += OnDeviceStopped;
        Device.DriverResetRequest += (_, _) => DriverResetRequest?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeviceStopped(object? sender, StoppedEventArgs e)
    {
        playbackState = PlaybackState.Stopped;
        HasReachedEnd = false;
        if (suppressNextStopped)
        {
            suppressNextStopped = false;
            return;
        }
        PlaybackStopped?.Invoke(this, e);
    }

    /// <summary>
    /// Shows the control panel
    /// </summary>
    public void ShowControlPanel() => Device.ShowControlPanel();

    /// <summary>
    /// Starts playback
    /// </summary>
    public void Play()
    {
        if (playbackState != PlaybackState.Playing)
        {
            playbackState = PlaybackState.Playing;
            HasReachedEnd = false;
            Device.Start();
        }
    }

    /// <summary>
    /// Stops playback
    /// </summary>
    public void Stop()
    {
        playbackState = PlaybackState.Stopped;
        Device.Stop();
        // OnDeviceStopped clears HasReachedEnd and forwards PlaybackStopped via the captured SynchronizationContext.
    }

    /// <summary>
    /// Pauses playback
    /// </summary>
    public void Pause()
    {
        playbackState = PlaybackState.Paused;
        // Pause historically did not raise PlaybackStopped — swallow the next Stopped that this triggers.
        suppressNextStopped = true;
        Device.Stop();
    }

    /// <summary>
    /// Initialises to play
    /// </summary>
    /// <param name="waveProvider">Source audio source</param>
    public void Init(IWaveProvider waveProvider)
    {
        InitCore(waveProvider, 0, -1);
    }

    /// <summary>
    /// Initialises to play, with optional recording
    /// </summary>
    /// <param name="waveProvider">Source audio source - set to null for record only</param>
    /// <param name="recordChannels">Number of channels to record</param>
    /// <param name="recordOnlySampleRate">Specify sample rate here if only recording, ignored otherwise</param>
    [Obsolete("Use AsioDevice.InitRecording (record only) or AsioDevice.InitDuplex (record and play) instead (see Docs/AsioMigration.md). AsioOut's recording support will be removed in a future major version.")]
    public void InitRecordAndPlayback(IWaveProvider? waveProvider, int recordChannels, int recordOnlySampleRate)
    {
        InitCore(waveProvider, recordChannels, recordOnlySampleRate);
    }

    private void InitCore(IWaveProvider? waveProvider, int recordChannels, int recordOnlySampleRate)
    {
        if (isInitialized)
        {
            throw new InvalidOperationException("Already initialised this instance of AsioOut - dispose and create a new one");
        }
        isInitialized = true;
        int desiredSampleRate = waveProvider != null ? waveProvider.WaveFormat.SampleRate : recordOnlySampleRate;
        var underlyingDriver = Device.UnderlyingDriver;

        if (waveProvider != null)
        {
            sourceStream = waveProvider;

            this.NumberOfOutputChannels = waveProvider.WaveFormat.Channels;

            // Select the correct sample convertor from WaveFormat -> ASIOFormat
            var asioSampleType = underlyingDriver.Capabilities.OutputChannelInfos[0].type;
            convertor = AsioSampleConvertor.SelectSampleConvertor(waveProvider.WaveFormat, asioSampleType);

            switch (asioSampleType)
            {
                case AsioSampleType.Float32LSB:
                    outputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(waveProvider.WaveFormat.SampleRate, waveProvider.WaveFormat.Channels);
                    break;
                case AsioSampleType.Int32LSB:
                    outputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 32, waveProvider.WaveFormat.Channels);
                    break;
                case AsioSampleType.Int16LSB:
                    outputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 16, waveProvider.WaveFormat.Channels);
                    break;
                case AsioSampleType.Int24LSB:
                    outputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 24, waveProvider.WaveFormat.Channels);
                    break;
                default:
                    throw new NotSupportedException($"{asioSampleType} not currently supported");
            }
        }
        else
        {
            this.NumberOfOutputChannels = 0;
        }

        if (!underlyingDriver.IsSampleRateSupported(desiredSampleRate))
        {
            throw new ArgumentException("SampleRate is not supported");
        }
        if (underlyingDriver.Capabilities.SampleRate != desiredSampleRate)
        {
            underlyingDriver.SetSampleRate(desiredSampleRate);
        }

        this.NumberOfInputChannels = recordChannels;
        // Use preferred ASIO buffer size.
        nbSamples = underlyingDriver.CreateBuffers(NumberOfOutputChannels, NumberOfInputChannels, false);
        underlyingDriver.SetChannelOffset(ChannelOffset, InputChannelOffset); // throws if offset+count exceeds channel count

        if (waveProvider != null)
        {
            // make a buffer big enough to read enough from the sourceStream to fill the ASIO buffers
            waveBuffer = new byte[nbSamples * NumberOfOutputChannels * waveProvider.WaveFormat.BitsPerSample / 8];
        }

        Device.ConfigureLegacyRawCallback(driver_BufferUpdate);
    }

    /// <summary>
    /// driver buffer update callback to fill the wave buffer.
    /// </summary>
    /// <param name="inputChannels">The input channels.</param>
    /// <param name="outputChannels">The output channels.</param>
    private void driver_BufferUpdate(IntPtr[] inputChannels, IntPtr[] outputChannels)
    {
        if (this.NumberOfInputChannels > 0)
        {
#pragma warning disable CS0618 // the legacy recording path is obsolete but still supported until it's removed
            var audioAvailable = AudioAvailable;
            if (audioAvailable != null)
            {
                var args = new AsioAudioAvailableEventArgs(inputChannels, outputChannels, nbSamples,
                                                           Device.UnderlyingDriver.Capabilities.InputChannelInfos[0].type);
                audioAvailable(this, args);
                if (args.WrittenToOutputBuffers)
                    return;
            }
#pragma warning restore CS0618
        }

        // sourceStream, waveBuffer and convertor are all set whenever there are output channels
        var source = sourceStream;
        var buffer = waveBuffer;
        var sampleConvertor = convertor;
        if (this.NumberOfOutputChannels > 0 && source != null && buffer != null && sampleConvertor != null)
        {
            int read = source.Read(buffer.AsSpan());
            if (read < buffer.Length)
            {
                // we have reached the end of the input data - clear out the end
                Array.Clear(buffer, read, buffer.Length - read);
            }

            // Call the convertor
            unsafe
            {
                fixed (void* pBuffer = &buffer[0])
                {
                    sampleConvertor(new IntPtr(pBuffer), outputChannels, NumberOfOutputChannels, nbSamples);
                }
            }

            if (read == 0)
            {
                HasReachedEnd = true;
                if (AutoStop)
                {
                    // Phase 0 finding F1 — never call Stop() from inside the buffer-switch callback.
                    // AsioDevice.Stop would throw InvalidOperationException here; defer to a worker thread.
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try { Stop(); } catch { /* device may already be disposed */ }
                    });
                }
            }
        }
    }

    /// <summary>
    /// Gets the latency (in samples) of the playback driver
    /// </summary>
    public int PlaybackLatency => Device.OutputLatencySamples;

    /// <summary>
    /// Automatically stop when the end of the input stream is reached
    /// Disable this if auto-stop is causing hanging issues
    /// </summary>
    public bool AutoStop { get; set; }

    /// <summary>
    /// A flag to let you know that we have reached the end of the input file
    /// Useful if AutoStop is set to false
    /// You can monitor this yourself and call Stop when it is true
    /// </summary>
    public bool HasReachedEnd { get; private set; }

    /// <summary>
    /// Playback State
    /// </summary>
    public PlaybackState PlaybackState => playbackState;

    /// <summary>
    /// Driver Name
    /// </summary>
    public string DriverName => Device.DriverName;

    /// <summary>
    /// The number of output channels we are currently using for playback
    /// (Must be less than or equal to DriverOutputChannelCount)
    /// </summary>
    public int NumberOfOutputChannels { get; private set; }

    /// <summary>
    /// The number of input channels we are currently recording from
    /// (Must be less than or equal to DriverInputChannelCount)
    /// </summary>
    public int NumberOfInputChannels { get; private set; }

    /// <summary>
    /// The maximum number of input channels this ASIO driver supports
    /// </summary>
    public int DriverInputChannelCount => Device.Capabilities.NbInputChannels;

    /// <summary>
    /// The maximum number of output channels this ASIO driver supports
    /// </summary>
    public int DriverOutputChannelCount => Device.Capabilities.NbOutputChannels;

    /// <summary>
    /// The number of samples per channel, per buffer.
    /// </summary>
    public int FramesPerBuffer
    {
        get
        {
            if (!isInitialized)
                throw new InvalidOperationException("Not initialized yet. Call this after calling Init");

            return nbSamples;
        }
    }

    /// <summary>
    /// By default the first channel on the input WaveProvider is sent to the first ASIO output.
    /// This option sends it to the specified channel number.
    /// Warning: make sure you don't set it higher than the number of available output channels -
    /// the number of source channels.
    /// n.b. Future NAudio may modify this
    /// </summary>
    public int ChannelOffset { get; set; }

    /// <summary>
    /// Input channel offset (used when recording), allowing you to choose to record from just one
    /// specific input rather than them all
    /// </summary>
    public int InputChannelOffset { get; set; }

    /// <summary>
    /// Sets the volume (1.0 is unity gain)
    /// Not supported for ASIO Out. Set the volume on the input stream instead
    /// </summary>
    [Obsolete("this function will be removed in a future NAudio as ASIO does not support setting the volume on the device")]
    public float Volume
    {
        get
        {
            return 1.0f;
        }
        set
        {
            if (value != 1.0f)
            {
                throw new InvalidOperationException("AsioOut does not support setting the device volume");
            }
        }
    }

    /// <inheritdoc/>
    public WaveFormat OutputWaveFormat => outputWaveFormat ?? throw new InvalidOperationException("No playback source has been initialised");

    /// <inheritdoc/>
    /// <remarks>
    /// Derived from the driver's reported output latency in samples (<c>ASIOgetLatencies</c>).
    /// Computed lazily from cached values — no work is done on the ASIO callback thread.
    /// </remarks>
    public TimeSpan AverageLatency
    {
        get
        {
            if (!isInitialized) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(
                Device.OutputLatencySamples / (double)Device.CurrentSampleRate);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see cref="AverageLatency"/>. ASIO swaps fixed-size buffers at perfectly
    /// regular intervals, so the live latency is always equal to the steady-state latency
    /// to within half a buffer. Reporting an instrumented value would require timestamping
    /// the buffer-switch callback, which we deliberately avoid to keep the real-time path
    /// free of extra work.
    /// </remarks>
    public TimeSpan CurrentLatency => AverageLatency;

    /// <summary>
    /// Get the input channel name
    /// </summary>
    /// <param name="channel">channel index (zero based)</param>
    /// <returns>channel name</returns>
    public string AsioInputChannelName(int channel)
    {
        return channel > DriverInputChannelCount ? "" : Device.Capabilities.InputChannelInfos[channel].name;
    }

    /// <summary>
    /// Get the output channel name
    /// </summary>
    /// <param name="channel">channel index (zero based)</param>
    /// <returns>channel name</returns>
    public string AsioOutputChannelName(int channel)
    {
        return channel > DriverOutputChannelCount ? "" : Device.Capabilities.OutputChannelInfos[channel].name;
    }
}
