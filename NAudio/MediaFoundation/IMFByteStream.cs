using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("ad4c1b00-4bf7-422f-9175-756693d9130d")]
    public interface IMFByteStream
    {
        //virtual HRESULT STDMETHODCALLTYPE GetCapabilities(/*[out]*/ __RPC__out DWORD *pdwCapabilities) = 0;
        void GetCapabilities(ref int pdwCapabiities);
        //virtual HRESULT STDMETHODCALLTYPE GetLength(/*[out]*/ __RPC__out QWORD *pqwLength) = 0;
        void GetLength(ref long pqwLength);
        //virtual HRESULT STDMETHODCALLTYPE SetLength(/*[in]*/ QWORD qwLength) = 0;
        void SetLength(long qwLength);
        //virtual HRESULT STDMETHODCALLTYPE GetCurrentPosition(/*[out]*/ __RPC__out QWORD *pqwPosition) = 0;
        void GetCurrentPosition(ref long pqwPosition);
        //virtual HRESULT STDMETHODCALLTYPE SetCurrentPosition(/*[in]*/ QWORD qwPosition) = 0;
        void SetCurrentPosition(long qwPosition);
        //virtual HRESULT STDMETHODCALLTYPE IsEndOfStream(/*[out]*/ __RPC__out BOOL *pfEndOfStream) = 0;
        void IsEndOfStream([MarshalAs(UnmanagedType.Bool)] ref bool pfEndOfStream);
        //virtual HRESULT STDMETHODCALLTYPE Read(/*[size_is][out]*/ __RPC__out_ecount_full(cb) BYTE *pb, /*[in]*/ ULONG cb, /*[out]*/ __RPC__out ULONG *pcbRead) = 0;
        void Read(IntPtr pb, int cb, ref int pcbRead);
        //virtual /*[local]*/ HRESULT STDMETHODCALLTYPE BeginRead(/*[out]*/ _Out_writes_bytes_(cb)  BYTE *pb, /*[in]*/ ULONG cb, /*[in]*/ IMFAsyncCallback *pCallback, /*[in]*/ IUnknown *punkState) = 0;
        void BeginRead(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);
        //virtual /*[local]*/ HRESULT STDMETHODCALLTYPE EndRead(/*[in]*/ IMFAsyncResult *pResult, /*[out]*/ _Out_  ULONG *pcbRead) = 0;
        void EndRead(IntPtr pResult, ref int pcbRead);
        //virtual HRESULT STDMETHODCALLTYPE Write(/*[size_is][in]*/ __RPC__in_ecount_full(cb) const BYTE *pb, /*[in]*/ ULONG cb, /*[out]*/ __RPC__out ULONG *pcbWritten) = 0;
        void Write(IntPtr pb, int cb, ref int pcbWritten);
        //virtual /*[local]*/ HRESULT STDMETHODCALLTYPE BeginWrite(/*[in]*/ _In_reads_bytes_(cb)  const BYTE *pb, /*[in]*/ ULONG cb, /*[in]*/ IMFAsyncCallback *pCallback, /*[in]*/ IUnknown *punkState) = 0;
        void BeginWrite(IntPtr pb, int cb, IntPtr pCallback, IntPtr punkState);
        //virtual /*[local]*/ HRESULT STDMETHODCALLTYPE EndWrite(/*[in]*/ IMFAsyncResult *pResult, /*[out]*/ _Out_  ULONG *pcbWritten) = 0;
        void EndWrite(IntPtr pResult, ref int pcbWritten);
        //virtual HRESULT STDMETHODCALLTYPE Seek(/*[in]*/ MFBYTESTREAM_SEEK_ORIGIN SeekOrigin, /*[in]*/ LONGLONG llSeekOffset, /*[in]*/ DWORD dwSeekFlags, /*[out]*/ __RPC__out QWORD *pqwCurrentPosition) = 0;
        void Seek(int SeekOrigin, long llSeekOffset, int dwSeekFlags, ref long pqwCurrentPosition);
        //virtual HRESULT STDMETHODCALLTYPE Flush( void) = 0;
        void Flush();
        //virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        void Close();
    }
}