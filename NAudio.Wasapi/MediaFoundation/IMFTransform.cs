using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFTransform, defined in mftransform.h
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("bf94c121-5b05-4e6f-8000-ba598961414d")]
    internal interface IMFTransform
    {
        /// <summary>
        /// Retrieves the minimum and maximum number of input and output streams.
        /// </summary>
        void GetStreamLimits([Out] out int pdwInputMinimum, [Out] out int pdwInputMaximum, [Out] out int pdwOutputMinimum, [Out] out int pdwOutputMaximum);

        /// <summary>
        /// Retrieves the current number of input and output streams on this MFT.
        /// </summary>
        void GetStreamCount([Out] out int pcInputStreams, [Out] out int pcOutputStreams);

        /// <summary>
        /// Retrieves the stream identifiers for the input and output streams on this MFT.
        /// </summary>
        void GetStreamIds([In] int dwInputIdArraySize, [In, Out] IntPtr pdwInputIDs, [In] int dwOutputIdArraySize, [In, Out] IntPtr pdwOutputIDs);

        /// <summary>
        /// Gets the buffer requirements and other information for an input stream on this MFT.
        /// </summary>
        void GetInputStreamInfo([In] int dwInputStreamId, [Out] out MftInputStreamInfo pStreamInfo);

        /// <summary>
        /// Gets the buffer requirements and other information for an output stream on this MFT.
        /// </summary>
        void GetOutputStreamInfo([In] int dwOutputStreamId, [Out] out MftOutputStreamInfo pStreamInfo);

        /// <summary>
        /// Gets the global attribute store for this MFT.
        /// </summary>
        void GetAttributes([Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Retrieves the attribute store for an input stream on this MFT.
        /// </summary>
        void GetInputStreamAttributes([In] int dwInputStreamId, [Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Retrieves the attribute store for an output stream on this MFT.
        /// </summary>
        void GetOutputStreamAttributes([In] int dwOutputStreamId, [Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Removes an input stream from this MFT.
        /// </summary>
        void DeleteInputStream([In] int dwOutputStreamId);

        /// <summary>
        /// Adds one or more new input streams to this MFT.
        /// </summary>
        void AddInputStreams([In] int cStreams, [In] IntPtr adwStreamIDs);

        /// <summary>
        /// Gets an available media type for an input stream on this MFT.
        /// </summary>
        void GetInputAvailableType([In] int dwInputStreamId, [In] int dwTypeIndex, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Retrieves an available media type for an output stream on this MFT.
        /// </summary>
        void GetOutputAvailableType([In] int dwOutputStreamId, [In] int dwTypeIndex, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Sets, tests, or clears the media type for an input stream on this MFT.
        /// </summary>
        void SetInputType([In] int dwInputStreamId, [In] IMFMediaType pType, [In] MftSetTypeFlags dwFlags);

        /// <summary>
        /// Sets, tests, or clears the media type for an output stream on this MFT.
        /// </summary>
        void SetOutputType([In] int dwOutputStreamId, [In] IMFMediaType pType, [In] MftSetTypeFlags dwFlags);

        /// <summary>
        /// Gets the current media type for an input stream on this MFT.
        /// </summary>
        void GetInputCurrentType([In] int dwInputStreamId, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Gets the current media type for an output stream on this MFT.
        /// </summary>
        void GetOutputCurrentType([In] int dwOutputStreamId, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Queries whether an input stream on this MFT can accept more data.
        /// </summary>
        void GetInputStatus([In] int dwInputStreamId, [Out] out MftInputStatusFlags pdwFlags);

        /// <summary>
        /// Queries whether the MFT is ready to produce output data.
        /// </summary>
        void GetOutputStatus([In] int dwInputStreamId, [Out] out MftOutputStatusFlags pdwFlags);

        /// <summary>
        /// Sets the range of time stamps the client needs for output.
        /// </summary>
        void SetOutputBounds([In] long hnsLowerBound, [In] long hnsUpperBound);

        /// <summary>
        /// Sends an event to an input stream on this MFT.
        /// </summary>
        void ProcessEvent([In] int dwInputStreamId, [In] IMFMediaEvent pEvent);

        /// <summary>
        /// Sends a message to the MFT.
        /// </summary>
        void ProcessMessage([In] MftMessageType eMessage, [In] IntPtr ulParam);

        /// <summary>
        /// Delivers data to an input stream on this MFT.
        /// </summary>
        void ProcessInput([In] int dwInputStreamId, [In] IMFSample pSample, int dwFlags);

        /// <summary>
        /// Generates output from the current input data.
        /// </summary>
        [PreserveSig]
        int ProcessOutput([In] MftProcessOutputFlags dwFlags,
                           [In] int cOutputBufferCount,
                           [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] MftOutputDataBuffer[] pOutputSamples,
                           [Out] out MftProcessOutputStatus pdwStatus);
    }
}
