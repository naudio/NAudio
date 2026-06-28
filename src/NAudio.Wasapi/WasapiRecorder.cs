using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.Wave;

/// <summary>
/// Modern WASAPI audio recorder with zero-copy buffer access, MMCSS thread priority,
/// process-specific loopback capture, and IAsyncEnumerable support.
/// Created via <see cref="WasapiRecorderBuilder"/>.
/// </summary>
public class WasapiRecorder : IDisposable, IAsyncDisposable
{
    private const long ReftimesPerMillisec = 10000;

    private readonly AudioClientShareMode shareMode;
    private readonly bool isUsingEventSync;
    private readonly bool useLoopback;
    private readonly bool isProcessLoopback;
    private readonly int bufferMilliseconds;
    private readonly string mmcssTaskName;
    private readonly bool configureEchoCancellationReference;
    private readonly string echoCancellationReferenceEndpointId;
    private readonly bool useCommunicationsMode;
    private readonly bool preferLowLatency;
    private readonly bool requireLowLatency;
    private readonly MMDevice mmDevice;
    private readonly SynchronizationContext syncContext;

    private AudioClient audioClient;
    private int bytesPerFrame;
    private readonly WaveFormat waveFormat;
    private volatile CaptureState captureState;
    private Thread captureThread;
    private EventWaitHandle frameEvent;

    /// <summary>
    /// Fired when captured audio data is available. The buffer span is only valid
    /// for the duration of the callback — copy it if you need to keep it.
    /// </summary>
    public event CaptureDataAvailableHandler DataAvailable;

    /// <summary>
    /// Fired when recording stops, either by request or due to an error.
    /// </summary>
    public event EventHandler<StoppedEventArgs> RecordingStopped;

    /// <summary>
    /// The capture format.
    /// </summary>
    public WaveFormat WaveFormat => waveFormat;

    /// <summary>
    /// Current capture state.
    /// </summary>
    public CaptureState CaptureState => captureState;

    /// <summary>
    /// Whether IAudioClient3 low-latency shared mode is actually in use after recording has been
    /// initialized. This is only ever true when <see cref="WasapiRecorderBuilder.WithLowLatency"/> was
    /// requested <em>and</em> the device, share mode, and capture format allowed it. When low latency
    /// was requested but could not be honoured, capture silently falls back to standard shared mode and
    /// this remains false — check it to find out what you actually got.
    /// </summary>
    public bool LowLatencyActive { get; private set; }

    /// <summary>
    /// When low latency was requested via <see cref="WasapiRecorderBuilder.WithLowLatency"/> but could
    /// not be honoured, a short human-readable explanation of why (e.g. the requested capture format did
    /// not match the device mix format). Null when low latency is active or was never requested.
    /// </summary>
    public string LowLatencyUnavailableReason { get; private set; }

    /// <summary>
    /// The effective latency in milliseconds in use after recording has been initialized. In standard
    /// mode this is the configured buffer length; in IAudioClient3 low-latency mode it is derived from
    /// the engine period the device granted, so it is typically much smaller. Zero before
    /// <see cref="StartRecording"/> (or <see cref="CaptureAsync"/>) has initialized the audio client.
    /// </summary>
    public int LatencyMilliseconds { get; private set; }

    internal WasapiRecorder(MMDevice device, AudioClientShareMode shareMode, bool useEventSync,
        int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName, bool useLoopback = false,
        bool configureEchoCancellationReference = false, string echoCancellationReferenceEndpointId = null,
        bool useCommunicationsMode = false, bool preferLowLatency = false, bool requireLowLatency = false)
    {
        syncContext = SynchronizationContext.Current;
        this.shareMode = shareMode;
        isUsingEventSync = useEventSync;
        this.useLoopback = useLoopback;
        this.bufferMilliseconds = bufferMilliseconds;
        this.mmcssTaskName = mmcssTaskName;
        this.configureEchoCancellationReference = configureEchoCancellationReference;
        this.echoCancellationReferenceEndpointId = echoCancellationReferenceEndpointId;
        this.useCommunicationsMode = useCommunicationsMode;
        this.preferLowLatency = preferLowLatency;
        this.requireLowLatency = requireLowLatency;

        mmDevice = device;
        audioClient = device.CreateAudioClient();
        waveFormat = requestedFormat ?? audioClient.MixFormat;
    }

    // Private constructor for process loopback (audioClient created externally via ActivateAudioInterfaceAsync)
    private WasapiRecorder(AudioClient audioClient, bool useEventSync,
        int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName)
    {
        syncContext = SynchronizationContext.Current;
        shareMode = AudioClientShareMode.Shared;
        // The process-loopback virtual device is always event-driven (LOOPBACK | EVENTCALLBACK).
        isUsingEventSync = true;
        isProcessLoopback = true;
        this.bufferMilliseconds = bufferMilliseconds;
        this.mmcssTaskName = mmcssTaskName;
        this.audioClient = audioClient;
        // GetMixFormat is not supported on the process-loopback virtual device, so a format must
        // be supplied. Default to 44.1kHz stereo IEEE float (widely compatible).
        waveFormat = requestedFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    }

    internal static async Task<WasapiRecorder> CreateProcessLoopbackAsync(uint processId, ProcessLoopbackMode mode,
        bool useEventSync, int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName)
    {
        // Process loopback uses ActivateAudioInterfaceAsync with AUDIOCLIENT_ACTIVATION_PARAMS —
        // an inherently asynchronous activation, hence the async factory.
        var audioClient = await AudioClient.ActivateProcessLoopbackAsync(processId, mode).ConfigureAwait(false);
        return new WasapiRecorder(audioClient, useEventSync, bufferMilliseconds, requestedFormat, mmcssTaskName);
    }

    // Private constructor for automatic stream routing. The audio client is activated externally via
    // ActivateAudioInterfaceAsync against the default-capture virtual endpoint, so there is no MMDevice.
    // Unlike the process-loopback device, the routing endpoint behaves like a normal shared-mode capture
    // client (GetMixFormat and AutoConvertPcm work), so this uses the standard shared path — not the
    // loopback flag. Low latency is rejected by the builder, keeping CreateAudioClient() recovery out.
    private WasapiRecorder(AudioClient audioClient, bool useEventSync, int bufferMilliseconds,
        WaveFormat requestedFormat, string mmcssTaskName, bool isDefaultDeviceRouting)
    {
        syncContext = SynchronizationContext.Current;
        shareMode = AudioClientShareMode.Shared;
        isUsingEventSync = useEventSync;
        this.bufferMilliseconds = bufferMilliseconds;
        this.mmcssTaskName = mmcssTaskName;
        this.audioClient = audioClient;
        waveFormat = requestedFormat ?? audioClient.MixFormat;
    }

    internal static async Task<WasapiRecorder> CreateDefaultDeviceRoutingAsync(
        bool useEventSync, int bufferMilliseconds, WaveFormat requestedFormat, string mmcssTaskName)
    {
        // Automatic stream routing follows the default capture device, re-routing transparently when
        // the default changes. Activation is asynchronous, hence the async factory.
        var audioClient = await AudioClient.ActivateDefaultDeviceAsync(DataFlow.Capture).ConfigureAwait(false);
        return new WasapiRecorder(audioClient, useEventSync, bufferMilliseconds, requestedFormat, mmcssTaskName,
            isDefaultDeviceRouting: true);
    }

    /// <summary>
    /// Start recording.
    /// </summary>
    public void StartRecording()
    {
        if (captureState != CaptureState.Stopped)
            throw new InvalidOperationException("Already recording");

        captureState = CaptureState.Starting;
        InitializeAudioClient();
        captureThread = new Thread(CaptureThread) { IsBackground = true, Name = "NAudio WasapiRecorder Capture" };
        captureThread.Start();
    }

    /// <summary>
    /// Stop recording.
    /// </summary>
    public void StopRecording()
    {
        if (captureState != CaptureState.Stopped)
            captureState = CaptureState.Stopping;
    }

    /// <summary>
    /// Capture audio as an async enumerable. Each yielded <see cref="AudioBuffer"/> contains
    /// a copy of the captured data (safe to store/process asynchronously).
    /// Use the <see cref="DataAvailable"/> event for zero-copy processing instead.
    /// </summary>
    public async IAsyncEnumerable<AudioBuffer> CaptureAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (captureState != CaptureState.Stopped)
            throw new InvalidOperationException("Already recording");

        captureState = CaptureState.Starting;
        InitializeAudioClient();

        IntPtr mmcssHandle = IntPtr.Zero;
        try
        {
            if (mmcssTaskName != null)
            {
                uint taskIndex = 0;
                mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
            }

            bytesPerFrame = waveFormat.BlockAlign;
            var capture = audioClient.AudioCaptureClient;
            audioClient.Start();
            captureState = CaptureState.Capturing;

            while (!cancellationToken.IsCancellationRequested && captureState == CaptureState.Capturing)
            {
                // Wait for data
                if (isUsingEventSync && frameEvent != null)
                {
                    await Task.Run(() => frameEvent.WaitOne(3 * bufferMilliseconds), cancellationToken);
                }
                else
                {
                    await Task.Delay(bufferMilliseconds / 2, cancellationToken);
                }

                // Read all available packets using IntPtr API (ref struct can't cross yield)
                int packetSize = capture.GetNextPacketSize();
                while (packetSize > 0)
                {
                    var bufferPtr = capture.GetBuffer(out var framesRead, out var flags,
                        out var devicePosition, out var qpcPosition);
                    try
                    {
                        if ((flags & AudioClientBufferFlags.Silent) == 0 && framesRead > 0)
                        {
                            int byteCount = framesRead * bytesPerFrame;
                            var data = new byte[byteCount];
                            Marshal.Copy(bufferPtr, data, 0, byteCount);
                            yield return new AudioBuffer(data, flags, devicePosition, qpcPosition);
                        }
                    }
                    finally
                    {
                        capture.ReleaseBuffer(framesRead);
                    }

                    packetSize = capture.GetNextPacketSize();
                }
            }
        }
        finally
        {
            audioClient.Stop();
            audioClient.Reset();
            captureState = CaptureState.Stopped;
            if (mmcssHandle != IntPtr.Zero)
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
        }
    }

    private void InitializeAudioClient()
    {
        LatencyMilliseconds = bufferMilliseconds;

        // Try the IAudioClient3 low-latency shared path first when requested. It uses a much smaller
        // engine period than the configured buffer length, at the cost of higher wake-up frequency.
        if (preferLowLatency)
        {
            var precondition = LowLatencyPreconditionReason();
            if (precondition == null && TryInitializeLowLatency())
            {
                LowLatencyActive = true;
            }
            else
            {
                if (precondition != null)
                    LowLatencyUnavailableReason = precondition;
                if (requireLowLatency)
                    throw new InvalidOperationException(
                        $"Low latency was required but could not be honoured: {LowLatencyUnavailableReason}. " +
                        "Low-latency capture needs shared mode, event-driven sync, no loopback, IAudioClient3 " +
                        "support, and a capture format matching the device mix format (omit WithFormat). " +
                        "Request WithLowLatency() without required to fall back to standard shared mode instead.");
                InitializeStandard();
            }
        }
        else
        {
            InitializeStandard();
        }

        if (configureEchoCancellationReference)
        {
            // GetService for the AEC control is only valid once the stream is initialized.
            var aecControl = audioClient.TryGetAcousticEchoCancellationControl();
            if (aecControl == null)
            {
                throw new NotSupportedException(
                    "The capture endpoint does not support controlling the acoustic echo cancellation " +
                    "reference endpoint. This requires Windows 11 build 22621 or later and a capture " +
                    "endpoint whose AEC effect supports loopback reference control.");
            }
            // A null endpoint id lets Windows pick the loopback reference device itself.
            aecControl.SetReferenceEndpoint(echoCancellationReferenceEndpointId);
        }
    }

    /// <summary>
    /// Standard (non-low-latency) initialization. The audio engine handles any format conversion in
    /// shared mode via AutoConvertPcm, so the configured buffer length is honoured as requested.
    /// </summary>
    private void InitializeStandard()
    {
        long bufferDuration = bufferMilliseconds * ReftimesPerMillisec;

        AudioClientStreamFlags flags;
        if (isProcessLoopback)
        {
            // The process-loopback virtual device only accepts LOOPBACK (+ EVENTCALLBACK below)
            // with an explicit PCM format; AutoConvertPcm/SrcDefaultQuality are not supported.
            flags = AudioClientStreamFlags.Loopback;
        }
        else
        {
            flags = shareMode == AudioClientShareMode.Shared
                ? AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality
                : AudioClientStreamFlags.None;

            if (useLoopback)
                flags |= AudioClientStreamFlags.Loopback;
        }

        if (isUsingEventSync)
            flags |= AudioClientStreamFlags.EventCallback;

        // The communications signal-processing mode must be requested before Initialize. It is what
        // engages the system's AEC/NS/AGC capture pipeline (and exposes the AEC reference control) on
        // most endpoints. Process-loopback clients have no IAudioClient2 and are excluded by the builder.
        if (useCommunicationsMode)
            audioClient.SetClientProperties(AudioStreamCategory.Communications);

        audioClient.Initialize(shareMode, flags, bufferDuration, 0, waveFormat, Guid.Empty);

        if (isUsingEventSync)
        {
            frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
        }
    }

    /// <summary>
    /// Returns a human-readable reason why IAudioClient3 low-latency capture cannot be attempted for the
    /// current configuration, or null if all preconditions are satisfied. IAudioClient3's
    /// InitializeSharedAudioStream only supports a shared, event-driven stream in the device mix format —
    /// it does no format conversion and the loopback flag is not permitted.
    /// </summary>
    private string LowLatencyPreconditionReason()
    {
        if (isProcessLoopback)
            return "process-loopback capture cannot use IAudioClient3 low-latency mode";
        if (useLoopback)
            return "loopback capture cannot use IAudioClient3 low-latency mode (the loopback stream flag is incompatible with a shared low-latency stream)";
        if (shareMode != AudioClientShareMode.Shared)
            return "low latency is only available in shared mode";
        if (!isUsingEventSync)
            return "low latency requires event-driven synchronization; use WithEventSync()";
        if (!audioClient.SupportsAudioClient3)
            return "IAudioClient3 is not supported on this device (requires Windows 10 version 1607 or later)";
        if (!waveFormat.Equals(audioClient.MixFormat))
            return $"the requested capture format ({waveFormat}) does not match the device mix format ({audioClient.MixFormat}); " +
                   "low-latency shared capture cannot convert formats — omit WithFormat to capture at the device mix format";
        return null;
    }

    /// <summary>
    /// Performs the IAudioClient3 low-latency COM initialization against the device mix format (which
    /// <see cref="LowLatencyPreconditionReason"/> has already confirmed matches <see cref="waveFormat"/>).
    /// Returns false — leaving a fresh <see cref="audioClient"/> ready for the standard path and
    /// recording <see cref="LowLatencyUnavailableReason"/> — if the engine declines.
    /// </summary>
    private bool TryInitializeLowLatency()
    {
        AudioClientPeriodInfo periodInfo;
        try
        {
            periodInfo = audioClient.GetSharedModeEnginePeriod(waveFormat);
        }
        catch (COMException)
        {
            LowLatencyUnavailableReason = "the audio engine did not report a low-latency period for this format";
            return false;
        }

        var periodInFrames = periodInfo.ChooseLowestLatencyPeriod();

        // The communications signal-processing mode (if requested) must be set before initialization;
        // it is independent of the low-latency periodicity.
        if (useCommunicationsMode)
            audioClient.SetClientProperties(AudioStreamCategory.Communications);

        try
        {
            // InitializeSharedAudioStream's only supported flag is EventCallback.
            audioClient.InitializeSharedAudioStream(
                AudioClientStreamFlags.EventCallback, periodInFrames, waveFormat, Guid.Empty);
        }
        catch (COMException)
        {
            // The engine declined this period/format combination. The client may be in a partially
            // initialized state, so recreate it before the standard path runs.
            audioClient.Dispose();
            audioClient = mmDevice.CreateAudioClient();
            LowLatencyUnavailableReason = "the audio engine declined the low-latency request";
            return false;
        }

        LatencyMilliseconds = Math.Max(1, (int)(periodInFrames * 1000L / waveFormat.SampleRate));

        frameEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        audioClient.SetEventHandle(frameEvent.SafeWaitHandle.DangerousGetHandle());
        return true;
    }

    /// <summary>
    /// Gets the acoustic echo cancellation (AEC) reference control for this capture stream, or null
    /// if the endpoint does not support controlling the AEC reference endpoint. Use it to change the
    /// render endpoint used as the echo cancellation reference stream while recording.
    /// </summary>
    /// <remarks>
    /// Only available after <see cref="StartRecording"/> (or <see cref="CaptureAsync"/>) has
    /// initialized the audio client. Returns null before then. Requires Windows 11 build 22621 or later.
    /// </remarks>
    public AcousticEchoCancellationControl AcousticEchoCancellationControl =>
        captureState == CaptureState.Stopped ? null : audioClient?.TryGetAcousticEchoCancellationControl();

    private void CaptureThread()
    {
        IntPtr mmcssHandle = IntPtr.Zero;
        Exception exception = null;
        try
        {
            if (mmcssTaskName != null)
            {
                uint taskIndex = 0;
                mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics(mmcssTaskName, ref taskIndex);
            }

            bytesPerFrame = waveFormat.BlockAlign;
            var capture = audioClient.AudioCaptureClient;

            audioClient.Start();
            captureState = CaptureState.Capturing;

            int waitMilliseconds = isUsingEventSync
                ? 3 * bufferMilliseconds
                : bufferMilliseconds / 2;

            while (captureState == CaptureState.Capturing)
            {
                if (isUsingEventSync && frameEvent != null)
                    frameEvent.WaitOne(waitMilliseconds, false);
                else
                    Thread.Sleep(waitMilliseconds);

                if (captureState != CaptureState.Capturing)
                    break;

                ReadAvailablePackets(capture);
            }
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            audioClient.Stop();
            audioClient.Reset();
            captureState = CaptureState.Stopped;
            if (mmcssHandle != IntPtr.Zero)
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            RaiseRecordingStopped(exception);
        }
    }

    private void ReadAvailablePackets(AudioCaptureClient capture)
    {
        int packetSize = capture.GetNextPacketSize();
        while (packetSize > 0)
        {
            using var lease = capture.GetBufferLease(bytesPerFrame);
            DataAvailable?.Invoke(lease.Buffer, lease.Flags);
            packetSize = capture.GetNextPacketSize();
        }
    }

    private void RaiseRecordingStopped(Exception e)
    {
        var handler = RecordingStopped;
        if (handler != null)
        {
            if (syncContext != null)
                syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
            else
                handler(this, new StoppedEventArgs(e));
        }
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        StopRecording();
        captureThread?.Join();
        audioClient?.Dispose();
        audioClient = null;
        frameEvent?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Async dispose.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        StopRecording();
        if (captureThread != null)
            await Task.Run(() => captureThread.Join());
        audioClient?.Dispose();
        audioClient = null;
        frameEvent?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Delegate for zero-copy capture data events. The buffer is a <see cref="ReadOnlySpan{T}"/>
/// directly over the WASAPI buffer — only valid for the duration of the callback.
/// </summary>
/// <param name="buffer">The captured audio data. Copy it if you need to keep it.</param>
/// <param name="flags">Buffer flags from WASAPI (e.g. Silent).</param>
public delegate void CaptureDataAvailableHandler(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags);

/// <summary>
/// A captured audio buffer suitable for async consumption (heap-allocated copy).
/// </summary>
public readonly struct AudioBuffer
{
    /// <summary>
    /// The captured audio data.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    /// Buffer flags from WASAPI (e.g. Silent).
    /// </summary>
    public AudioClientBufferFlags Flags { get; }

    /// <summary>
    /// Device position at time of capture.
    /// </summary>
    public long DevicePosition { get; }

    /// <summary>
    /// QPC position at time of capture (100-nanosecond units).
    /// </summary>
    public long QPCPosition { get; }

    internal AudioBuffer(byte[] data, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        Data = data;
        Flags = flags;
        DevicePosition = devicePosition;
        QPCPosition = qpcPosition;
    }
}
