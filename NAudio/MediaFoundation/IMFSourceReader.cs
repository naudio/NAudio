using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

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
        void SetCurrentPosition([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidTimeFormat, [In] ref PropVariant varPosition);
        /// <summary>
        /// Reads the next sample from the media source.
        /// </summary>
        void ReadSample([In] int dwStreamIndex, [In] int dwControlFlags, [Out] out int pdwActualStreamIndex, [Out] out int pdwStreamFlags,
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
        int GetPresentationAttribute([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidAttribute, [Out] out PropVariant pvarAttribute);
    }
}