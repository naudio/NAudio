using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Wave;

/// <summary>
/// Modern WASAPI audio player with zero-copy buffer access, MMCSS thread priority,
/// and IAudioClient3 low-latency support. Created via <see cref="WasapiPlayerBuilder"/>.
/// </summary>
public class WasapiPlayer : IWavePlayer, IWavePosition, IAsyncDisposable
{
    private readonly MMDevice mmDevice;
    private readonly AudioClientShareMode shareMode;
    private readonly bool isUsingEventSync;
    private readonly AudioStreamCategory? audioCategory;
    private readonly string mmcssTaskName;
    private readonly bool preferLowLatency;
    private readonly bool requireLowLatency;
    private readonly SynchronizationContext syncContext;
    private readonly EventWaitHandle stopEvent = new(false, EventResetMode.ManualReset);

    private AudioClient audioClient;
    private AudioRenderClient renderClient;
    private int latencyMilliseconds;
    private int bufferFrameCount;
    private int bytesPerFrame;
    private EventWaitHandle frameEvent;
    private Thread playThread;
    private volatile PlaybackState playbackState;
    // Termination is driven by this flag (and stopEvent), not by publishing PlaybackState.Stopped.
    // That lets the play thread keep PlaybackState.Stopped as its final act — set only after the
    // device has been stopped and reset — so an observer that polls PlaybackState and disposes can
    // never race the thread's last COM access (see Stop/PlayThread and issue #970).
    private volatile bool stopRequested;
    private IWaveProvider waveProvider;

    /// <summary>
    /// Raised when playback stops, either because the source ended or an error occurred.
    /// If a <see cref="SynchronizationContext"/> was captured at construction, this event
    /// is raised on that context (e.g. the UI thread).
    /// </summary>
    public event EventHandler<StoppedEventArgs> PlaybackStopped;

    /// <summary>
    /// Current playback state.
    /// </summary>
    public PlaybackState PlaybackState => playbackState;

    /// <summary>
    /// The output format being sent to the audio device.
    /// </summary>
    public WaveFormat OutputWaveFormat { get; private set; }

    /// <summary>
    /// Whether IAudioClient3 low-latency shared mode is actually in use after <see cref="Init"/>.
    /// This is only ever true when <see cref="WasapiPlayerBuilder.WithLowLatency"/> was requested
    /// <em>and</em> the device, share mode, and source format allowed it. When low latency was
    /// requested but could not be honoured, playback silently falls back to standard shared mode
    /// and this remains false — check it to find out what you actually got.
    /// </summary>
    public bool LowLatencyActive { get; private set; }

    /// <summary>
    /// The latency in milliseconds actually in use after <see cref="Init"/>. In low-latency mode
    /// this is derived from the engine period the device granted, so it may differ from the value
    /// requested via <see cref="WasapiPlayerBuilder.WithLatency"/>.
    /// </summary>
    public int LatencyMilliseconds => latencyMilliseconds;

    /// <summary>
    /// When low latency was requested via <see cref="WasapiPlayerBuilder.WithLowLatency"/> but could
    /// not be honoured, a short human-readable explanation of why (e.g. a sample-rate mismatch that
    /// would require resampling). Null when low latency is active or was never requested.
    /// </summary>
    public string LowLatencyUnavailableReason { get; private set; }

    #region Volume

    /// <summary>
    /// Gets or sets the session volume (0.0 to 1.0). This controls your application's
    /// volume as shown in the Windows volume mixer, without affecting other applications.
    /// Delegates to <see cref="SessionVolume"/>.
    /// </summary>
    public float Volume
    {
        get => SessionVolume.Volume;
        set
        {
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            SessionVolume.Volume = value;
        }
    }

    /// <summary>
    /// Gets or sets the session mute state. This is the mute toggle for your application
    /// in the Windows volume mixer. Delegates to <see cref="SessionVolume"/>.
    /// </summary>
    public bool IsMuted
    {
        get => SessionVolume.Mute;
        set => SessionVolume.Mute = value;
    }

    /// <summary>
    /// Per-session volume and mute control. This is the volume slider shown for your
    /// application in the Windows volume mixer. Use this for simple volume/mute control
    /// that only affects your application.
    /// </summary>
    /// <remarks>
    /// When following the default device with automatic stream routing (no fixed endpoint) this is
    /// obtained from the audio client itself, so it is only available after <see cref="Init"/>.
    /// </remarks>
    public SimpleAudioVolume SessionVolume =>
        mmDevice != null
            ? mmDevice.AudioSessionManager.SimpleAudioVolume
            : audioClient.SimpleAudioVolume;

    /// <summary>
    /// Per-stream, per-channel volume control (0.0 to 1.0 per channel).
    /// Use this for balance, pan, or independent channel level adjustments.
    /// Only available in shared mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the player is using exclusive mode, where per-stream volume is not available.
    /// </exception>
    public AudioStreamVolume StreamVolume
    {
        get
        {
            if (shareMode == AudioClientShareMode.Exclusive)
                throw new InvalidOperationException(
                    "StreamVolume is not available in exclusive mode. " +
                    "Use DeviceVolume for endpoint-level control, or adjust levels in your audio pipeline before playback.");
            return audioClient.AudioStreamVolume;
        }
    }

    /// <summary>
    /// Device endpoint volume — controls the master volume for the audio device,
    /// affecting all applications playing through it. Includes per-channel levels,
    /// mute, volume step control, dB range information, and change notifications.
    /// Use with care: changes are system-wide and visible to the user.
    /// Available in both shared and exclusive modes.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when following the default device with automatic stream routing: there is no fixed
    /// endpoint, so endpoint-wide volume has no meaning. Use <see cref="Volume"/>/<see cref="SessionVolume"/>
    /// for per-application volume instead.
    /// </exception>
    public AudioEndpointVolume DeviceVolume => mmDevice != null
        ? mmDevice.AudioEndpointVolume
        : throw new InvalidOperationException(
            "DeviceVolume (endpoint-wide volume) is not available when following the default device " +
            "with automatic stream routing, because there is no fixed endpoint. Use Volume or " +
            "SessionVolume for per-application volume instead.");

    #endregion

    internal WasapiPlayer(MMDevice device, AudioClientShareMode shareMode, bool useEventSync,
        int latencyMilliseconds, AudioStreamCategory? audioCategory, string mmcssTaskName,
        bool preferLowLatency, bool requireLowLatency)
    {
        mmDevice = device;
        this.shareMode = shareMode;
        isUsingEventSync = useEventSync;
        this.latencyMilliseconds = latencyMilliseconds;
        this.audioCategory = audioCategory;
        this.mmcssTaskName = mmcssTaskName;
        this.preferLowLatency = preferLowLatency;
        this.requireLowLatency = requireLowLatency;
        syncContext = SynchronizationContext.Current;

        audioClient = device.CreateAudioClient();
        OutputWaveFormat = audioClient.MixFormat;
    }

    // Private constructor for automatic stream routing. The audio client is activated externally via
    // ActivateAudioInterfaceAsync against the default-render virtual endpoint, so there is no MMDevice.
    // Routing is standard shared mode only (exclusive and IAudioClient3 low latency are rejected by the
    // builder), which keeps every MMDevice.CreateAudioClient() recovery path out of the routed flow.
    private WasapiPlayer(AudioClient activatedClient, bool useEventSync, int latencyMilliseconds,
        AudioStreamCategory? audioCategory, string mmcssTaskName)
    {
        mmDevice = null;
        shareMode = AudioClientShareMode.Shared;
        isUsingEventSync = useEventSync;
        this.latencyMilliseconds = latencyMilliseconds;
        this.audioCategory = audioCategory;
        this.mmcssTaskName = mmcssTaskName;
        preferLowLatency = false;
        requireLowLatency = false;
        syncContext = SynchronizationContext.Current;

        audioClient = activatedClient;
        OutputWaveFormat = audioClient.MixFormat;
    }

    internal static async Task<WasapiPlayer> CreateDefaultDeviceRoutingAsync(bool useEventSync,
        int latencyMilliseconds, AudioStreamCategory? audioCategory, string mmcssTaskName)
    {
        // Automatic stream routing follows the default render device, re-routing transparently when
        // the default changes. Activation is asynchronous, hence the async factory.
        var activatedClient = await AudioClient.ActivateDefaultDeviceAsync(DataFlow.Render).ConfigureAwait(false);
        return new WasapiPlayer(activatedClient, useEventSync, latencyMilliseconds, audioCategory, mmcssTaskName);
    }

    /// <summary>
    /// Gets the current position in bytes from the wave output device.
    /// (This is not the same as the position within your reader stream.)
    /// </summary>
    public long GetPosition()
    {
        if (playbackState == PlaybackState.Stopped)
            return 0;

        ulong pos;
        if (playbackState == PlaybackState.Playing)
            pos = audioClient.AudioClockClient.AdjustedPosition;
        else
            audioClient.AudioClockClient.GetPosition(out pos, out _);

        return (long)pos * OutputWaveFormat.AverageBytesPerSecond
             / (long)audioClient.AudioClockClient.Frequency;
    }

    /// <summary>
    /// The device's preferred mix format (shared mode). This is always supported in shared mode.
    /// </summary>
    public WaveFormat DeviceMixFormat => audioClient.MixFormat;

    /// <summary>
    /// Checks whether the specified format is supported by the device in the current share mode.
    /// </summary>
    /// <param name="format">The format to check.</param>
    /// <param name="closestMatch">In shared mode, the closest supported format if the exact format isn't supported. Always null in exclusive mode.</param>
    /// <returns>True if the format is supported.</returns>
    public bool IsFormatSupported(WaveFormat format, out WaveFormatExtensible closestMatch)
    {
        return audioClient.IsFormatSupported(shareMode, format, out closestMatch);
    }

    /// <summary>
    /// Checks whether the specified format is supported by the device in the current share mode.
    /// </summary>
    public bool IsFormatSupported(WaveFormat format)
    {
        return audioClient.IsFormatSupported(shareMode, format);
    }

    /// <summary>
    /// Reports, without opening the stream, what <see cref="Init"/> would do for a source of the given
    /// format: whether playback is possible at all, whether low latency would actually engage, the
    /// format the device would receive, and any latency-free conversions (bit depth / channels) that
    /// would be inserted. Call this before <see cref="Init"/> to validate the chosen options.
    /// </summary>
    /// <param name="sourceFormat">The format of the source you intend to play.</param>
    public WasapiPlaybackCapability GetPlaybackCapability(WaveFormat sourceFormat)
    {
        if (shareMode == AudioClientShareMode.Exclusive)
        {
            if (audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, sourceFormat))
                return new WasapiPlaybackCapability(true, AudioClientShareMode.Exclusive, false,
                    sourceFormat, Array.Empty<string>(), latencyMilliseconds, null);

            var target = WasapiFormatAdaptation.FindSupportedExclusiveFormatAtSampleRate(audioClient, sourceFormat);
            if (target != null)
            {
                var conversions = new List<string>();
                WasapiFormatAdaptation.TryDescribeAdaptation(sourceFormat, target, conversions);
                return new WasapiPlaybackCapability(true, AudioClientShareMode.Exclusive, false,
                    target, conversions, latencyMilliseconds, null);
            }

            return new WasapiPlaybackCapability(false, AudioClientShareMode.Exclusive, false,
                sourceFormat, Array.Empty<string>(), 0,
                $"The device supports no exclusive-mode format at {sourceFormat.SampleRate} Hz; playback would require resampling.");
        }

        // Shared mode. Attempt the low-latency plan first.
        if (preferLowLatency && audioClient.SupportsAudioClient3)
        {
            var mixFormat = audioClient.MixFormat;
            var conversions = new List<string>();
            if (WasapiFormatAdaptation.TryDescribeAdaptation(sourceFormat, mixFormat, conversions))
                return new WasapiPlaybackCapability(true, AudioClientShareMode.Shared, true,
                    mixFormat, conversions, EstimateLowLatencyMilliseconds(mixFormat), null);

            var reason = sourceFormat.SampleRate != mixFormat.SampleRate
                ? $"source sample rate {sourceFormat.SampleRate} Hz does not match the engine mix rate {mixFormat.SampleRate} Hz (would require resampling)"
                : $"source format could not be adapted to the engine mix format ({mixFormat}) without resampling";
            return new WasapiPlaybackCapability(true, AudioClientShareMode.Shared, false,
                sourceFormat, Array.Empty<string>(), latencyMilliseconds, reason);
        }

        // Standard shared mode — the engine converts everything (including sample rate) via AutoConvertPcm.
        return new WasapiPlaybackCapability(true, AudioClientShareMode.Shared, false,
            sourceFormat, Array.Empty<string>(), latencyMilliseconds, null);
    }

    /// <summary>
    /// Estimates the latency the IAudioClient3 low-latency path would achieve for the given format,
    /// from the engine's minimum supported period. Falls back to the configured latency on error.
    /// </summary>
    private int EstimateLowLatencyMilliseconds(WaveFormat mixFormat)
    {
        try
        {
            var period = audioClient.GetSharedModeEnginePeriod(mixFormat).ChooseLowestLatencyPeriod();
            return Math.Max(1, (int)(period * 1000L / mixFormat.SampleRate));
        }
        catch (COMException)
        {
            return latencyMilliseconds;
        }
    }

    /// <summary>
    /// Finds a supported exclusive-mode format for this device, trying the preferred format first,
    /// then falling back through standard sample rates, bit depths, and multi-channel speaker
    /// configurations from the Windows Driver Kit (ksmedia.h).
    /// Use this to discover what format to provide to <see cref="Init"/> when using exclusive mode.
    /// </summary>
    /// <param name="preferredFormat">The format you'd ideally like to use.</param>
    /// <returns>A supported <see cref="WaveFormatExtensible"/>, or null if no supported format was found.</returns>
    public WaveFormatExtensible GetSupportedExclusiveFormat(WaveFormat preferredFormat)
    {
        var deviceSampleRate = audioClient.MixFormat.SampleRate;
        var deviceChannels = audioClient.MixFormat.Channels;

        var sampleRatesToTry = new List<int> { preferredFormat.SampleRate };
        if (!sampleRatesToTry.Contains(deviceSampleRate)) sampleRatesToTry.Add(deviceSampleRate);
        if (!sampleRatesToTry.Contains(44100)) sampleRatesToTry.Add(44100);
        if (!sampleRatesToTry.Contains(48000)) sampleRatesToTry.Add(48000);

        var channelCountsToTry = new List<int> { preferredFormat.Channels };
        if (!channelCountsToTry.Contains(deviceChannels)) channelCountsToTry.Add(deviceChannels);
        if (!channelCountsToTry.Contains(2)) channelCountsToTry.Add(2);

        var bitDepthsToTry = new List<int> { preferredFormat.BitsPerSample };
        if (!bitDepthsToTry.Contains(32)) bitDepthsToTry.Add(32);
        if (!bitDepthsToTry.Contains(24)) bitDepthsToTry.Add(24);
        if (!bitDepthsToTry.Contains(16)) bitDepthsToTry.Add(16);

        // Channel mask 0 uses the WaveFormatExtensible default (sequential bit-shift),
        // which covers stereo, 3.0, 3.1, and the obsolete 5.1/7.1 layouts.
        // Additional masks from ksmedia.h cover standard speaker configurations.
        var channelMasksToTry = new List<int> { 0 };
        if (channelCountsToTry.Contains(1)) channelMasksToTry.Add(0x0004); // 1.0: FC        (KSAUDIO_SPEAKER_MONO)
        if (channelCountsToTry.Contains(2)) channelMasksToTry.Add(0x000C); // 1.1: FC|LFE    (KSAUDIO_SPEAKER_1POINT1)
        if (channelCountsToTry.Contains(3)) channelMasksToTry.Add(0x000B); // 2.1: FL|FR|LFE (KSAUDIO_SPEAKER_2POINT1)
        if (channelCountsToTry.Contains(4))
        {
            channelMasksToTry.Add(0x0033); // 4.0: FL|FR|BL|BR (KSAUDIO_SPEAKER_QUAD)
            channelMasksToTry.Add(0x0107); // 4.0: FL|FR|FC|BC (KSAUDIO_SPEAKER_SURROUND)
        }
        if (channelCountsToTry.Contains(5)) channelMasksToTry.Add(0x0607); // 5.0: FL|FR|FC|SL|SR           (KSAUDIO_SPEAKER_5POINT0)
        if (channelCountsToTry.Contains(6)) channelMasksToTry.Add(0x060F); // 5.1: FL|FR|FC|LFE|SL|SR       (KSAUDIO_SPEAKER_5POINT1_SURROUND)
        if (channelCountsToTry.Contains(7)) channelMasksToTry.Add(0x0637); // 7.0: FL|FR|FC|BL|BR|SL|SR     (KSAUDIO_SPEAKER_7POINT0)
        if (channelCountsToTry.Contains(8)) channelMasksToTry.Add(0x063F); // 7.1: FL|FR|FC|LFE|BL|BR|SL|SR (KSAUDIO_SPEAKER_7POINT1_SURROUND)

        foreach (var sampleRate in sampleRatesToTry)
        {
            foreach (var channelCount in channelCountsToTry)
            {
                foreach (var bitDepth in bitDepthsToTry)
                {
                    foreach (var channelMask in channelMasksToTry)
                    {
                        var format = new WaveFormatExtensible(sampleRate, bitDepth, channelCount, channelMask);
                        if (audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, format))
                            return format;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Initialize for playing the specified audio source.
    /// </summary>
    /// <remarks>
    /// The source's bit depth and channel count are adapted automatically (without resampling) to a
    /// format the device supports: in standard shared mode the audio engine does this for you; in
    /// exclusive and IAudioClient3 low-latency modes <see cref="WasapiPlayer"/> inserts the necessary
    /// conversion (PCM↔float, mono↔stereo). Sample rate is never changed — converting it would add
    /// latency — so exclusive mode throws if the device cannot accept the source's sample rate, and
    /// low latency silently falls back to standard shared mode (see <see cref="LowLatencyActive"/> and
    /// <see cref="LowLatencyUnavailableReason"/>). When the source already matches the device format,
    /// playback is zero-copy with no conversion inserted.
    /// </remarks>
    public void Init(IWaveProvider source)
    {
        waveProvider = source;
        InitializeAudioClient(source.WaveFormat);
    }


    private void InitializeAudioClient(WaveFormat sourceFormat)
    {
        long latencyRefTimes = latencyMilliseconds * 10000L;
        OutputWaveFormat = sourceFormat;

        // Set audio category via IAudioClient2 if requested. Must happen before Initialize.
        if (audioCategory.HasValue && audioClient.SupportsAudioClient2)
        {
            audioClient.SetClientProperties(audioCategory.Value);
        }

        if (shareMode == AudioClientShareMode.Exclusive)
        {
            // Exclusive mode does no conversion in the engine: pick a device-supported format at the
            // source's sample rate and adapt bit depth/channels to it (throwing if that's impossible).
            ConfigureExclusiveFormat(sourceFormat);
            if (isUsingEventSync)
                InitializeWithEventSync(AudioClientStreamFlags.None, latencyRefTimes);
            else
                audioClient.Initialize(shareMode, AudioClientStreamFlags.None, latencyRefTimes, 0, OutputWaveFormat, Guid.Empty);
            renderClient = audioClient.AudioRenderClient;
            return;
        }

        // Shared mode. Try the IAudioClient3 low-latency path first, adapting bit depth/channels to
        // the engine mix format if the sample rate already matches.
        if (preferLowLatency)
        {
            if (audioClient.SupportsAudioClient3 && TryConfigureLowLatency(sourceFormat))
            {
                LowLatencyActive = true;
                return;
            }

            // Not honoured. Record why (TryConfigureLowLatency sets the reason when it was tried).
            if (!audioClient.SupportsAudioClient3)
                LowLatencyUnavailableReason = "IAudioClient3 is not supported on this device (requires Windows 10 version 1607 or later)";

            if (requireLowLatency)
                throw new InvalidOperationException(
                    $"Low latency was required but could not be honoured: {LowLatencyUnavailableReason}. " +
                    "Call GetPlaybackCapability before Init to check, request WithLowLatency() without required, or use standard shared mode.");
        }

        // Standard shared mode. AutoConvertPcm lets the audio engine handle ALL adaptation, including
        // sample-rate conversion, so the source is passed through unchanged.
        OutputWaveFormat = sourceFormat;
        var flags = AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
        if (isUsingEventSync)
            InitializeWithEventSync(flags, latencyRefTimes);
        else
            audioClient.Initialize(shareMode, flags, latencyRefTimes, 0, OutputWaveFormat, Guid.Empty);

        renderClient = audioClient.AudioRenderClient;
    }

    /// <summary>
    /// Configures the IAudioClient3 low-latency shared stream, adapting the source to the engine mix
    /// format (bit depth/channels only — never sample rate). Returns false, restoring the original
    /// source provider and recording <see cref="LowLatencyUnavailableReason"/>, when low latency
    /// can't be honoured; the caller then falls back to standard shared mode.
    /// </summary>
    private bool TryConfigureLowLatency(WaveFormat sourceFormat)
    {
        var mixFormat = audioClient.MixFormat;
        var adapted = WasapiFormatAdaptation.AdaptProvider(waveProvider, mixFormat);
        if (adapted == null)
        {
            LowLatencyUnavailableReason = sourceFormat.SampleRate != mixFormat.SampleRate
                ? $"source sample rate {sourceFormat.SampleRate} Hz does not match the engine mix rate {mixFormat.SampleRate} Hz (would require resampling)"
                : $"source format could not be adapted to the engine mix format ({mixFormat}) without resampling";
            return false;
        }

        var originalProvider = waveProvider;
        waveProvider = adapted;
        OutputWaveFormat = mixFormat;
        if (TryInitializeLowLatency())
            return true;

        // The engine declined; revert so the standard shared path runs against the original source.
        waveProvider = originalProvider;
        LowLatencyUnavailableReason = "the audio engine declined the low-latency request";
        return false;
    }

    /// <summary>
    /// Resolves and applies a device-supported exclusive-mode format for the source, adapting bit
    /// depth and channels (never sample rate) where needed.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the device cannot accept the source's sample rate in exclusive mode, which would
    /// require resampling.
    /// </exception>
    private void ConfigureExclusiveFormat(WaveFormat sourceFormat)
    {
        // Native, zero-copy match?
        if (audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, sourceFormat))
        {
            OutputWaveFormat = sourceFormat;
            return;
        }

        // A supported format at the SAME sample rate that we can reach by bit-depth/channel conversion.
        var target = WasapiFormatAdaptation.FindSupportedExclusiveFormatAtSampleRate(audioClient, sourceFormat);
        if (target != null)
        {
            var adapted = WasapiFormatAdaptation.AdaptProvider(waveProvider, target);
            if (adapted != null)
            {
                waveProvider = adapted;
                OutputWaveFormat = target;
                return;
            }
        }

        throw new NotSupportedException(
            $"The device does not support the source format ({sourceFormat}) in exclusive mode, and it " +
            "cannot be adapted without resampling (the device requires a different sample rate). " +
            "Resample to a supported sample rate first, or use shared mode.");
    }

    /// <summary>
    /// Performs the IAudioClient3 low-latency COM initialization against <see cref="OutputWaveFormat"/>,
    /// which the caller (<see cref="TryConfigureLowLatency"/>) has already resolved to the engine mix
    /// format. Returns false (leaving a fresh <see cref="audioClient"/> ready for the standard path)
    /// if the engine declines.
    /// </summary>
    /// <remarks>
    /// IAudioClient3 shared low-latency does <em>no</em> format conversion: per the API contract the
    /// only supported stream flag is <see cref="AudioClientStreamFlags.EventCallback"/>, and the only
    /// format the engine accepts is its own current mix format — which is why
    /// <see cref="TryConfigureLowLatency"/> resolves the source to the mix format before calling here.
    /// <para>
    /// Note that shared-mode <see cref="AudioClient.IsFormatSupported(AudioClientShareMode, WaveFormat)"/>
    /// is <em>not</em> a usable gate: it reports success for any format the shared-mode converter can
    /// handle (i.e. almost anything), but InitializeSharedAudioStream bypasses that converter.
    /// </para>
    /// </remarks>
    private bool TryInitializeLowLatency()
    {
        // Defensive assertion: TryConfigureLowLatency has already resolved OutputWaveFormat to the
        // mix format, which is the only format InitializeSharedAudioStream accepts.
        if (!OutputWaveFormat.Equals(audioClient.MixFormat))
            return false;

        AudioClientPeriodInfo periodInfo;
        try
        {
            periodInfo = audioClient.GetSharedModeEnginePeriod(OutputWaveFormat);
        }
        catch (COMException)
        {
            return false;
        }

        var periodInFrames = periodInfo.ChooseLowestLatencyPeriod();

        try
        {
            audioClient.InitializeSharedAudioStream(
                AudioClientStreamFlags.EventCallback,
                periodInFrames, OutputWaveFormat, Guid.Empty);
        }
        catch (COMException)
        {
            // The engine declined this period/format combination. The client may be in a partially
            // initialized state, so recreate it before the standard path runs.
            audioClient.Dispose();
            audioClient = mmDevice.CreateAudioClient();
            return false;
        }

        latencyMilliseconds = (int)(periodInFrames * 1000L / OutputWaveFormat.SampleRate);
        if (latencyMilliseconds < 1) latencyMilliseconds = 1;

        frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
        renderClient = audioClient.AudioRenderClient;
        return true;
    }

    private void InitializeWithEventSync(AudioClientStreamFlags flags, long latencyRefTimes)
    {
        if (shareMode == AudioClientShareMode.Shared)
        {
            audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                latencyRefTimes, 0, OutputWaveFormat, Guid.Empty);

            var streamLatency = audioClient.StreamLatency;
            if (streamLatency != 0)
                latencyMilliseconds = (int)(streamLatency / 10000);
        }
        else
        {
            try
            {
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                    latencyRefTimes, latencyRefTimes, OutputWaveFormat, Guid.Empty);
            }
            catch (COMException ex) when (ex.ErrorCode == AudioClientErrorCode.BufferSizeNotAligned)
            {
                long newLatencyRefTimes = (long)(10000000.0 / OutputWaveFormat.SampleRate * audioClient.BufferSize + 0.5);
                audioClient.Dispose();
                audioClient = mmDevice.CreateAudioClient();
                audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags,
                    newLatencyRefTimes, newLatencyRefTimes, OutputWaveFormat, Guid.Empty);
            }
        }

        frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
        renderClient = audioClient.AudioRenderClient;
    }

    /// <summary>
    /// Begin playback.
    /// </summary>
    public void Play()
    {
        // Already actively playing and not in the middle of an async Stop()? Nothing to do.
        if (playbackState == PlaybackState.Playing && !stopRequested)
            return;

        // Resume from pause without restarting the playback thread.
        if (playbackState == PlaybackState.Paused && !stopRequested)
        {
            playbackState = PlaybackState.Playing;
            return;
        }

        // Either fully stopped, or a Stop() is still in flight (stopRequested) from a recent call —
        // the common Stop()+Play() "restart" idiom. A new playback thread cannot start until the
        // previous one has finished and released the audio device, so wait for it here. stopRequested
        // is still set, so the outgoing thread sees the stop and exits; only then do we rearm and
        // start fresh. This blocks briefly *only* during such a restart — a normal Play() from a
        // stopped state does not wait.
        JoinPlayThread();
        stopRequested = false;
        stopEvent.Reset();
        playThread = new Thread(PlayThread) { IsBackground = true, Name = "NAudio WasapiPlayer Playback" };
        playbackState = PlaybackState.Playing;
        playThread.Start();
    }

    /// <summary>
    /// Requests that playback stop, and returns immediately without waiting for the playback thread
    /// to finish. The device is stopped and flushed on the playback thread; completion is signalled
    /// by the <see cref="PlaybackStopped"/> event. <see cref="PlaybackState"/> therefore remains
    /// <see cref="PlaybackState.Playing"/> (or <see cref="PlaybackState.Paused"/>) until that event
    /// fires.
    /// </summary>
    /// <remarks>
    /// This is the everyday "stop playback" call and is safe to use from a UI thread or other
    /// latency-sensitive context. If you need to wait until the device has actually been released —
    /// for example before re-initializing with a different format or device — use
    /// <see cref="StopAsync"/>, or handle <see cref="PlaybackStopped"/>.
    /// <para>
    /// Calling <see cref="Play"/> immediately after <see cref="Stop"/> restarts playback correctly:
    /// <see cref="Play"/> waits for the outgoing playback thread to release the device before
    /// starting a new one. Because the stop is asynchronous, the stopped session still raises
    /// <see cref="PlaybackStopped"/> (after the restart) — so a <see cref="PlaybackStopped"/> handler
    /// that tears things down should check <see cref="PlaybackState"/> rather than assume playback is
    /// finished. To restart with no stale event, await <see cref="StopAsync"/> before
    /// <see cref="Play"/>.
    /// </para>
    /// </remarks>
    public void Stop()
    {
        if (playbackState != PlaybackState.Stopped)
        {
            stopRequested = true;
            stopEvent.Set();
        }
    }

    /// <summary>
    /// Requests that playback stop and returns a task that completes once the playback thread has
    /// fully stopped — the audio device has been stopped, reset and is free to be reused, and
    /// <see cref="PlaybackState"/> has reached <see cref="PlaybackState.Stopped"/>. The returned
    /// task does not block the calling thread while it waits.
    /// </summary>
    /// <remarks>
    /// Use this when the next step depends on the device being released (e.g. re-<see cref="Init"/>
    /// with a different source or endpoint, or deterministic teardown inside an async method). For a
    /// plain "stop now, don't wait" call use <see cref="Stop"/> instead.
    /// </remarks>
    public Task StopAsync()
    {
        Stop();
        return JoinPlayThreadAsync();
    }

    /// <summary>
    /// Pause playback without flushing buffers.
    /// </summary>
    public void Pause()
    {
        if (playbackState == PlaybackState.Playing)
            playbackState = PlaybackState.Paused;
    }

    private void PlayThread()
    {
        IntPtr mmcssHandle = IntPtr.Zero;
        Exception exception = null;
        try
        {
            // Elevate thread priority via MMCSS
            if (mmcssTaskName != null)
            {
                uint taskIndex = 0;
                mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
            }

            bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = OutputWaveFormat.BlockAlign;

            // Fill the initial buffer. A zero-length source ends immediately, before the device is
            // ever started; the finally block still publishes Stopped and raises PlaybackStopped.
            if (!FillBuffer(bufferFrameCount))
            {
                var waitHandles = (isUsingEventSync || frameEvent != null)
                    ? new WaitHandle[] { frameEvent, stopEvent }
                    : null;

                audioClient.Start();

                var reachedEndOfStream = false;
                while (!stopRequested)
                {
                    if (waitHandles != null)
                    {
                        WaitHandle.WaitAny(waitHandles, 3 * latencyMilliseconds, false);
                    }
                    else
                    {
                        stopEvent.WaitOne(latencyMilliseconds / 2, false);
                    }

                    if (stopRequested)
                        break;

                    if (playbackState == PlaybackState.Playing)
                    {
                        if (reachedEndOfStream)
                        {
                            if (shareMode == AudioClientShareMode.Exclusive || audioClient.CurrentPadding == 0)
                                break;
                            continue;
                        }

                        int numFramesPadding = (isUsingEventSync && shareMode == AudioClientShareMode.Exclusive)
                            ? 0
                            : audioClient.CurrentPadding;
                        int numFramesAvailable = bufferFrameCount - numFramesPadding;
                        if (numFramesAvailable > 10)
                        {
                            if (FillBuffer(numFramesAvailable))
                                reachedEndOfStream = true;
                        }
                    }
                }

                audioClient.Stop();
                audioClient.Reset();
            }
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            // Publish Stopped only now — after the device has been stopped and reset — so a caller
            // that observes PlaybackState.Stopped (or handles PlaybackStopped) and then disposes
            // cannot race the play thread's final COM access. Dispose/StopAsync additionally join
            // this thread before releasing the audio client (#970).
            playbackState = PlaybackState.Stopped;
            if (mmcssHandle != IntPtr.Zero)
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            RaisePlaybackStopped(exception);
        }
    }

    /// <summary>
    /// Fills the WASAPI render buffer directly from the audio source using Span (zero-copy).
    /// Returns true if the source has ended.
    /// </summary>
    private bool FillBuffer(int frameCount)
    {
        using var lease = renderClient.GetBufferLease(frameCount, bytesPerFrame);
        int bytesRead = waveProvider.Read(lease.Buffer);
        if (bytesRead == 0)
        {
            lease.Release(0, AudioClientBufferFlags.Silent);
            return true;
        }

        int framesRead = bytesRead / bytesPerFrame;
        if (isUsingEventSync && shareMode == AudioClientShareMode.Exclusive)
        {
            // In exclusive event mode, must release the full frame count
            if (bytesRead < frameCount * bytesPerFrame)
                lease.Buffer.Slice(bytesRead).Clear();
            lease.Release(frameCount);
        }
        else
        {
            lease.Release(framesRead);
        }
        return false;
    }

    private void RaisePlaybackStopped(Exception e)
    {
        var handler = PlaybackStopped;
        if (handler != null)
        {
            if (syncContext != null)
                syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
            else
                handler(this, new StoppedEventArgs(e));
        }
    }

    /// <summary>
    /// Stops playback, waits for the playback thread to finish (blocking), and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Stop();
        JoinPlayThread();
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Stops playback and releases all resources without blocking the calling thread while it waits
    /// for the playback thread to finish. Prefer this over <see cref="Dispose()"/> in async or UI
    /// contexts where blocking is undesirable.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Stop();
        await JoinPlayThreadAsync().ConfigureAwait(false);
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Waits for the playback thread to finish, then clears the reference. The play thread does all
    /// its COM cleanup before exiting, so joining here guarantees the audio client is idle before
    /// <see cref="DisposeCore"/> releases it. Skips the join if called on the play thread itself
    /// (e.g. from a PlaybackStopped handler raised with no SynchronizationContext) to avoid a
    /// self-join deadlock.
    /// </summary>
    private void JoinPlayThread()
    {
        var thread = playThread;
        if (thread != null && thread.ManagedThreadId != Environment.CurrentManagedThreadId)
        {
            thread.Join();
            playThread = null;
        }
    }

    /// <summary>
    /// Asynchronous counterpart to <see cref="JoinPlayThread"/>: completes once the playback thread
    /// has finished, without blocking the caller. Returns a completed task when there is nothing to
    /// wait for, or when called on the play thread itself (avoiding a self-join deadlock).
    /// </summary>
    private Task JoinPlayThreadAsync()
    {
        var thread = playThread;
        if (thread == null || thread.ManagedThreadId == Environment.CurrentManagedThreadId)
        {
            playThread = null;
            return Task.CompletedTask;
        }
        return Task.Run(() =>
        {
            thread.Join();
            playThread = null;
        });
    }

    private void DisposeCore()
    {
        audioClient?.Dispose();
        audioClient = null;
        renderClient = null;
        stopEvent.Dispose();
        frameEvent?.Dispose();
    }
}
