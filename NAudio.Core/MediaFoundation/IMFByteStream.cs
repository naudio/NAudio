using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// IMFByteStream 
    /// http://msdn.microsoft.com/en-gb/library/windows/desktop/ms698720%28v=vs.85%29.aspx
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("ad4c1b00-4bf7-422f-9175-756693d9130d")]
    public interface IMFByteStream
    {
        /// <summary>
        /// Retrieves the characteristics of the byte stream. 
        /// virtual HRESULT STDMETHODCALLTYPE GetCapabilities(/*[out]*/ __RPC__out DWORD *pdwCapabilities) = 0;
        /// </summary>
        void GetCapabilities(ref int pdwCapabiities);

        /// <summary>
        /// Retrieves the length of the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE GetLength(/*[out]*/ __RPC__out QWORD *pqwLength) = 0;
        /// </summary>
        void GetLength(ref long pqwLength);

        /// <summary>
        /// Sets the length of the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE SetLength(/*[in]*/ QWORD qwLength) = 0;
        /// </summary>
        void SetLength(long qwLength);
        
        /// <summary>
        /// Retrieves the current read or write position in the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE GetCurrentPosition(/*[out]*/ __RPC__out QWORD *pqwPosition) = 0;
        /// </summary>
        void GetCurrentPosition(ref long pqwPosition);

        /// <summary>
        /// Sets the current read or write position. 
        /// virtual HRESULT STDMETHODCALLTYPE SetCurrentPosition(/*[in]*/ QWORD qwPosition) = 0;
        /// </summary>
        void SetCurrentPosition(long qwPosition);

        /// <summary>
        /// Queries whether the current position has reached the end of the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE IsEndOfStream(/*[out]*/ __RPC__out BOOL *pfEndOfStream) = 0;
        /// </summary>
        void IsEndOfStream([MarshalAs(UnmanagedType.Bool)] ref bool pfEndOfStream);

        /// <summary>
        /// Reads data from the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE Read(/*[size_is][out]*/ __RPC__out_ecount_full(cb) BYTE *pb, /*[in]*/ ULONG cb, /*[out]*/ __RPC__out ULONG *pcbRead) = 0;
        /// </summary>
        void Read(IntPtr pb, int cb, ref int pcbRead);

        /// <summary>
        /// Begins an asynchronous read operation from the stream. 
        /// virtual /*[local]*/ HRESULT STDMETHODCALLTYPE BeginRead(/*[out]*/ _Out_writes_bytes_(cb)  BYTE *pb, /*[in]*/ ULONG cb, /*[in]*/ IMFAsyncCallback *pCallback, /*[in]*/ IUnknown *punkState) = 0;
        /// </summary>
        void BeginRead(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);

        /// <summary>
        /// Completes an asynchronous read operation. 
        /// virtual /*[local]*/ HRESULT STDMETHODCALLTYPE EndRead(/*[in]*/ IMFAsyncResult *pResult, /*[out]*/ _Out_  ULONG *pcbRead) = 0;
        /// </summary>
        void EndRead(IntPtr pResult, ref int pcbRead);

        /// <summary>
        /// Writes data to the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE Write(/*[size_is][in]*/ __RPC__in_ecount_full(cb) const BYTE *pb, /*[in]*/ ULONG cb, /*[out]*/ __RPC__out ULONG *pcbWritten) = 0;
        /// </summary>
        void Write(IntPtr pb, int cb, ref int pcbWritten);

        /// <summary>
        /// Begins an asynchronous write operation to the stream. 
        /// virtual /*[local]*/ HRESULT STDMETHODCALLTYPE BeginWrite(/*[in]*/ _In_reads_bytes_(cb)  const BYTE *pb, /*[in]*/ ULONG cb, /*[in]*/ IMFAsyncCallback *pCallback, /*[in]*/ IUnknown *punkState) = 0;
        /// </summary>
        void BeginWrite(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);

        /// <summary>
        /// Completes an asynchronous write operation. 
        /// virtual /*[local]*/ HRESULT STDMETHODCALLTYPE EndWrite(/*[in]*/ IMFAsyncResult *pResult, /*[out]*/ _Out_  ULONG *pcbWritten) = 0;
        /// </summary>
        void EndWrite(IntPtr pResult, ref int pcbWritten);
        
        /// <summary>
        /// Moves the current position in the stream by a specified offset. 
        /// virtual HRESULT STDMETHODCALLTYPE Seek(/*[in]*/ MFBYTESTREAM_SEEK_ORIGIN SeekOrigin, /*[in]*/ LONGLONG llSeekOffset, /*[in]*/ DWORD dwSeekFlags, /*[out]*/ __RPC__out QWORD *pqwCurrentPosition) = 0;
        /// </summary>
        void Seek(int SeekOrigin, long llSeekOffset, int dwSeekFlags, ref long pqwCurrentPosition);

        /// <summary>
        /// Clears any internal buffers used by the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE Flush( void) = 0;
        /// </summary>
        void Flush();

        /// <summary>
        /// Closes the stream and releases any resources associated with the stream. 
        /// virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        /// </summary>
        void Close();
    }
}