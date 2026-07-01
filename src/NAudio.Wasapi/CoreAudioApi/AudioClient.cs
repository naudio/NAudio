using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;

namespace NAudio.CoreAudioApi;

/// <summary>
/// Windows CoreAudio AudioClient. Wraps IAudioClient, IAudioClient2, and IAudioClient3.
/// </summary>
public class AudioClient : IDisposable
{
    private static readonly Guid ID_AudioStreamVolume = new("93014887-242D-4068-8A15-CF5E93B90FE3");
    private static readonly Guid ID_AudioClockClient = new("CD63314F-3FBA-4a1b-812C-EF96358728E7");
    private static readonly Guid ID_AudioRenderClient = new("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
    private static readonly Guid ID_AudioCaptureClient = new("c8adbd64-e71e-48a0-a4de-185c395cd317");
    private static readonly Guid ID_AcousticEchoCancellationControl = new("f4ae25b5-aaa3-437d-b6b3-dbbe2d0e9549");
    private static readonly Guid ID_SimpleAudioVolume = new("87CE5498-68D6-44E5-9215-6DA47EF883D8");
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    private static readonly Guid IID_IAudioClient2 = new("726778CD-F60A-4eda-82DE-E47610CD78AA");
    private static readonly Guid IID_IActivateAudioInterfaceCompletionHandler = new("41D949AB-9862-444A-80F6-C261334DA5EB");
    private IAudioClient audioClientInterface;
    private readonly IAudioClient2 audioClientInterface2;
    private readonly IAudioClient3 audioClientInterface3;
    private WaveFormat mixFormat;
    private AudioRenderClient audioRenderClient;
    private AudioCaptureClient audioCaptureClient;
    private AudioClockClient audioClockClient;
    private AudioStreamVolume audioStreamVolume;
    private SimpleAudioVolume simpleAudioVolume;
    private AcousticEchoCancellationControl acousticEchoCancellationControl;
    private AudioClientShareMode shareMode;
    private int disposed;

    /// <summary>
    /// The special device interface path passed to ActivateAudioInterfaceAsync to request a
    /// per-process loopback capture client. See the Windows ApplicationLoopback sample.
    /// </summary>
    private const string VirtualAudioDeviceProcessLoopback = "VAD\\Process_Loopback";

    /// <summary>
    /// Device-interface GUID representing the default render device for automatic stream routing.
    /// </summary>
    private static readonly Guid DEVINTERFACE_AUDIO_RENDER = new("E6327CAD-DCEC-4949-AE8A-991E976A79D2");

    /// <summary>
    /// Device-interface GUID representing the default capture device for automatic stream routing.
    /// </summary>
    private static readonly Guid DEVINTERFACE_AUDIO_CAPTURE = new("2EEF81BE-33FA-4800-9670-1CD474972C3F");

    /// <summary>
    /// Activates an AudioClient that follows the current default endpoint with automatic stream
    /// routing: when the user changes (or unplugs) the default device, Windows seamlessly transfers
    /// the stream to the new default device with no application code. Requires Windows 10 version
    /// 1607 or later.
    /// </summary>
    /// <param name="dataFlow">Whether to follow the default render or capture device.</param>
    /// <remarks>
    /// This activates the special <c>DEVINTERFACE_AUDIO_RENDER</c> / <c>DEVINTERFACE_AUDIO_CAPTURE</c>
    /// virtual endpoint via <c>ActivateAudioInterfaceAsync</c>. Unlike the process-loopback virtual
    /// device, the returned client is backed by the current default endpoint and behaves like a normal
    /// shared-mode client, including <see cref="MixFormat"/>.
    /// </remarks>
    public static Task<AudioClient> ActivateDefaultDeviceAsync(DataFlow dataFlow)
    {
        var deviceInterface = dataFlow == DataFlow.Capture ? DEVINTERFACE_AUDIO_CAPTURE : DEVINTERFACE_AUDIO_RENDER;
        // StringFromIID yields the registry-format braced GUID (e.g. "{E6327CAD-...}"); Guid's "B"
        // format produces the same string the API expects (device-interface paths are case-insensitive).
        var deviceInterfacePath = deviceInterface.ToString("B").ToUpperInvariant();

        // Activate against the base IAudioClient (with null activation params): render+IAudioClient
        // is one of the activations Windows documents as not requiring the UI thread, and the
        // AudioClient constructor still cross-casts to IAudioClient2/3 where the device supports them.
        return ActivateAudioInterfaceAsync(deviceInterfacePath, IID_IAudioClient, IntPtr.Zero, _ => { });
    }

    /// <summary>
    /// Activate Async
    /// </summary>
    public static Task<AudioClient> ActivateAsync(string deviceInterfacePath, AudioClientProperties? audioClientProperties)
    {
        return ActivateAudioInterfaceAsync(deviceInterfacePath, IID_IAudioClient2, IntPtr.Zero,
            client =>
            {
                if (audioClientProperties != null)
                {
                    IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(audioClientProperties.Value));
                    try
                    {
                        Marshal.StructureToPtr(audioClientProperties.Value, p, false);
                        ((IAudioClient2)client).SetClientProperties(p);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(p);
                    }
                }
            });
    }

    /// <summary>
    /// Activates an AudioClient for process-specific loopback capture, capturing the audio
    /// rendered by the specified process (and optionally its child processes).
    /// Requires Windows 10 version 2004 (build 19041) or later.
    /// </summary>
    /// <param name="processId">The target process id.</param>
    /// <param name="mode">Whether to include or exclude the target process tree.</param>
    public static async Task<AudioClient> ActivateProcessLoopbackAsync(uint processId,
        ProcessLoopbackMode mode = ProcessLoopbackMode.IncludeTargetProcessTree)
    {
        var activationParams = new AudioClientActivationParams
        {
            ActivationType = AudioClientActivationType.ProcessLoopback,
            ProcessLoopbackParams = new AudioClientProcessLoopbackParams
            {
                TargetProcessId = processId,
                ProcessLoopbackMode = mode,
            },
        };

        // Both the activation params and the PROPVARIANT wrapping them must stay alive until the
        // native activation call has consumed them, so allocate on the unmanaged heap rather than
        // pinning locals across the await.
        int paramsSize = Marshal.SizeOf<AudioClientActivationParams>();
        IntPtr paramsPtr = Marshal.AllocHGlobal(paramsSize);
        IntPtr pvPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariantBlobHeader>());
        try
        {
            Marshal.StructureToPtr(activationParams, paramsPtr, false);
            var pv = new PropVariantBlobHeader
            {
                Vt = (ushort)VarType.VT_BLOB,
                BlobSize = (uint)paramsSize,
                BlobData = paramsPtr,
            };
            Marshal.StructureToPtr(pv, pvPtr, false);

            // The process-loopback virtual device only supports the base IAudioClient interface.
            return await ActivateAudioInterfaceAsync(VirtualAudioDeviceProcessLoopback, IID_IAudioClient, pvPtr, _ => { })
                .ConfigureAwait(false);
        }
        finally
        {
            Marshal.FreeHGlobal(pvPtr);
            Marshal.FreeHGlobal(paramsPtr);
        }
    }

    private static async Task<AudioClient> ActivateAudioInterfaceAsync(string deviceInterfacePath,
        Guid riid, IntPtr activationParams, Action<IAudioClient> initializeAction)
    {
        var icbh = new ActivateAudioInterfaceCompletionHandler(initializeAction);
        // Multi-vtable CCW hazard: GetOrCreateComInterfaceForObject returns the IUnknown
        // vtable pointer; the WASAPI runtime expects an IActivateAudioInterfaceCompletionHandler
        // pointer. QI for the specific IID before handing the pointer to native, otherwise
        // it dereferences the wrong vtable (STATUS_STACK_BUFFER_OVERRUN).
        var unknownPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(icbh, CreateComInterfaceFlags.None);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknownPtr, in IID_IActivateAudioInterfaceCompletionHandler, out var handlerPtr));
            try
            {
                Marshal.ThrowExceptionForHR(NativeMethods.ActivateAudioInterfaceAsync(deviceInterfacePath, riid, activationParams, handlerPtr, out var operationPtr));
                Marshal.Release(operationPtr);
            }
            finally
            {
                Marshal.Release(handlerPtr);
            }
        }
        finally
        {
            Marshal.Release(unknownPtr);
        }
        var audioClientInterface = await icbh.Completion.ConfigureAwait(false);
        return new AudioClient(audioClientInterface);
    }

    /// <summary>
    /// Sequential-layout view of the head of a PROPVARIANT carrying a VT_BLOB payload. The CLR
    /// lays out the trailing pointer with native alignment, so this matches the real BLOB layout
    /// on both x86 (pointer at offset 12) and x64 (offset 16) without manual padding.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariantBlobHeader
    {
        public ushort Vt;
        public ushort Reserved1;
        public ushort Reserved2;
        public ushort Reserved3;
        public uint BlobSize;
        public IntPtr BlobData;
    }

    /// <summary>
    /// Creates a new AudioClient
    /// </summary>
    internal AudioClient(IAudioClient audioClientInterface)
    {
        this.audioClientInterface = audioClientInterface;
        audioClientInterface2 = audioClientInterface as IAudioClient2;
        audioClientInterface3 = audioClientInterface as IAudioClient3;
    }

    /// <summary>
    /// Whether this audio client supports IAudioClient2 features (Windows 8+)
    /// </summary>
    public bool SupportsAudioClient2 => audioClientInterface2 != null;

    /// <summary>
    /// Whether this audio client supports IAudioClient3 low-latency features (Windows 10 1607+)
    /// </summary>
    public bool SupportsAudioClient3 => audioClientInterface3 != null;

    /// <summary>
    /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
    /// Can be called before initialize
    /// </summary>
    public WaveFormat MixFormat
    {
        get
        {
            if (mixFormat == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetMixFormat(out var waveFormatPointer));
                var waveFormat = WaveFormat.MarshalFromPtr(waveFormatPointer);
                Marshal.FreeCoTaskMem(waveFormatPointer);
                mixFormat = waveFormat;
            }
            return mixFormat;
        }
    }

    /// <summary>
    /// Initializes the Audio Client
    /// </summary>
    /// <param name="shareMode">Share Mode</param>
    /// <param name="streamFlags">Stream Flags</param>
    /// <param name="bufferDuration">Buffer Duration</param>
    /// <param name="periodicity">Periodicity</param>
    /// <param name="waveFormat">Wave Format</param>
    /// <param name="audioSessionGuid">Audio Session GUID (can be null)</param>
    public void Initialize(AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags,
        long bufferDuration,
        long periodicity,
        WaveFormat waveFormat,
        Guid audioSessionGuid)
    {
        this.shareMode = shareMode;
        var formatPtr = WaveFormat.MarshalToPtr(waveFormat);
        try
        {
            CoreAudioException.ThrowIfFailed(
                audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, formatPtr, in audioSessionGuid));
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }
        // may have changed the mix format so reset it
        mixFormat = null;
    }

    /// <summary>
    /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint. (must initialize first)
    /// </summary>
    public int BufferSize
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.GetBufferSize(out uint bufferSize));
            return (int)bufferSize;
        }
    }

    /// <summary>
    /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
    /// </summary>
    public long StreamLatency
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.GetStreamLatency(out var latency));
            return latency;
        }
    }

    /// <summary>
    /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
    /// </summary>
    public int CurrentPadding
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.GetCurrentPadding(out var currentPadding));
            return currentPadding;
        }
    }

    /// <summary>
    /// Retrieves the length of the periodic interval separating successive processing passes by the audio engine on the data in the endpoint buffer.
    /// (can be called before initialize)
    /// </summary>
    public long DefaultDevicePeriod
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.GetDevicePeriod(out var defaultDevicePeriod, out _));
            return defaultDevicePeriod;
        }
    }

    /// <summary>
    /// Gets the minimum device period
    /// (can be called before initialize)
    /// </summary>
    public long MinimumDevicePeriod
    {
        get
        {
            CoreAudioException.ThrowIfFailed(audioClientInterface.GetDevicePeriod(out _, out var minimumDevicePeriod));
            return minimumDevicePeriod;
        }
    }

    /// <summary>
    /// Returns the AudioStreamVolume service for this AudioClient.
    /// </summary>
    /// <remarks>
    /// This returns the AudioStreamVolume object ONLY for shared audio streams.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This is thrown when an exclusive audio stream is being used.
    /// </exception>
    public AudioStreamVolume AudioStreamVolume
    {
        get
        {
            if (shareMode == AudioClientShareMode.Exclusive)
            {
                throw new InvalidOperationException("AudioStreamVolume is ONLY supported for shared audio streams.");
            }
            if (audioStreamVolume == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioStreamVolume, out var ptr));
                audioStreamVolume = new AudioStreamVolume(ptr);
            }
            return audioStreamVolume;
        }
    }

    /// <summary>
    /// Returns the SimpleAudioVolume service for this AudioClient — per-session volume and mute, as
    /// shown for the application in the Windows volume mixer. Unlike <see cref="MMDevice"/>-derived
    /// session volume, this is obtained from the client itself, so it works for clients activated
    /// without a concrete endpoint (e.g. automatic stream routing).
    /// </summary>
    public SimpleAudioVolume SimpleAudioVolume
    {
        get
        {
            if (simpleAudioVolume == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_SimpleAudioVolume, out var ptr));
                simpleAudioVolume = new SimpleAudioVolume(ptr);
            }
            return simpleAudioVolume;
        }
    }

    /// <summary>
    /// Gets the AudioClockClient service
    /// </summary>
    public AudioClockClient AudioClockClient
    {
        get
        {
            if (audioClockClient == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioClockClient, out var ptr));
                audioClockClient = new AudioClockClient(ptr);
            }
            return audioClockClient;
        }
    }

    /// <summary>
    /// Gets the AudioRenderClient service
    /// </summary>
    public AudioRenderClient AudioRenderClient
    {
        get
        {
            if (audioRenderClient == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioRenderClient, out var ptr));
                audioRenderClient = new AudioRenderClient(ptr);
            }
            return audioRenderClient;
        }
    }

    /// <summary>
    /// Gets the AudioCaptureClient service
    /// </summary>
    public AudioCaptureClient AudioCaptureClient
    {
        get
        {
            if (audioCaptureClient == null)
            {
                CoreAudioException.ThrowIfFailed(audioClientInterface.GetService(ID_AudioCaptureClient, out var ptr));
                audioCaptureClient = new AudioCaptureClient(ptr);
            }
            return audioCaptureClient;
        }
    }

    /// <summary>
    /// Attempts to get the acoustic echo cancellation (AEC) control for this capture stream,
    /// which lets you set the render endpoint used as the reference stream for echo cancellation.
    /// </summary>
    /// <remarks>
    /// The audio client must be initialized before calling this. AEC itself is performed by an
    /// audio processing object in the capture pipeline; this control only chooses the loopback
    /// reference endpoint. It is available only on Windows 11 build 22621 or later, and only when
    /// the capture endpoint's AEC effect supports controlling the reference endpoint.
    /// </remarks>
    /// <returns>
    /// The <see cref="AcousticEchoCancellationControl"/>, or null if the endpoint does not support
    /// controlling the AEC reference endpoint (GetService returns E_NOINTERFACE).
    /// </returns>
    public AcousticEchoCancellationControl TryGetAcousticEchoCancellationControl()
    {
        if (acousticEchoCancellationControl == null)
        {
            int hr = audioClientInterface.GetService(ID_AcousticEchoCancellationControl, out var ptr);
            if (hr == E_NOINTERFACE)
            {
                return null;
            }
            CoreAudioException.ThrowIfFailed(hr);
            acousticEchoCancellationControl = new AcousticEchoCancellationControl(ptr);
        }
        return acousticEchoCancellationControl;
    }

    /// <summary>
    /// Sets the audio stream category for this client via IAudioClient2. Must be called before
    /// <see cref="Initialize"/>. The category influences stream routing, ducking and — for capture
    /// streams opened as <see cref="AudioStreamCategory.Communications"/> — which audio processing
    /// (such as acoustic echo cancellation) the system applies.
    /// </summary>
    /// <param name="category">The audio stream category to request.</param>
    /// <exception cref="InvalidOperationException">Thrown when the underlying device does not
    /// support IAudioClient2 (for example the process-loopback virtual device).</exception>
    public void SetClientProperties(AudioStreamCategory category)
        => SetClientProperties(category, AudioClientStreamOptions.None);

    /// <summary>
    /// Sets the audio stream category and stream options for this client via IAudioClient2. Must be
    /// called before <see cref="Initialize"/>. Use <see cref="AudioClientStreamOptions.Raw"/> to open a
    /// 'raw' stream that bypasses signal processing (audio enhancements / APO effects) applied by the
    /// system, leaving only endpoint-specific always-on processing in the APO, driver, and hardware.
    /// </summary>
    /// <param name="category">The audio stream category to request.</param>
    /// <param name="options">The stream options to request (e.g. <see cref="AudioClientStreamOptions.Raw"/>).</param>
    /// <exception cref="InvalidOperationException">Thrown when the underlying device does not
    /// support IAudioClient2 (for example the process-loopback virtual device).</exception>
    public void SetClientProperties(AudioStreamCategory category, AudioClientStreamOptions options)
    {
        if (audioClientInterface2 == null)
            throw new InvalidOperationException("Setting client properties requires IAudioClient2, which this device does not support.");

        var properties = new AudioClientProperties
        {
            cbSize = (uint)Marshal.SizeOf<AudioClientProperties>(),
            bIsOffload = 0,
            eCategory = category,
            Options = options
        };
        var propertiesPointer = Marshal.AllocHGlobal(Marshal.SizeOf<AudioClientProperties>());
        try
        {
            Marshal.StructureToPtr(properties, propertiesPointer, false);
            CoreAudioException.ThrowIfFailed(audioClientInterface2.SetClientProperties(propertiesPointer));
        }
        finally
        {
            Marshal.FreeHGlobal(propertiesPointer);
        }
    }

    /// <summary>
    /// Determines whether if the specified output format is supported
    /// </summary>
    /// <param name="shareMode">The share mode.</param>
    /// <param name="desiredFormat">The desired format.</param>
    /// <returns>True if the format is supported</returns>
    public bool IsFormatSupported(AudioClientShareMode shareMode,
        WaveFormat desiredFormat)
    {
        return IsFormatSupported(shareMode, desiredFormat, out _);
    }

    /// <summary>
    /// Determines if the specified output format is supported in shared mode
    /// </summary>
    /// <param name="shareMode">Share Mode</param>
    /// <param name="desiredFormat">Desired Format</param>
    /// <param name="closestMatchFormat">Output The closest match format.</param>
    /// <returns>True if the format is supported</returns>
    public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat, out WaveFormatExtensible closestMatchFormat)
    {
        closestMatchFormat = null;
        var formatPtr = WaveFormat.MarshalToPtr(desiredFormat);
        try
        {
            int hresult = audioClientInterface.IsFormatSupported(shareMode, formatPtr, out var closestMatchPtr);

            if (closestMatchPtr != IntPtr.Zero)
            {
                closestMatchFormat = Marshal.PtrToStructure<WaveFormatExtensible>(closestMatchPtr);
                Marshal.FreeCoTaskMem(closestMatchPtr);
            }

            // S_OK is 0, S_FALSE = 1
            if (hresult == 0)
            {
                return true;
            }
            if (hresult == 1)
            {
                return false;
            }
            if (hresult == AudioClientErrorCode.UnsupportedFormat)
            {
                return false;
            }
            CoreAudioException.ThrowIfFailed(hresult);
            throw new NotSupportedException("Unknown hresult " + hresult);
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }
    }

    /// <summary>
    /// Starts the audio stream
    /// </summary>
    public void Start()
    {
        CoreAudioException.ThrowIfFailed(audioClientInterface.Start());
    }

    /// <summary>
    /// Stops the audio stream.
    /// </summary>
    public void Stop()
    {
        CoreAudioException.ThrowIfFailed(audioClientInterface.Stop());
    }

    /// <summary>
    /// Set the Event Handle for buffer synchro.
    /// </summary>
    /// <param name="eventWaitHandle">The Wait Handle to setup</param>
    public void SetEventHandle(IntPtr eventWaitHandle)
    {
        CoreAudioException.ThrowIfFailed(audioClientInterface.SetEventHandle(eventWaitHandle));
    }

    /// <summary>
    /// Resets the audio stream
    /// Reset is a control method that the client calls to reset a stopped audio stream.
    /// Resetting the stream flushes all pending data and resets the audio clock stream
    /// position to 0. This method fails if it is called on a stream that is not stopped
    /// </summary>
    public void Reset()
    {
        CoreAudioException.ThrowIfFailed(audioClientInterface.Reset());
    }

    // ---- IAudioClient3 low-latency shared mode ----

    /// <summary>
    /// Returns the range of periodicities supported by the engine for the specified stream format.
    /// Requires Windows 10 1607 or later (IAudioClient3).
    /// </summary>
    public AudioClientPeriodInfo GetSharedModeEnginePeriod(WaveFormat format)
    {
        if (audioClientInterface3 == null)
            throw new PlatformNotSupportedException("IAudioClient3 requires Windows 10 version 1607 or later.");

        var formatPtr = WaveFormat.MarshalToPtr(format);
        try
        {
            CoreAudioException.ThrowIfFailed(
                audioClientInterface3.GetSharedModeEnginePeriod(formatPtr,
                    out uint defaultPeriod, out uint fundamentalPeriod,
                    out uint minPeriod, out uint maxPeriod));
            return new AudioClientPeriodInfo(defaultPeriod, fundamentalPeriod, minPeriod, maxPeriod);
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }
    }

    /// <summary>
    /// Initializes a shared audio stream with the specified periodicity.
    /// Requires Windows 10 1607 or later (IAudioClient3).
    /// </summary>
    public void InitializeSharedAudioStream(
        AudioClientStreamFlags streamFlags,
        uint periodInFrames,
        WaveFormat waveFormat,
        Guid audioSessionGuid)
    {
        if (audioClientInterface3 == null)
            throw new PlatformNotSupportedException("IAudioClient3 requires Windows 10 version 1607 or later.");

        this.shareMode = AudioClientShareMode.Shared;
        var formatPtr = WaveFormat.MarshalToPtr(waveFormat);
        try
        {
            CoreAudioException.ThrowIfFailed(
                audioClientInterface3.InitializeSharedAudioStream(streamFlags, periodInFrames, formatPtr, in audioSessionGuid));
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }
        mixFormat = null;
    }

    #region IDisposable Members

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        // Idempotent and safe against concurrent/re-entrant disposal: WASAPI capture
        // and playback wrappers can race a teardown thread against their worker
        // thread's stop path, and a second COM release here previously surfaced as a
        // NullReferenceException out of the interop layer (issue #1183).
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }
        if (audioClientInterface != null)
        {
            audioClockClient?.Dispose();
            audioClockClient = null;
            audioRenderClient?.Dispose();
            audioRenderClient = null;
            audioCaptureClient?.Dispose();
            audioCaptureClient = null;
            audioStreamVolume?.Dispose();
            audioStreamVolume = null;
            simpleAudioVolume?.Dispose();
            simpleAudioVolume = null;
            acousticEchoCancellationControl?.Dispose();
            acousticEchoCancellationControl = null;
            // audioClientInterface2 / audioClientInterface3 are the same ComObject
            // as audioClientInterface (DICASTABLE returns the same wrapper for
            // cross-casts), so a single FinalRelease releases all three views.
            // Releasing each separately would double-free.
            if ((object)audioClientInterface is ComObject co)
            {
                co.FinalRelease();
            }
            audioClientInterface = null;
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}

/// <summary>
/// Information about the audio engine periodicity for shared mode.
/// </summary>
public readonly struct AudioClientPeriodInfo
{
    /// <summary>Default period in frames</summary>
    public uint DefaultPeriodInFrames { get; }
    /// <summary>Fundamental period in frames (all periods must be multiples of this)</summary>
    public uint FundamentalPeriodInFrames { get; }
    /// <summary>Minimum period in frames</summary>
    public uint MinPeriodInFrames { get; }
    /// <summary>Maximum period in frames</summary>
    public uint MaxPeriodInFrames { get; }

    /// <summary>
    /// Creates a new AudioClientPeriodInfo
    /// </summary>
    public AudioClientPeriodInfo(uint defaultPeriod, uint fundamentalPeriod, uint minPeriod, uint maxPeriod)
    {
        DefaultPeriodInFrames = defaultPeriod;
        FundamentalPeriodInFrames = fundamentalPeriod;
        MinPeriodInFrames = minPeriod;
        MaxPeriodInFrames = maxPeriod;
    }

    /// <summary>
    /// Picks the lowest engine period that satisfies the IAudioClient3 constraints: an integral
    /// multiple of the fundamental period, no smaller than the minimum and no larger than the maximum.
    /// This is the period to pass to <see cref="AudioClient.InitializeSharedAudioStream"/> for the
    /// lowest achievable shared-mode latency.
    /// </summary>
    public uint ChooseLowestLatencyPeriod()
    {
        var period = MinPeriodInFrames;
        if (FundamentalPeriodInFrames > 0 && period % FundamentalPeriodInFrames != 0)
        {
            // Round up to the next multiple of the fundamental period.
            period = (period / FundamentalPeriodInFrames + 1) * FundamentalPeriodInFrames;
            if (period > MaxPeriodInFrames)
                period = MaxPeriodInFrames;
        }
        return period;
    }
}
