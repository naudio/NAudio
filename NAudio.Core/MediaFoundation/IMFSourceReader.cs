using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFSourceReader interface
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd374655%28v=vs.85%29.aspx
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
    public interface IMFSourceReader
    {
        /// <summary>
        /// Queries whether a stream is selected.
        /// </summary>
        void GetStreamSelection([In] int dwStreamIndex, [Out, MarshalAs(UnmanagedType.Bool)] out bool pSelected);
        /// <summary>
        /// Selects or deselects one or more streams.
        /// </summary>
        void SetStreamSelection([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.Bool)] bool pSelected);
        /// <summary>
        /// Gets a format that is supported natively by the media source.
        /// </summary>
        void GetNativeMediaType([In] int dwStreamIndex, [In] int dwMediaTypeIndex, [Out] out IMFMediaType ppMediaType);
        /// <summary>
        /// Gets the current media type for a stream.
        /// </summary>
        void GetCurrentMediaType([In] int dwStreamIndex, [Out] out IMFMediaType ppMediaType);
        /// <summary>
        /// Sets the media type for a stream.
        /// </summary>
        void SetCurrentMediaType([In] int dwStreamIndex, IntPtr pdwReserved, [In] IMFMediaType pMediaType);
        /// <summary>
        /// Seeks to a new position in the media source.
        /// </summary>
        void SetCurrentPosition([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidTimeFormat, [In] IntPtr varPosition);
        /// <summary>
        /// Reads the next sample from the media source.
        /// </summary>
        void ReadSample([In] int dwStreamIndex, [In] int dwControlFlags, [Out] out int pdwActualStreamIndex, [Out] out MF_SOURCE_READER_FLAG pdwStreamFlags,
                        [Out] out UInt64 pllTimestamp, [Out] out IMFSample ppSample);
        /// <summary>
        /// Flushes one or more streams.
        /// </summary>
        void Flush([In] int dwStreamIndex);

        /// <summary>
        /// Queries the underlying media source or decoder for an interface.
        /// </summary>
        void GetServiceForStream([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidService,
                                 [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out] out IntPtr ppvObject);

        /// <summary>
        /// Gets an attribute from the underlying media source.
        /// </summary>
        [PreserveSig]
        int GetPresentationAttribute([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidAttribute, [Out] IntPtr pvarAttribute);
    }

    /// <summary>
    /// Contains flags that indicate the status of the IMFSourceReader::ReadSample method
    /// http://msdn.microsoft.com/en-us/library/windows/desktop/dd375773(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum MF_SOURCE_READER_FLAG { 
        /// <summary>
        /// No Error
        /// </summary>
        None = 0,
        /// <summary>
        /// An error occurred. If you receive this flag, do not make any further calls to IMFSourceReader methods.
        /// </summary>
        MF_SOURCE_READERF_ERROR                    = 0x00000001,
        /// <summary>
        /// The source reader reached the end of the stream.
        /// </summary>
        MF_SOURCE_READERF_ENDOFSTREAM              = 0x00000002,
        /// <summary>
        /// One or more new streams were created
        /// </summary>
        MF_SOURCE_READERF_NEWSTREAM                = 0x00000004,
        /// <summary>
        /// The native format has changed for one or more streams. The native format is the format delivered by the media source before any decoders are inserted.
        /// </summary>
        MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED   = 0x00000010,
        /// <summary>
        /// The current media has type changed for one or more streams. To get the current media type, call the IMFSourceReader::GetCurrentMediaType method.
        /// </summary>
        MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED  = 0x00000020,
        /// <summary>
        /// There is a gap in the stream. This flag corresponds to an MEStreamTick event from the media source.
        /// </summary>
        MF_SOURCE_READERF_STREAMTICK               = 0x00000100,
        /// <summary>
        /// All transforms inserted by the application have been removed for a particular stream.
        /// </summary>
        MF_SOURCE_READERF_ALLEFFECTSREMOVED        = 0x00000200
    }
}