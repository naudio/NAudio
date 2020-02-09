using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// http://msdn.microsoft.com/en-gb/library/windows/desktop/ms702192%28v=vs.85%29.aspx
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
    public interface IMFSample : IMFAttributes
    {
        /// <summary>
        /// Retrieves the value associated with a key.
        /// </summary>
        new void GetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, Out] IntPtr pValue);

        /// <summary>
        /// Retrieves the data type of the value associated with a key.
        /// </summary>
        new void GetItemType([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pType);

        /// <summary>
        /// Queries whether a stored attribute value equals a specified PROPVARIANT.
        /// </summary>
        new void CompareItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

        /// <summary>
        /// Compares the attributes on this object with the attributes on another object.
        /// </summary>
        new void Compare([MarshalAs(UnmanagedType.Interface)] IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

        /// <summary>
        /// Retrieves a UINT32 value associated with a key.
        /// </summary>
        new void GetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int punValue);

        /// <summary>
        /// Retrieves a UINT64 value associated with a key.
        /// </summary>
        new void GetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out long punValue);

        /// <summary>
        /// Retrieves a double value associated with a key.
        /// </summary>
        new void GetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out double pfValue);

        /// <summary>
        /// Retrieves a GUID value associated with a key.
        /// </summary>
        new void GetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out Guid pguidValue);

        /// <summary>
        /// Retrieves the length of a string value associated with a key.
        /// </summary>
        new void GetStringLength([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pcchLength);

        /// <summary>
        /// Retrieves a wide-character string associated with a key.
        /// </summary>
        new void GetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize,
                       out int pcchLength);

        /// <summary>
        /// Retrieves a wide-character string associated with a key. This method allocates the memory for the string.
        /// </summary>
        new void GetAllocatedString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue,
                                out int pcchLength);

        /// <summary>
        /// Retrieves the length of a byte array associated with a key.
        /// </summary>
        new void GetBlobSize([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out int pcbBlobSize);

        /// <summary>
        /// Retrieves a byte array associated with a key.
        /// </summary>
        new void GetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pBuf, int cbBufSize,
                     out int pcbBlobSize);

        /// <summary>
        /// Retrieves a byte array associated with a key. This method allocates the memory for the array.
        /// </summary>
        new void GetAllocatedBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, out IntPtr ip, out int pcbSize);

        /// <summary>
        /// Retrieves an interface pointer associated with a key.
        /// </summary>
        new void GetUnknown([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        /// <summary>
        /// Associates an attribute value with a key.
        /// </summary>
        new void SetItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, IntPtr value);

        /// <summary>
        /// Removes a key/value pair from the object's attribute list.
        /// </summary>
        new void DeleteItem([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);

        /// <summary>
        /// Removes all key/value pairs from the object's attribute list.
        /// </summary>
        new void DeleteAllItems();

        /// <summary>
        /// Associates a UINT32 value with a key.
        /// </summary>
        new void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, int unValue);

        /// <summary>
        /// Associates a UINT64 value with a key.
        /// </summary>
        new void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, long unValue);

        /// <summary>
        /// Associates a double value with a key.
        /// </summary>
        new void SetDouble([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);

        /// <summary>
        /// Associates a GUID value with a key.
        /// </summary>
        new void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);

        /// <summary>
        /// Associates a wide-character string with a key.
        /// </summary>
        new void SetString([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);

        /// <summary>
        /// Associates a byte array with a key.
        /// </summary>
        new void SetBlob([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf,
                     int cbBufSize);

        /// <summary>
        /// Associates an IUnknown pointer with a key.
        /// </summary>
        new void SetUnknown([MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);

        /// <summary>
        /// Locks the attribute store so that no other thread can access it.
        /// </summary>
        new void LockStore();

        /// <summary>
        /// Unlocks the attribute store.
        /// </summary>
        new void UnlockStore();

        /// <summary>
        /// Retrieves the number of attributes that are set on this object.
        /// </summary>
        new void GetCount(out int pcItems);

        /// <summary>
        /// Retrieves an attribute at the specified index.
        /// </summary>
        new void GetItemByIndex(int unIndex, out Guid pGuidKey, [In, Out] IntPtr pValue);

        /// <summary>
        /// Copies all of the attributes from this object into another attribute store.
        /// </summary>
        new void CopyAllItems([In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pDest);

        /// <summary>
        /// Retrieves flags associated with the sample.
        /// </summary>
        void GetSampleFlags(out int pdwSampleFlags);

        /// <summary>
        /// Sets flags associated with the sample.
        /// </summary>
        void SetSampleFlags(int dwSampleFlags);

        /// <summary>
        /// Retrieves the presentation time of the sample.
        /// </summary>
        void GetSampleTime(out long phnsSampletime);

        /// <summary>
        /// Sets the presentation time of the sample.
        /// </summary>
        void SetSampleTime(long hnsSampleTime);

        /// <summary>
        /// Retrieves the duration of the sample.
        /// </summary>
        void GetSampleDuration(out long phnsSampleDuration);

        /// <summary>
        /// Sets the duration of the sample.
        /// </summary>
        void SetSampleDuration(long hnsSampleDuration);

        /// <summary>
        /// Retrieves the number of buffers in the sample.
        /// </summary>
        void GetBufferCount(out int pdwBufferCount);

        /// <summary>
        /// Retrieves a buffer from the sample.
        /// </summary>
        void GetBufferByIndex(int dwIndex, out IMFMediaBuffer ppBuffer);

        /// <summary>
        /// Converts a sample with multiple buffers into a sample with a single buffer.
        /// </summary>
        void ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);

        /// <summary>
        ///  Adds a buffer to the end of the list of buffers in the sample.
        /// </summary>
        void AddBuffer(IMFMediaBuffer pBuffer);

        /// <summary>
        /// Removes a buffer at a specified index from the sample.
        /// </summary>
        void RemoveBufferByIndex(int dwIndex);
        
        /// <summary>
        /// Removes all buffers from the sample.
        /// </summary>
        void RemoveAllBuffers();
        
        /// <summary>
        /// Retrieves the total length of the valid data in all of the buffers in the sample.
        /// </summary>
        void GetTotalLength(out int pcbTotalLength);

        /// <summary>
        /// Copies the sample data to a buffer.
        /// </summary>
        void CopyToBuffer(IMFMediaBuffer pBuffer);
    }
}