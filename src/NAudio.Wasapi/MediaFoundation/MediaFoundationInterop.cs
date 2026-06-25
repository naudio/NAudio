using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.MediaFoundation;

/// <summary>
/// Interop definitions for MediaFoundation
/// thanks to Lucian Wischik for the initial work on many of these definitions (also various interfaces)
/// n.b. the goal is to make as much of this internal as possible, and provide
/// better .NET APIs using the MediaFoundationApi class instead
/// </summary>
/// <remarks>
/// Blittable signatures use <see cref="LibraryImportAttribute"/> for source-generated
/// p/invoke stubs (smaller AOT footprint, no JIT-time stub generation, fully-static
/// trim-analyzer view). The two signatures that still take managed reference-type
/// parameters (<see cref="MFInitMediaTypeFromWaveFormatEx"/> with <c>WaveFormat</c>,
/// <see cref="MFTEnumEx"/> with <c>MftRegisterTypeInfo</c>) remain on
/// <see cref="DllImportAttribute"/> until those types get blittable representations
/// or custom marshallers — non-trivial design work, not a mechanical conversion.
///
/// All <c>[LibraryImport]</c> declarations return <c>int</c> (HRESULT). Callers must
/// wrap with <c>MediaFoundationException.ThrowIfFailed(...)</c> — this replaces the
/// previous <c>PreserveSig=false</c> auto-throw behaviour of <c>[DllImport]</c>
/// (which is unavailable under <c>[LibraryImport]</c>). <c>MediaFoundationException</c>
/// derives from <c>COMException</c>, so existing <c>catch (COMException)</c> blocks
/// keep working.
/// </remarks>
internal static partial class MediaFoundationInterop
{
    /// <summary>
    /// Initializes Microsoft Media Foundation.
    /// </summary>
    [LibraryImport("mfplat.dll")]
    public static partial int MFStartup(int version, int dwFlags = 0);

    /// <summary>
    /// Shuts down the Microsoft Media Foundation platform
    /// </summary>
    [LibraryImport("mfplat.dll")]
    public static partial int MFShutdown();

    /// <summary>
    /// Creates an empty media type.
    /// </summary>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateMediaType(out IntPtr ppMFType);

    /// <summary>
    /// Initializes a media type from a WAVEFORMATEX structure.
    /// </summary>
    /// <remarks>
    /// Stays on <c>[DllImport]</c> because <c>WaveFormat</c> is a managed reference
    /// type. Migrating to <c>[LibraryImport]</c> requires either a blittable
    /// <c>WaveFormatEx</c> struct or a custom marshaller — design discussion
    /// pending (see follow-up note in MODERNIZATION.md).
    /// </remarks>
    [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
    internal static extern void MFInitMediaTypeFromWaveFormatEx([In] IntPtr pMFType, [In] WaveFormat pWaveFormat, [In] int cbBufSize);

    /// <summary>
    /// Converts a Media Foundation audio media type to a WAVEFORMATEX structure.
    /// </summary>
    /// TODO: try making second parameter out WaveFormatExtraData
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateWaveFormatExFromMFMediaType(IntPtr pMFType, ref IntPtr ppWF, ref int pcbSize, int flags = 0);

    /// <summary>
    /// Creates the source reader from a URL.
    /// </summary>
    [LibraryImport("mfreadwrite.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MFCreateSourceReaderFromURL(string pwszURL, IntPtr pAttributes,
                                                          out IntPtr ppSourceReader);

    /// <summary>
    /// Creates the source reader from a byte stream.
    /// </summary>
    [LibraryImport("mfreadwrite.dll")]
    public static partial int MFCreateSourceReaderFromByteStream(IntPtr pByteStream, IntPtr pAttributes, out IntPtr ppSourceReader);

    /// <summary>
    /// Creates the sink writer from a URL or byte stream.
    /// </summary>
    /// <remarks>
    /// <paramref name="pwszOutputURL"/> may be <c>null</c> when supplying a byte stream;
    /// the source-generated stub passes a null pointer to native in that case.
    /// </remarks>
    [LibraryImport("mfreadwrite.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MFCreateSinkWriterFromURL(string pwszOutputURL,
                                                        IntPtr pByteStream, IntPtr pAttributes, out IntPtr ppSinkWriter);

    /// <summary>
    /// Gets a list of Microsoft Media Foundation transforms (MFTs) that match specified search criteria.
    /// </summary>
    /// <remarks>
    /// Stays on <c>[DllImport]</c> because <c>MftRegisterTypeInfo</c> is a managed
    /// reference type. Same rationale as <see cref="MFInitMediaTypeFromWaveFormatEx"/>.
    /// </remarks>
    [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
    public static extern void MFTEnumEx([In] Guid guidCategory, [In] MftEnumFlags flags, [In] MftRegisterTypeInfo pInputType, [In] MftRegisterTypeInfo pOutputType,
                                        out IntPtr pppMFTActivate, out int pcMFTActivate);

    /// <summary>
    /// Creates an empty media sample.
    /// </summary>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateSample(out IntPtr ppIMFSample);

    /// <summary>
    /// Allocates system memory and creates a media buffer to manage it.
    /// </summary>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateMemoryBuffer(int cbMaxLength, out IntPtr ppBuffer);

    /// <summary>
    /// Creates an empty attribute store.
    /// </summary>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateAttributes(out IntPtr ppMFAttributes, int cInitialSize);

    /// <summary>
    /// Gets a list of output formats from an audio encoder.
    /// </summary>
    [LibraryImport("mf.dll")]
    public static partial int MFTranscodeGetAudioOutputAvailableTypes(
        in Guid guidSubType,
        MftEnumFlags dwMFTFlags,
        IntPtr pCodecConfig,
        out IntPtr ppAvailableTypes);

    /// <summary>
    /// Creates a new Media Foundation work queue of the specified type.
    /// </summary>
    /// <param name="type">
    /// The type of work queue to create. 
    /// See the <see cref="WorkQueueType"/> enumeration for more information.
    /// </param>
    /// <param name="workQueueToken">The allocated work queue token.</param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFAllocateWorkQueueEx(WorkQueueType type, out uint workQueueToken); // returns: HRESULT

    /// <summary>
    /// Destroys a previously created Media Foundation work queue.
    /// </summary>
    /// <param name="dwQueue">The token that represents the work queue to destroy.</param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFUnlockWorkQueue(uint dwQueue);

    /// <summary>
    /// Puts a work queue item to the specified queue. <br />
    /// This function accepts an <see cref="Interfaces.IMFAsyncCallback"/> object,
    /// and a state object to use.
    /// </summary>
    /// <param name="dwQueue">The work queue where to put the work queue item.</param>
    /// <param name="callbackPtr">
    /// The <see cref="Interfaces.IMFAsyncCallback"/> object 
    /// that is the callback to be called by the work queue.
    /// </param>
    /// <param name="statePtr">
    /// Optional. 
    /// An object inheriting from <c>IUnknown</c> providing state.
    /// </param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFPutWorkItem(uint dwQueue, IntPtr callbackPtr, IntPtr statePtr); // returns: HRESULT

    /// <summary>
    /// Invokes the specified callback to any of the Media Foundation work queues.
    /// </summary>
    /// <param name="resultPtr">
    /// The <see cref="Interfaces.IMFAsyncResult"/>
    /// object that is the callback to be invoked.
    /// </param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFInvokeCallback(IntPtr resultPtr);

    /// <summary>
    /// Puts a work queue item to the specified queue. <br />
    /// This function accepts an <see cref="Interfaces.IMFAsyncResult"/> object, 
    /// created via the <see cref="MFCreateAsyncResult"/> function.
    /// </summary>
    /// <remarks>
    /// Creating a custom implementation of the <see cref="Interfaces.IMFAsyncResult"/> 
    /// and passing that here will not work because all work queues depend on the 
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/mfapi/ns-mfapi-mfasyncresult">MFASYNCRESULT</see>
    /// structure. But it does not make really sense why to create a custom IMFAsyncResult implementation.
    /// </remarks>
    /// <param name="dwQueue">The work queue where to put the work queue item.</param>
    /// <param name="resultPtr">
    /// The <see cref="Interfaces.IMFAsyncResult"/> object 
    /// that is the callback to be called by the work queue.
    /// </param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFPutWorkItemEx(uint dwQueue, IntPtr resultPtr);

    /// <summary>
    /// Creates a new instance of the <see cref="Interfaces.IMFAsyncResult"/> object,
    /// that is compatible with the Media Foundation's work queues.
    /// </summary>
    /// <param name="ptrObject">
    /// Optional. 
    /// An object inheriting from <c>IUnknown</c> providing application-defined data.
    /// </param>
    /// <param name="callbackPtr">
    /// An instance of the <see cref="Interfaces.IMFAsyncCallback"/> 
    /// which defines which callback to call.
    /// </param>
    /// <param name="ptrState">
    /// Optional.
    /// An object inheriting from <c>IUnknown</c> providing state.
    /// </param>
    /// <param name="asyncResultPtr">
    /// If a call of this function succeeds, this parameter contains 
    /// the created <see cref="Interfaces.IMFAsyncResult"/> object.
    /// </param>
    /// <returns><c>HRESULT</c> indicating success or failure.</returns>
    [LibraryImport("mfplat.dll")]
    internal static partial int MFCreateAsyncResult(IntPtr ptrObject, IntPtr callbackPtr, IntPtr ptrState, out IntPtr asyncResultPtr);

    /// <summary>
    /// All streams
    /// </summary>
    public const int MF_SOURCE_READER_ALL_STREAMS = unchecked((int)0xFFFFFFFE);
    /// <summary>
    /// First audio stream
    /// </summary>
    public const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = unchecked((int)0xFFFFFFFD);
    /// <summary>
    /// First video stream
    /// </summary>
    public const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xFFFFFFFC);
    /// <summary>
    /// Media source
    /// </summary>
    public const int MF_SOURCE_READER_MEDIASOURCE = unchecked((int)0xFFFFFFFF);
    /// <summary>
    /// Media Foundation SDK Version
    /// </summary>
    public const int MF_SDK_VERSION = 0x2;
    /// <summary>
    /// Media Foundation API Version
    /// </summary>
    public const int MF_API_VERSION = 0x70;
    /// <summary>
    /// Media Foundation Version
    /// </summary>
    public const int MF_VERSION = (MF_SDK_VERSION << 16) | MF_API_VERSION;

    #region Common work queue tokens

    /// <summary>
    /// In most cases, applications should use <see cref="MFASYNC_CALLBACK_QUEUE_MULTITHREADED"/>. <br />
    /// This work queue is used for synchronous operations.Using the standard work queue may run the risk of deadlocking. <br />
    /// Applications can create a private synchronous queue on top of the multithreaded queue by using <see href="https://learn.microsoft.com/en-us/windows/desktop/api/mfapi/nf-mfapi-mfallocateserialworkqueue">MFAllocateSerialWorkQueue</see>.
    /// </summary>
    public const uint MFASYNC_CALLBACK_QUEUE_STANDARD = 0x00000001;

    /// <summary>
    /// This multithreaded work queue should be used in most cases. <br />
    /// This work queue is used for asynchronous operations throughout Media Foundation.
    /// </summary>
    public const uint MFASYNC_CALLBACK_QUEUE_MULTITHREADED = 0x00000005;

    /// <summary>Undefined work queue.</summary>
    public const uint MFASYNC_CALLBACK_QUEUE_UNDEFINED = 0x00000000;

    #endregion
}
