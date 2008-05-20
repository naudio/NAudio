using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WASAPI.Interfaces
{
    // from Propidl.h.
    // http://msdn.microsoft.com/en-us/library/aa380072(VS.85).aspx
    // contains a union so we have to do an explicit layout
    [StructLayout(LayoutKind.Explicit)]
    struct PropVariant
    {
        [FieldOffset(0)] short vt;
        [FieldOffset(2)] short wReserved1;
        [FieldOffset(4)] short wReserved2;
        [FieldOffset(6)] short wReserved3;
        [FieldOffset(8)] sbyte cVal;
        [FieldOffset(8)] byte bVal;
        [FieldOffset(8)] short iVal;
        [FieldOffset(8)] ushort uiVal;
        [FieldOffset(8)] int lVal;
        [FieldOffset(8)] uint ulVal;
        [FieldOffset(8)] int intVal;
        [FieldOffset(8)] uint uintVal;
        [FieldOffset(8)] long hVal;
        [FieldOffset(8)] long uhVal;
        [FieldOffset(8)] float fltVal;
        [FieldOffset(8)] double dblVal;
        [FieldOffset(8)] bool boolVal;
        [FieldOffset(8)] int scode;
        //CY cyVal;
        [FieldOffset(8)] DateTime date;
        [FieldOffset(8)] FILETIME filetime;
        /*
        CLSID* puuid;
        CLIPDATA* pclipdata;
        BSTR bstrVal;
        BSTRBLOB bstrblobVal;
        BLOB blob;
        LPSTR pszVal;
        LPWSTR pwszVal;
        IUnknown* punkVal;
        IDispatch* pdispVal;
        IStream* pStream;
        IStorage* pStorage;
        LPVERSIONEDSTREAM pVersionedStream;
        LPSAFEARRAY parray;
        CAC cac;
        CAUB caub;
        CAI cai;
        CAUI caui;
        CAL cal;
        CAUL caul;
        CAH cah;
        CAUH cauh;
        CAFLT caflt;
        CADBL cadbl;
        CABOOL cabool;
        CASCODE cascode;
        CACY cacy;
        CADATE cadate;
        CAFILETIME cafiletime;
        CACLSID cauuid;
        CACLIPDATA caclipdata;
        CABSTR cabstr;
        CABSTRBLOB cabstrblob;
        CALPSTR calpstr;
        CALPWSTR calpwstr;
        CAPROPVARIANT capropvar;
        CHAR* pcVal;
        UCHAR* pbVal;
        SHORT* piVal;
        USHORT* puiVal;
        LONG* plVal;
        ULONG* pulVal;
        INT* pintVal;
        UINT* puintVal;
        FLOAT* pfltVal;
        DOUBLE* pdblVal;
        VARIANT_BOOL* pboolVal;
        DECIMAL* pdecVal;
        SCODE* pscode;
        CY* pcyVal;
        DATE* pdate;
        BSTR* pbstrVal;
        IUnknown** ppunkVal;
        IDispatch** ppdispVal;
        LPSAFEARRAY* pparray;
        PROPVARIANT* pvarVal;
        */
    }
}
