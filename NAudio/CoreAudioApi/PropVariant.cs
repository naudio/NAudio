/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
// adapted for use in NAudio
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// from Propidl.h.
    /// http://msdn.microsoft.com/en-us/library/aa380072(VS.85).aspx
    /// contains a union so we have to do an explicit layout
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant
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
        [FieldOffset(8)] System.Runtime.InteropServices.ComTypes.FILETIME filetime;
        //CLSID* puuid;
        //CLIPDATA* pclipdata;
        //BSTR bstrVal;
        //BSTRBLOB bstrblobVal;
        [FieldOffset(8)] Blob blobVal;
        //LPSTR pszVal;
        [FieldOffset(8)] IntPtr pwszVal; //LPWSTR 
        //IUnknown* punkVal;
        /*IDispatch* pdispVal;
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

        /// <summary>
        /// Helper method to gets blob data
        /// </summary>
        byte[] GetBlob()
        {
            byte[] Result = new byte[blobVal.Length];
            Marshal.Copy(blobVal.Data, Result, 0, Result.Length);
            return Result;
        }

        /// <summary>
        /// Property value
        /// </summary>
        public object Value
        {
            get
            {
                VarEnum ve = (VarEnum)vt;
                switch (ve)
                {
                    case VarEnum.VT_I1:
                        return bVal;
                    case VarEnum.VT_I2:
                        return iVal;
                    case VarEnum.VT_I4:
                        return lVal;
                    case VarEnum.VT_I8:
                        return hVal;
                    case VarEnum.VT_INT:
                        return iVal;
                    case VarEnum.VT_UI4:
                        return ulVal;
                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(pwszVal);
                    case VarEnum.VT_BLOB:
                        return GetBlob();
                }
                throw new NotImplementedException("PropVariant " + ve.ToString());
            }
        }
    }
}
