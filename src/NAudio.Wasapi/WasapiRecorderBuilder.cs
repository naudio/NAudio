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

    /// <summary>
    /// Use the specified audio device for capture.
    /// </summary>
    public WasapiRecorderBuilder WithDevice(MMDevice device)
    {
        this.device = device;
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

        var actualDevice = device ?? GetDefaultDevice(useLoopback);
        return new WasapiRecorder(actualDevice, shareMode, useEventSync,
            bufferMilliseconds, requestedFormat, mmcssTaskName, useLoopback,
            configureEchoCancellationReference, echoCancellationReferenceEndpointId, useCommunicationsMode);
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
            return WasapiRecorder.CreateProcessLoopbackAsync(
                processLoopbackId.Value, processLoopbackMode,
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
