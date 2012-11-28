using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Interop definitions for MediaFoundation
    /// thanks to Lucian Wischik for the initial work on many of these definitions (also various interfaces)
    /// </summary>
    public static class MediaFoundationInterop
    {
        /// <summary>
        /// Initializes Microsoft Media Foundation.
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFStartup(int version, int dwFlags = 0);

        /// <summary>
        /// Shuts down the Microsoft Media Foundation platform
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFShutdown();

        /// <summary>
        /// Creates an empty media type.
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateMediaType(out IMFMediaType ppMFType);

        /// <summary>
        /// Converts a Media Foundation audio media type to a WAVEFORMATEX structure.
        /// </summary>
        /// TODO: try making second parameter out WaveFormatExtraData
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateWaveFormatExFromMFMediaType(IMFMediaType pMFType, ref IntPtr ppWF, ref int pcbSize, int flags = 0);

        /// <summary>
        /// Creates the source reader from a URL.
        /// </summary>
        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSourceReaderFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] IMFAttributes pAttributes,
                                                                [Out, MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

        /// <summary>
        /// Creates the source reader from a byte stream.
        /// </summary>
        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSourceReaderFromByteStream(IMFByteStream pByteStream, [In] IMFAttributes pAttributes, ref IMFSourceReader ppSourceReader);

        /// <summary>
        /// Creates the sink writer from a URL or byte stream.
        /// </summary>
        [DllImport("mfreadwrite.dll", ExactSpelling = true)]
        public static extern int MFCreateSinkWriterFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszOutputURL,
                                                           [In] IMFByteStream pByteStream, [In] IMFAttributes pAttributes, [Out] out IMFSinkWriter ppSinkWriter);

        /// <summary>
        /// Creates a Microsoft Media Foundation byte stream that wraps an IRandomAccessStream object.
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateMFByteStreamOnStreamEx([MarshalAs(UnmanagedType.IUnknown)] object punkStream, ref IMFByteStream ppByteStream);

        /// <summary>
        /// Gets a list of Microsoft Media Foundation transforms (MFTs) that match specified search criteria. 
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFTEnumEx([In] Guid guidCategory, [In] _MFT_ENUM_FLAG flags, [In] MFT_REGISTER_TYPE_INFO pInputType, [In] MFT_REGISTER_TYPE_INFO pOutputType,
                                            [Out] out IntPtr pppMFTActivate, [Out] out int pcMFTActivate);

        /// <summary>
        /// Creates an empty media sample.
        /// </summary>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSample(out IMFSample ppIMFSample);

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
        
        /// <summary>
        /// Media type Major Type
        /// </summary>
        public static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        /// <summary>
        /// Media Type subtype
        /// </summary>
        public static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        /// <summary>
        /// Audio block alignment
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_BLOCK_ALIGNMENT = new Guid("322de230-9eeb-43bd-ab7a-ff412251541d");
        /// <summary>
        /// Audio average bytes per second
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_AVG_BYTES_PER_SECOND = new Guid("1aab75c8-cfef-451c-ab95-ac034b8e1731");
        /// <summary>
        /// Audio number of channels
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_NUM_CHANNELS = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a");
        /// <summary>
        /// Audio samples per second
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_SAMPLES_PER_SECOND = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
        /// <summary>
        /// Audio bits per sample
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_BITS_PER_SAMPLE = new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");

    }
}
