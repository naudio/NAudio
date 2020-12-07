using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Implemented by the Microsoft Media Foundation sink writer object.
    /// </summary>
    [ComImport, Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMFSinkWriter
    {
        /// <summary>
        /// Adds a stream to the sink writer.
        /// </summary>
        void AddStream([In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pTargetMediaType, out int pdwStreamIndex);
        /// <summary>
        /// Sets the input format for a stream on the sink writer.
        /// </summary>
        void SetInputMediaType([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFMediaType pInputMediaType, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pEncodingParameters);
        /// <summary>
        /// Initializes the sink writer for writing.
        /// </summary>
        void BeginWriting();
        /// <summary>
        /// Delivers a sample to the sink writer.
        /// </summary>
        void WriteSample([In] int dwStreamIndex, [In, MarshalAs(UnmanagedType.Interface)] IMFSample pSample);
        /// <summary>
        /// Indicates a gap in an input stream.
        /// </summary>
        void SendStreamTick([In] int dwStreamIndex, [In] long llTimestamp);
        /// <summary>
        /// Places a marker in the specified stream.
        /// </summary>
        void PlaceMarker([In] int dwStreamIndex, [In] IntPtr pvContext);
        /// <summary>
        /// Notifies the media sink that a stream has reached the end of a segment.
        /// </summary>
        void NotifyEndOfSegment([In] int dwStreamIndex);
        /// <summary>
        /// Flushes one or more streams.
        /// </summary>
        void Flush([In] int dwStreamIndex);
        /// <summary>
        /// (Finalize) Completes all writing operations on the sink writer.
        /// </summary>
        void DoFinalize();
        /// <summary>
        /// Queries the underlying media sink or encoder for an interface.
        /// </summary>
        void GetServiceForStream([In] int dwStreamIndex, [In] ref Guid guidService, [In] ref Guid riid, out IntPtr ppvObject);
        /// <summary>
        /// Gets statistics about the performance of the sink writer.
        /// </summary>
        void GetStatistics([In] int dwStreamIndex, [In, Out] MF_SINK_WRITER_STATISTICS pStats);
    }
}
