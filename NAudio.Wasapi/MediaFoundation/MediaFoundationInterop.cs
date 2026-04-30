using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
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
        /// Creates a Microsoft Media Foundation byte stream that wraps an IStream object.
        /// </summary>
        /// <remarks>
        /// The classic MFCreateMFByteStreamOnStream P/Invoke took a typed [In] IStream parameter,
        /// which under BuiltInComInteropSupport=false cannot be marshalled. The modern shape takes
        /// the QI'd IStream pointer the caller has already produced via ComWrappers; see
        /// MediaFoundationApi.CreateByteStream and the Phase 2f QI-for-IID pattern.
        /// </remarks>
        [LibraryImport("mfplat.dll")]
        public static partial int MFCreateMFByteStreamOnStream(IntPtr punkStream, out IntPtr ppByteStream);

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


    }
}
