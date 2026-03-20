using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Represents a media sample, which is a container for media data. Extends IMFAttributes.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMFSample (mfobjects.h).
    /// https://learn.microsoft.com/windows/win32/api/mfobjects/nn-mfobjects-imfsample
    /// </remarks>
    [GeneratedComInterface]
    [Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFSample
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

        // IMFSample own methods

        [PreserveSig]
        int GetSampleFlags(out int pdwSampleFlags);

        [PreserveSig]
        int SetSampleFlags(int dwSampleFlags);

        [PreserveSig]
        int GetSampleTime(out long phnsSampleTime);

        [PreserveSig]
        int SetSampleTime(long hnsSampleTime);

        [PreserveSig]
        int GetSampleDuration(out long phnsSampleDuration);

        [PreserveSig]
        int SetSampleDuration(long hnsSampleDuration);

        [PreserveSig]
        int GetBufferCount(out int pdwBufferCount);

        [PreserveSig]
        int GetBufferByIndex(int dwIndex, out IntPtr ppBuffer);

        [PreserveSig]
        int ConvertToContiguousBuffer(out IntPtr ppBuffer);

        [PreserveSig]
        int AddBuffer(IntPtr pBuffer);

        [PreserveSig]
        int RemoveBufferByIndex(int dwIndex);

        [PreserveSig]
        int RemoveAllBuffers();

        [PreserveSig]
        int GetTotalLength(out int pcbTotalLength);

        [PreserveSig]
        int CopyToBuffer(IntPtr pBuffer);
    }
}
