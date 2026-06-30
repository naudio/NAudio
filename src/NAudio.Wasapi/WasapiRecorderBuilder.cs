using NAudio.CoreAudioApi;
using System;
using System.Threading.Tasks;

namespace NAudio.Wave;

/// <summary>
/// Fluent builder for creating a <see cref="WasapiRecorder"/>.
/// </summary>
public class WasapiRecorderBuilder
{
    private MMDevice device;
    private AudioClientShareMode shareMode = AudioClientShareMode.Shared;
    private bool useEventSync = true;
    private bool useLoopback;
    private int bufferMilliseconds = 100;
    private WaveFormat requestedFormat;
    private string mmcssTaskName;
    private uint? processLoopbackId;
    private ProcessLoopbackMode processLoopbackMode = ProcessLoopbackMode.IncludeTargetProcessTree;
    private bool configureEchoCancellationReference;
    private string echoCancellationReferenceEndpointId;
    private bool useCommunicationsMode;
    private bool preferLowLatency;
    private bool requireLowLatency;
    private bool useDefaultDeviceRouting;

    /// <summary>
    /// Use the specified audio device for capture.
    /// </summary>
    public WasapiRecorderBuilder WithDevice(MMDevice device)
    {
        this.device = device;
        return this;
    }

    /// <summary>
    /// Follow the default capture device with automatic stream routing (Windows 10 version 1607 or
    /// later). When the user changes the default recording device — or unplugs the current one — Windows
    /// seamlessly transfers capture to the new default device with no application code.
    /// </summary>
    /// <remarks>
    /// Activation is asynchronous, so the recorder must be created via <see cref="BuildAsync"/> rather
    /// than <see cref="Build"/>. Routing is standard shared mode only: do not combine it with
    /// <see cref="WithDevice"/>, <see cref="WithExclusiveMode"/>, <see cref="WithLowLatency"/>,
    /// <see cref="WithLoopbackCapture"/>, or <see cref="WithProcessLoopback"/>.
    /// </remarks>
    public WasapiRecorderBuilder WithDefaultDeviceStreamRouting()
    {
        useDefaultDeviceRouting = true;
        return this;
    }

    /// <summary>
    /// Use shared mode (default).
    /// </summary>
    public WasapiRecorderBuilder WithSharedMode()
    {
        shareMode = AudioClientShareMode.Shared;
        return this;
    }

    /// <summary>
    /// Use exclusive mode for lower latency capture.
    /// </summary>
    public WasapiRecorderBuilder WithExclusiveMode()
    {
        shareMode = AudioClientShareMode.Exclusive;
        return this;
    }

    /// <summary>
    /// Use event-based synchronization (default).
    /// </summary>
    public WasapiRecorderBuilder WithEventSync()
    {
        useEventSync = true;
        return this;
    }

    /// <summary>
    /// Use polling-based synchronization.
    /// </summary>
    public WasapiRecorderBuilder WithPollingSync()
    {
        useEventSync = false;
        return this;
    }

    /// <summary>
    /// Capture audio from a render device in loopback mode (what the device is playing).
    /// Pass a render endpoint via <see cref="WithDevice"/>; if no device is set the default
    /// render device is used.
    /// </summary>
    public WasapiRecorderBuilder WithLoopbackCapture()
    {
        useLoopback = true;
        return this;
    }

    /// <summary>
    /// Set the internal buffer length in milliseconds. Default is 100ms.
    /// Lower values reduce latency but increase CPU usage.
    /// </summary>
    public WasapiRecorderBuilder WithBufferLength(int milliseconds)
    {
        bufferMilliseconds = milliseconds;
        return this;
    }

    /// <summary>
    /// Request a specific capture format. If not set, uses the device's mix format.
    /// In shared mode with AutoConvertPcm, the engine will convert to this format.
    /// </summary>
    public WasapiRecorderBuilder WithFormat(WaveFormat format)
    {
        requestedFormat = format;
        return this;
    }

    /// <summary>
    /// Elevate the capture thread priority via MMCSS.
    /// Common task names: "Pro Audio", "Audio", "Capture".
    /// </summary>
    public WasapiRecorderBuilder WithMmcssThreadPriority(string taskName = "Pro Audio")
    {
        mmcssTaskName = taskName;
        return this;
    }

    /// <summary>
    /// Sets the render endpoint used as the reference stream for acoustic echo cancellation (AEC)
    /// on the capture stream. Pass the render device whose output should be cancelled out of the
    /// microphone signal, or null (the default) to let Windows pick the loopback reference itself.
    /// </summary>
    /// <remarks>
    /// AEC is performed by an audio processing object in the capture pipeline; this only selects the
    /// loopback reference endpoint. <see cref="WasapiRecorder.StartRecording"/> throws
    /// <see cref="NotSupportedException"/> if the capture endpoint does not support controlling the
    /// AEC reference endpoint. Requires Windows 11 build 22621 or later.
    /// </remarks>
    /// <param name="referenceRenderDevice">The render device to use as the reference stream, or null
    /// to let Windows choose automatically.</param>
    public WasapiRecorderBuilder WithEchoCancellationReferenceEndpoint(MMDevice referenceRenderDevice = null)
    {
        configureEchoCancellationReference = true;
        echoCancellationReferenceEndpointId = referenceRenderDevice?.ID;
        // The AEC effect (and therefore the reference-endpoint control) is only inserted into the
        // capture pipeline when the stream is opened in the communications signal-processing mode.
        // Most endpoints (laptop mics, webcams) expose no AEC control in the default mode, so opt in
        // automatically here. Call WithCommunicationsMode() explicitly for AEC without selecting a
        // reference endpoint.
        useCommunicationsMode = true;
        return this;
    }

    /// <summary>
    /// Opens the capture stream in the communications signal-processing mode
    /// (<see cref="AudioStreamCategory.Communications"/>). This requests the system's communications
    /// audio pipeline — acoustic echo cancellation, noise suppression and automatic gain control —
    /// where the endpoint and OS provide it, and is what makes the
    /// <see cref="WithEchoCancellationReferenceEndpoint"/> control available on most devices.
    /// </summary>
    /// <remarks>
    /// Requires IAudioClient2 (Windows 8+); it is not applied to process-loopback capture. The exact
    /// effects applied depend on the capture endpoint and the installed audio processing objects.
    /// </remarks>
    public WasapiRecorderBuilder WithCommunicationsMode()
    {
        useCommunicationsMode = true;
        return this;
    }

    /// <summary>
    /// Request low-latency shared-mode capture via IAudioClient3 if available. This opens the capture
    /// stream at the engine's minimum supported period rather than the configured buffer length, which
    /// is useful for real-time scenarios such as live visualization or monitoring.
    /// </summary>
    /// <remarks>
    /// Low latency requires shared mode, event-driven synchronization (the default), no loopback, and a
    /// capture format matching the device mix format — so do not combine it with <see cref="WithFormat"/>
    /// requesting a different format, <see cref="WithLoopbackCapture"/>, <see cref="WithExclusiveMode"/>,
    /// <see cref="WithPollingSync"/>, or <see cref="WithProcessLoopback"/>. It also needs IAudioClient3
    /// (Windows 10 version 1607 or later).
    /// </remarks>
    /// <param name="required">
    /// When false (the default), capture silently falls back to standard shared mode if low latency
    /// can't be honoured — inspect <see cref="WasapiRecorder.LowLatencyActive"/> and
    /// <see cref="WasapiRecorder.LowLatencyUnavailableReason"/> afterwards to see what you got. When
    /// true, <see cref="WasapiRecorder.StartRecording"/> (or <see cref="WasapiRecorder.CaptureAsync"/>)
    /// instead throws an <see cref="InvalidOperationException"/> if low latency can't be achieved.
    /// </param>
    public WasapiRecorderBuilder WithLowLatency(bool required = false)
    {
        preferLowLatency = true;
        requireLowLatency = required;
        return this;
    }

    /// <summary>
    /// Capture audio from a specific process (and optionally its child processes).
    /// Requires Windows 10 2004 (build 19041) or later.
    /// This uses ActivateAudioInterfaceAsync with AUDIOCLIENT_PROCESS_LOOPBACK_PARAMS.
    /// </summary>
    /// <param name="processId">The process ID to capture audio from.</param>
    /// <param name="mode">Whether to include or exclude the target process tree.</param>
    public WasapiRecorderBuilder WithProcessLoopback(uint processId,
        ProcessLoopbackMode mode = ProcessLoopbackMode.IncludeTargetProcessTree)
    {
        processLoopbackId = processId;
        processLoopbackMode = mode;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="WasapiRecorder"/> with the configured settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="WithProcessLoopback"/> was configured — process loopback is
    /// activated asynchronously, so <see cref="BuildAsync"/> must be used instead.
    /// </exception>
    public WasapiRecorder Build()
    {
        if (processLoopbackId.HasValue)
        {
            throw new InvalidOperationException(
                "Process loopback capture is activated asynchronously — call BuildAsync() instead of Build().");
        }

        if (useDefaultDeviceRouting)
        {
            throw new InvalidOperationException(
                "Automatic stream routing is activated asynchronously — call BuildAsync() instead of Build().");
        }

        var actualDevice = device ?? GetDefaultDevice(useLoopback);
        return new WasapiRecorder(actualDevice, shareMode, useEventSync,
            bufferMilliseconds, requestedFormat, mmcssTaskName, useLoopback,
            configureEchoCancellationReference, echoCancellationReferenceEndpointId, useCommunicationsMode,
            preferLowLatency, requireLowLatency);
    }

    /// <summary>
    /// Builds the <see cref="WasapiRecorder"/> with the configured settings. Required when
    /// <see cref="WithProcessLoopback"/> is used, since that activation path is asynchronous;
    /// for all other configurations this simply wraps <see cref="Build"/>.
    /// </summary>
    public Task<WasapiRecorder> BuildAsync()
    {
        if (processLoopbackId.HasValue)
        {
            if (configureEchoCancellationReference || useCommunicationsMode)
            {
                throw new InvalidOperationException(
                    "Communications mode and acoustic echo cancellation are not supported with process-loopback capture.");
            }
            if (preferLowLatency)
            {
                throw new InvalidOperationException(
                    "IAudioClient3 low latency is not supported with process-loopback capture.");
            }
            return WasapiRecorder.CreateProcessLoopbackAsync(
                processLoopbackId.Value, processLoopbackMode,
                useEventSync, bufferMilliseconds, requestedFormat, mmcssTaskName);
        }

        if (useDefaultDeviceRouting)
        {
            if (device != null)
                throw new InvalidOperationException(
                    "Automatic stream routing follows the default device, so it cannot be combined with WithDevice().");
            if (shareMode == AudioClientShareMode.Exclusive)
                throw new InvalidOperationException(
                    "Automatic stream routing is only available in shared mode — it cannot be combined with WithExclusiveMode().");
            if (useLoopback)
                throw new InvalidOperationException(
                    "Automatic stream routing follows the default capture device — it cannot be combined with WithLoopbackCapture().");
            if (preferLowLatency)
                throw new InvalidOperationException(
                    "IAudioClient3 low latency is not supported with automatic stream routing.");

            return WasapiRecorder.CreateDefaultDeviceRoutingAsync(
                useEventSync, bufferMilliseconds, requestedFormat, mmcssTaskName);
        }

        return Task.FromResult(Build());
    }

    private static MMDevice GetDefaultDevice(bool loopback)
    {
        var enumerator = new MMDeviceEnumerator();
        var flow = loopback ? DataFlow.Render : DataFlow.Capture;
        return enumerator.GetDefaultAudioEndpoint(flow, Role.Console);
    }
}
