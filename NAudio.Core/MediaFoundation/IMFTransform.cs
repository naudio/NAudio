using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFTransform, defined in mftransform.h
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("bf94c121-5b05-4e6f-8000-ba598961414d")]
    public interface IMFTransform
    {
        /// <summary>
        /// Retrieves the minimum and maximum number of input and output streams.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetStreamLimits( 
        ///     /* [out] */ __RPC__out DWORD *pdwInputMinimum,
        ///     /* [out] */ __RPC__out DWORD *pdwInputMaximum,
        ///     /* [out] */ __RPC__out DWORD *pdwOutputMinimum,
        ///     /* [out] */ __RPC__out DWORD *pdwOutputMaximum) = 0;
        /// </remarks>
        void GetStreamLimits([Out] out int pdwInputMinimum, [Out] out int pdwInputMaximum, [Out] out int pdwOutputMinimum, [Out] out int pdwOutputMaximum);

        /// <summary>
        /// Retrieves the current number of input and output streams on this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetStreamCount( 
        ///     /* [out] */ __RPC__out DWORD *pcInputStreams,
        ///     /* [out] */ __RPC__out DWORD *pcOutputStreams) = 0;
        /// </remarks>
        void GetStreamCount([Out] out int pcInputStreams, [Out] out int pcOutputStreams);

        /// <summary>
        /// Retrieves the stream identifiers for the input and output streams on this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetStreamIDs( 
        ///     DWORD dwInputIDArraySize,
        ///     /* [size_is][out] */ __RPC__out_ecount_full(dwInputIDArraySize) DWORD *pdwInputIDs,
        ///     DWORD dwOutputIDArraySize,
        ///     /* [size_is][out] */ __RPC__out_ecount_full(dwOutputIDArraySize) DWORD *pdwOutputIDs) = 0;
        /// </remarks>
        void GetStreamIds([In] int dwInputIdArraySize, [In, Out] IntPtr pdwInputIDs, [In] int dwOutputIdArraySize, [In, Out] IntPtr pdwOutputIDs);

        /// <summary>
        /// Gets the buffer requirements and other information for an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetInputStreamInfo( 
        ///     DWORD dwInputStreamID,
        ///     /* [out] */ __RPC__out MFT_INPUT_STREAM_INFO *pStreamInfo) = 0;
        /// </remarks>
        void GetInputStreamInfo([In] int dwInputStreamId, [Out] out MFT_INPUT_STREAM_INFO pStreamInfo);

        /// <summary>
        /// Gets the buffer requirements and other information for an output stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetOutputStreamInfo( 
        ///     DWORD dwOutputStreamID,
        ///     /* [out] */ __RPC__out MFT_OUTPUT_STREAM_INFO *pStreamInfo) = 0;
        /// </remarks>
        void GetOutputStreamInfo([In] int dwOutputStreamId, [Out] out MFT_OUTPUT_STREAM_INFO pStreamInfo);

        /// <summary>
        /// Gets the global attribute store for this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        ///  virtual HRESULT STDMETHODCALLTYPE GetAttributes( 
        ///     /* [out] */ __RPC__deref_out_opt IMFAttributes **pAttributes) = 0;
        /// </remarks>
        void GetAttributes([Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Retrieves the attribute store for an input stream on this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetInputStreamAttributes( 
        ///     DWORD dwInputStreamID,
        ///     /* [out] */ __RPC__deref_out_opt IMFAttributes **pAttributes) = 0;
        /// </remarks>
        void GetInputStreamAttributes([In] int dwInputStreamId, [Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Retrieves the attribute store for an output stream on this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetOutputStreamAttributes( 
        ///     DWORD dwOutputStreamID,
        ///     /* [out] */ __RPC__deref_out_opt IMFAttributes **pAttributes) = 0;
        /// </remarks>
        void GetOutputStreamAttributes([In] int dwOutputStreamId, [Out] out IMFAttributes pAttributes);

        /// <summary>
        /// Removes an input stream from this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE DeleteInputStream( 
        ///     DWORD dwStreamID) = 0;
        /// </remarks>
        void DeleteInputStream([In] int dwOutputStreamId);

        /// <summary>
        /// Adds one or more new input streams to this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE AddInputStreams( 
        ///     DWORD cStreams,
        ///     /* [in] */ __RPC__in DWORD *adwStreamIDs) = 0;
        /// </remarks>
        void AddInputStreams([In] int cStreams, [In] IntPtr adwStreamIDs);

        /// <summary>
        /// Gets an available media type for an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetInputAvailableType( 
        ///     DWORD dwInputStreamID,
        ///     DWORD dwTypeIndex,
        ///     /* [out] */ __RPC__deref_out_opt IMFMediaType **ppType) = 0;
        /// </remarks>
        void GetInputAvailableType([In] int dwInputStreamId, [In] int dwTypeIndex, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Retrieves an available media type for an output stream on this MFT.
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetOutputAvailableType( 
        ///     DWORD dwOutputStreamID,
        ///     DWORD dwTypeIndex,
        ///     /* [out] */ __RPC__deref_out_opt IMFMediaType **ppType) = 0;
        /// </remarks>
        void GetOutputAvailableType([In] int dwOutputStreamId, [In] int dwTypeIndex, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Sets, tests, or clears the media type for an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE SetInputType( 
        ///     DWORD dwInputStreamID,
        ///     /* [in] */ __RPC__in_opt IMFMediaType *pType,
        ///     DWORD dwFlags) = 0;
        /// </remarks>
        void SetInputType([In] int dwInputStreamId, [In] IMFMediaType pType, [In] _MFT_SET_TYPE_FLAGS dwFlags);

        /// <summary>
        /// Sets, tests, or clears the media type for an output stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE SetOutputType( 
        ///     DWORD dwOutputStreamID,
        ///     /* [in] */ __RPC__in_opt IMFMediaType *pType,
        ///     DWORD dwFlags) = 0;
        /// </remarks>
        void SetOutputType([In] int dwOutputStreamId, [In] IMFMediaType pType, [In] _MFT_SET_TYPE_FLAGS dwFlags);

        /// <summary>
        /// Gets the current media type for an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetInputCurrentType( 
        ///     DWORD dwInputStreamID,
        ///     /* [out] */ __RPC__deref_out_opt IMFMediaType **ppType) = 0;
        /// </remarks>
        void GetInputCurrentType([In] int dwInputStreamId, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Gets the current media type for an output stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetOutputCurrentType( 
        ///     DWORD dwOutputStreamID,
        ///     /* [out] */ __RPC__deref_out_opt IMFMediaType **ppType) = 0;
        /// </remarks>
        void GetOutputCurrentType([In] int dwOutputStreamId, [Out] out IMFMediaType ppType);

        /// <summary>
        /// Queries whether an input stream on this Media Foundation transform (MFT) can accept more data. 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetInputStatus( 
        ///     DWORD dwInputStreamID,
        ///     /* [out] */ __RPC__out DWORD *pdwFlags) = 0;
        /// </remarks>
        void GetInputStatus([In] int dwInputStreamId, [Out] out _MFT_INPUT_STATUS_FLAGS pdwFlags);

        /// <summary>
        /// Queries whether the Media Foundation transform (MFT) is ready to produce output data. 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE GetOutputStatus( 
        ///     /* [out] */ __RPC__out DWORD *pdwFlags) = 0;
        /// </remarks>
        void GetOutputStatus([In] int dwInputStreamId, [Out] out _MFT_OUTPUT_STATUS_FLAGS pdwFlags);

        /// <summary>
        /// Sets the range of time stamps the client needs for output. 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE SetOutputBounds( 
        ///     LONGLONG hnsLowerBound,
        ///     LONGLONG hnsUpperBound) = 0;
        /// </remarks>
        void SetOutputBounds([In] long hnsLowerBound, [In] long hnsUpperBound);

        /// <summary>
        /// Sends an event to an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE ProcessEvent( 
        ///     DWORD dwInputStreamID,
        ///     /* [in] */ __RPC__in_opt IMFMediaEvent *pEvent) = 0;
        /// </remarks>
        void ProcessEvent([In] int dwInputStreamId, [In] IMFMediaEvent pEvent);

        /// <summary>
        /// Sends a message to the Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual HRESULT STDMETHODCALLTYPE ProcessMessage( 
        ///     MFT_MESSAGE_TYPE eMessage,
        ///     ULONG_PTR ulParam) = 0;
        /// </remarks>
        void ProcessMessage([In] MFT_MESSAGE_TYPE eMessage, [In] IntPtr ulParam);

        /// <summary>
        /// Delivers data to an input stream on this Media Foundation transform (MFT). 
        /// </summary>
        /// <remarks>
        /// virtual /* [local] */ HRESULT STDMETHODCALLTYPE ProcessInput( 
        ///     DWORD dwInputStreamID,
        ///     IMFSample *pSample,
        ///     DWORD dwFlags) = 0;
        /// </remarks>
        void ProcessInput([In] int dwInputStreamId, [In] IMFSample pSample, int dwFlags);

        /// <summary>
        /// Generates output from the current input data. 
        /// </summary>
        /// <remarks>
        /// virtual /* [local] */ HRESULT STDMETHODCALLTYPE ProcessOutput( 
        ///     DWORD dwFlags,
        ///     DWORD cOutputBufferCount,
        ///     /* [size_is][out][in] */ MFT_OUTPUT_DATA_BUFFER *pOutputSamples,
        ///     /* [out] */ DWORD *pdwStatus) = 0;
        /// </remarks>
        [PreserveSig]
        int ProcessOutput([In] _MFT_PROCESS_OUTPUT_FLAGS dwFlags, 
                           [In] int cOutputBufferCount,
                           [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] MFT_OUTPUT_DATA_BUFFER[] pOutputSamples,
                           [Out] out _MFT_PROCESS_OUTPUT_STATUS pdwStatus);
    }
}
