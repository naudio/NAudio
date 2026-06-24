using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Represents a description of a media format. Extends IMFAttributes.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMFMediaType (mfobjects.h).
    /// https://learn.microsoft.com/windows/win32/api/mfobjects/nn-mfobjects-imfmediatype
    /// </remarks>
    [GeneratedComInterface]
    [Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFMediaType
    {
        // IMFAttributes methods (flattened vtable)

        [PreserveSig]
        int GetItem(in Guid guidKey, IntPtr pValue);

        [PreserveSig]
        int GetItemType(in Guid guidKey, out int pType);

        [PreserveSig]
        int CompareItem(in Guid guidKey, IntPtr value, out int pbResult);

        [PreserveSig]
        int Compare(IntPtr pTheirs, int matchType, out int pbResult);

        [PreserveSig]
        int GetUINT32(in Guid guidKey, out int punValue);

        [PreserveSig]
        int GetUINT64(in Guid guidKey, out long punValue);

        [PreserveSig]
        int GetDouble(in Guid guidKey, out double pfValue);

        [PreserveSig]
        int GetGUID(in Guid guidKey, out Guid pguidValue);

        [PreserveSig]
        int GetStringLength(in Guid guidKey, out int pcchLength);

        [PreserveSig]
        int GetString(in Guid guidKey, IntPtr pwszValue, int cchBufSize, out int pcchLength);

        [PreserveSig]
        int GetAllocatedString(in Guid guidKey, out IntPtr ppwszValue, out int pcchLength);

        [PreserveSig]
        int GetBlobSize(in Guid guidKey, out int pcbBlobSize);

        [PreserveSig]
        int GetBlob(in Guid guidKey, IntPtr pBuf, int cbBufSize, out int pcbBlobSize);

        [PreserveSig]
        int GetAllocatedBlob(in Guid guidKey, out IntPtr ppBuf, out int pcbSize);

        [PreserveSig]
        int GetUnknown(in Guid guidKey, in Guid riid, out IntPtr ppv);

        [PreserveSig]
        int SetItem(in Guid guidKey, IntPtr value);

        [PreserveSig]
        int DeleteItem(in Guid guidKey);

        [PreserveSig]
        int DeleteAllItems();

        [PreserveSig]
        int SetUINT32(in Guid guidKey, int unValue);

        [PreserveSig]
        int SetUINT64(in Guid guidKey, long unValue);

        [PreserveSig]
        int SetDouble(in Guid guidKey, double fValue);

        [PreserveSig]
        int SetGUID(in Guid guidKey, in Guid guidValue);

        [PreserveSig]
        int SetString(in Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);

        [PreserveSig]
        int SetBlob(in Guid guidKey, IntPtr pBuf, int cbBufSize);

        [PreserveSig]
        int SetUnknown(in Guid guidKey, IntPtr pUnknown);

        [PreserveSig]
        int LockStore();

        [PreserveSig]
        int UnlockStore();

        [PreserveSig]
        int GetCount(out int pcItems);

        [PreserveSig]
        int GetItemByIndex(int unIndex, out Guid pGuidKey, IntPtr pValue);

        [PreserveSig]
        int CopyAllItems(IntPtr pDest);

        // IMFMediaType own methods

        [PreserveSig]
        int GetMajorType(out Guid pguidMajorType);

        [PreserveSig]
        int IsCompressedFormat(out int pfCompressed);

        [PreserveSig]
        int IsEqual(IntPtr pIMediaType, out int pdwFlags);

        [PreserveSig]
        int GetRepresentation(in Guid guidRepresentation, out IntPtr ppvRepresentation);

        [PreserveSig]
        int FreeRepresentation(in Guid guidRepresentation, IntPtr pvRepresentation);
    }
}
