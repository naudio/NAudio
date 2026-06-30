using NAudio.CoreAudioApi;
using System;
using System.Threading.Tasks;

namespace NAudio.Wave;

/// <summary>
/// Fluent builder for creating a <see cref="WasapiPlayer"/>.
/// </summary>
public class WasapiPlayerBuilder
{
    private MMDevice device;
    private AudioClientShareMode shareMode = AudioClientShareMode.Shared;
    private int latencyMilliseconds = 200;
    private bool useEventSync = true;
    private AudioStreamCategory? audioCategory;
    private string mmcssTaskName;
    private bool preferLowLatency;
    private bool requireLowLatency;
    private bool useDefaultDeviceRouting;
    private bool useRawMode;

    /// <summary>
    /// Use the specified audio device for playback.
    /// </summary>
    public WasapiPlayerBuilder WithDevice(MMDevice device)
    {
        this.device = device;
        return this;
    }

    /// <summary>
    /// Follow the default render device with automatic stream routing (Windows 10 version 1607 or
    /// later). When the user changes the default playback device — or unplugs the current one — Windows
    /// seamlessly transfers playback to the new default device with no application code.
    /// </summary>
    /// <remarks>
    /// Activation is asynchronous, so the player must be created via <see cref="BuildAsync"/> rather
    /// than <see cref="Build"/>. Routing is standard shared mode only: do not combine it with
    /// <see cref="WithDevice"/>, <see cref="WithExclusiveMode"/>, or <see cref="WithLowLatency"/>.
    /// Because there is no fixed endpoint, <see cref="WasapiPlayer.DeviceVolume"/> is unavailable
    /// (use <see cref="WasapiPlayer.Volume"/>/<see cref="WasapiPlayer.SessionVolume"/> instead).
    /// </remarks>
    public WasapiPlayerBuilder WithDefaultDeviceStreamRouting()
    {
        useDefaultDeviceRouting = true;
        return this;
    }

    /// <summary>
    /// Use shared mode (default). Audio is mixed with other applications.
    /// </summary>
    public WasapiPlayerBuilder WithSharedMode()
    {
        shareMode = AudioClientShareMode.Shared;
        return this;
    }

    /// <summary>
    /// Use exclusive mode. The application has sole access to the audio device.
    /// Lower latency is possible but other applications cannot play audio.
    /// </summary>
    public WasapiPlayerBuilder WithExclusiveMode()
    {
        shareMode = AudioClientShareMode.Exclusive;
        return this;
    }

    /// <summary>
    /// Set the desired latency in milliseconds. Default is 200ms.
    /// In shared mode with IAudioClient3, the engine may use a lower period
    /// if <see cref="WithLowLatency"/> is also specified.
    /// </summary>
    public WasapiPlayerBuilder WithLatency(int milliseconds)
    {
        latencyMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Use event-based synchronization (default). More efficient than polling.
    /// </summary>
    public WasapiPlayerBuilder WithEventSync()
    {
        useEventSync = true;
        return this;
    }

    /// <summary>
    /// Use polling-based synchronization instead of events.
    /// </summary>
    public WasapiPlayerBuilder WithPollingSync()
    {
        useEventSync = false;
        return this;
    }

    /// <summary>
    /// Set the audio stream category, used by Windows for audio policy decisions
    /// (ducking, routing, priority). Requires IAudioClient2 (Windows 8+).
    /// </summary>
    public WasapiPlayerBuilder WithCategory(AudioStreamCategory category)
    {
        audioCategory = category;
        return this;
    }

    /// <summary>
    /// Elevate the audio thread priority via MMCSS (Multimedia Class Scheduler Service).
    /// Common task names: "Pro Audio", "Audio", "Playback".
    /// </summary>
    public WasapiPlayerBuilder WithMmcssThreadPriority(string taskName = "Pro Audio")
    {
        mmcssTaskName = taskName;
        return this;
    }

    /// <summary>
    /// Request low-latency shared mode via IAudioClient3 if available.
    /// </summary>
    /// <param name="required">
    /// When false (the default), playback silently falls back to standard shared mode if low latency
    /// can't be honoured (e.g. the source sample rate doesn't match the engine, or IAudioClient3 isn't
    /// supported) — inspect <see cref="WasapiPlayer.LowLatencyActive"/> afterwards to see what you got.
    /// When true, <see cref="WasapiPlayer.Init"/> instead throws an
    /// <see cref="System.InvalidOperationException"/> if low latency can't be achieved.
    /// </param>
    public WasapiPlayerBuilder WithLowLatency(bool required = false)
    {
        preferLowLatency = true;
        requireLowLatency = required;
        return this;
    }

    /// <summary>
    /// Open a 'raw' audio stream that bypasses the system signal-processing pipeline — the audio
    /// enhancements / APO effects (loudness equalization, bass boost, virtual surround, downmixing,
    /// etc.) that Windows applies by default. Only endpoint-specific, always-on processing in the APO,
    /// driver and hardware remains. Use this when you need the device to receive your samples
    /// unaltered, for example to keep stereo channels isolated rather than mixed toward mono.
    /// </summary>
    /// <remarks>
    /// Requires IAudioClient2 (Windows 8.1+); <see cref="WasapiPlayer.Init"/> throws
    /// <see cref="InvalidOperationException"/> if the device does not support it. Compatible with shared
    /// and exclusive mode, low latency, event/polling sync, a stream category and default-device stream
    /// routing. In exclusive mode the engine is already bypassed, so raw mode mainly affects any
    /// remaining driver/APO processing.
    /// </remarks>
    public WasapiPlayerBuilder WithRawMode()
    {
        useRawMode = true;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="WasapiPlayer"/> with the configured settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithDefaultDeviceStreamRouting"/> was configured — automatic stream
    /// routing is activated asynchronously, so <see cref="BuildAsync"/> must be used instead.
    /// </exception>
    public WasapiPlayer Build()
    {
        if (useDefaultDeviceRouting)
        {
            throw new InvalidOperationException(
                "Automatic stream routing is activated asynchronously — call BuildAsync() instead of Build().");
        }

        var actualDevice = device ?? GetDefaultRenderDevice();
        return new WasapiPlayer(actualDevice, shareMode, useEventSync, latencyMilliseconds,
            audioCategory, mmcssTaskName, preferLowLatency, requireLowLatency, useRawMode);
    }

    /// <summary>
    /// Builds the <see cref="WasapiPlayer"/> with the configured settings. Required when
    /// <see cref="WithDefaultDeviceStreamRouting"/> is used, since that activation path is asynchronous;
    /// for all other configurations this simply wraps <see cref="Build"/>.
    /// </summary>
    public Task<WasapiPlayer> BuildAsync()
    {
        if (useDefaultDeviceRouting)
        {
            if (device != null)
                throw new InvalidOperationException(
                    "Automatic stream routing follows the default device, so it cannot be combined with WithDevice().");
            if (shareMode == AudioClientShareMode.Exclusive)
                throw new InvalidOperationException(
                    "Automatic stream routing is only available in shared mode — it cannot be combined with WithExclusiveMode().");
            if (preferLowLatency)
                throw new InvalidOperationException(
                    "IAudioClient3 low latency is not supported with automatic stream routing.");

            return WasapiPlayer.CreateDefaultDeviceRoutingAsync(
                useEventSync, latencyMilliseconds, audioCategory, mmcssTaskName, useRawMode);
        }

        return Task.FromResult(Build());
    }

    private static MMDevice GetDefaultRenderDevice()
    {
        var enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
    }
}
