
using System;
using System.IO;
using System.Threading;

using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for .NET stream -> IMFByteStream <br />
    /// Newer implementation that does not utilize the IStream -> IMFByteStream shim. <br />
    /// Note: The caller is responsible for freeing the stream that is wrapped.
    /// </summary>
    [GeneratedComClass]
    internal sealed partial class MFByteStreamFromStream : IMFByteStream, IMFAttributes, IDisposable
    {
        private readonly Stream stream;
        private readonly long wrapper_init_pos;
        private volatile AsyncWorkData asyncdata;
        private IntPtr natively_allocated_attributes_ptr;
        private IMFAttributes natively_allocated_attributes;

        public MFByteStreamFromStream(Stream stream)
        {
            asyncdata = null;
            this.stream = stream;
            try { wrapper_init_pos = stream.Position; } catch { wrapper_init_pos = 0; }
            (natively_allocated_attributes_ptr, natively_allocated_attributes) = MediaFoundationApi.CreateAttributes(1);
        }

        // Asyncronous work data for an asyncronous request.
        // These data are both used for R/W operations.
        private sealed class AsyncWorkData
        {
            public IntPtr NativePointer;
            public int DataSize;

            public AsyncWorkData(IntPtr native, int cb)
            {
                NativePointer = native;
                DataSize = cb;
            }
        }

        #region IMFByteStream Interface Implementation

        // mdcdi1315: Do not trust this.
        // Main rationale for deciding to not trust this are two reasons:
        // 1. In some of my tests this can be avoided to be called by the source reader,
        // and as such the reader initializes successfully the first time,
        // but when reading from the audio thread, it fails with an exception.
        // I uncovered this from my own app and fails in 30% of cases.
        // Most notable on complex file formats, such as FLAC.
        // Specifically, it fails with 'The URL of the given bytestream is unsupported', flagging that the stream has not been repositioned.
        // 2. Any source reader and sink writer may fail to properly implement this in the native code,
        // due to an omission or so, so let's try to be a bit less error-prone.
        public int Close() => CommonHResults.S_OK;

        public int Flush()
        {
            try
            {
                stream.Flush();
            }
            catch (IOException)
            {
                // Flush is called during writing only, so IOException here could be mapped to STG_E_WRITEFAULT.
                return CommonHResults.STG_E_WRITEFAULT;
            }
            return CommonHResults.S_OK;
        }

        public int GetCapabilities(out int pdwCapabilities)
        {
            pdwCapabilities = 0;
            if (stream.CanSeek)
            {
                pdwCapabilities |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_SEEKABLE;
            }
            if (stream.CanWrite)
            {
                pdwCapabilities |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_WRITABLE;
            }
            if (stream.CanRead)
            {
                pdwCapabilities |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_READABLE;
                pdwCapabilities |= IMFByteStream.MFBYTESTREAM_CAPABILITY_DOES_NOT_USE_NETWORK;
            }
            return CommonHResults.S_OK;
        }

        public int GetCurrentPosition(out long pqwPosition)
        {
            try
            {
                pqwPosition = stream.Position;
            }
            catch (IOException)
            {
                pqwPosition = 0L;
                return CommonHResults.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                pqwPosition = 0L;
                return CommonHResults.E_FAIL;
            }
            return CommonHResults.S_OK;
        }

        public int GetLength(out long pqwLength)
        {
            try
            {
                pqwLength = stream.Length;
            }
            catch (NotSupportedException)
            {
                pqwLength = 0L;
                return MediaFoundationErrors.MF_E_BYTESTREAM_UNKNOWN_LENGTH;
            }
            return CommonHResults.S_OK;
        }

        public int IsEndOfStream(out int pfEndOfStream)
        {
            try
            {
                pfEndOfStream = stream.Position < stream.Length ? 0 : 1;
            }
            catch (NotSupportedException)
            {
                pfEndOfStream = 0;
                return CommonHResults.E_FAIL;
            }
            catch (IOException)
            {
                pfEndOfStream = 0;
                return CommonHResults.STG_E_SEEKERROR;
            }
            return CommonHResults.S_OK;
        }

        public int Seek(int seekOrigin, long llSeekOffset, int dwSeekFlags, out long pqwCurrentPosition)
        {
            try
            {
                if (seekOrigin == 0)
                {
                    pqwCurrentPosition = stream.Seek(llSeekOffset, SeekOrigin.Begin);
                }
                else if (seekOrigin == 1)
                {
                    pqwCurrentPosition = stream.Seek(llSeekOffset, SeekOrigin.Current);
                }
                else
                {
                    pqwCurrentPosition = 0;
                    return CommonHResults.E_INVALIDARG;
                }
                return CommonHResults.S_OK;
            }
            catch (IOException)
            {
                pqwCurrentPosition = 0;
                return CommonHResults.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                pqwCurrentPosition = 0;
                return CommonHResults.E_FAIL;
            }
        }

        public int SetCurrentPosition(long qwPosition)
        {
            try
            {
                _ = stream.Seek(qwPosition, SeekOrigin.Begin);
            }
            catch (IOException)
            {
                return CommonHResults.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                return CommonHResults.E_FAIL;
            }
            return CommonHResults.S_OK;
        }

        public int SetLength(long qwLength)
        {
            try
            {
                stream.SetLength(qwLength);
            }
            catch (NotSupportedException)
            {
                return CommonHResults.E_FAIL;
            }
            catch (IOException)
            {
                return CommonHResults.STG_E_WRITEFAULT;
            }
            return CommonHResults.S_OK;
        }

        public unsafe int Write(nint pb, int cb, out int pcbWritten)
        {
            try
            {
                stream.Write(new ReadOnlySpan<byte>(pb.ToPointer(), pcbWritten = cb));
                return CommonHResults.S_OK;
            }
            catch (OutOfMemoryException)
            {
                pcbWritten = 0;
                return CommonHResults.E_OUTOFMEMORY;
            }
            catch (IOException)
            {
                pcbWritten = 0;
                return CommonHResults.STG_E_WRITEFAULT;
            }
            catch (NotSupportedException)
            {
                pcbWritten = 0;
                return CommonHResults.STG_E_WRITEFAULT;
            }
            catch
            {
                pcbWritten = 0;
                return CommonHResults.E_FAIL;
            }
        }

        public unsafe int Read(nint pb, int cb, out int pcbRead)
        {
            try
            {
                pcbRead = stream.Read(new Span<byte>(pb.ToPointer(), cb));
                return CommonHResults.S_OK;
            }
            catch (OutOfMemoryException)
            {
                pcbRead = 0;
                return CommonHResults.E_OUTOFMEMORY;
            }
            catch (NotSupportedException)
            {
                pcbRead = 0;
                return CommonHResults.STG_E_READFAULT;
            }
            catch (IOException)
            {
                pcbRead = 0;
                return CommonHResults.STG_E_READFAULT;
            }
            catch
            {
                pcbRead = 0;
                return CommonHResults.E_FAIL;
            }
        }

        public int BeginRead(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
        {
            return Interlocked.CompareExchange(ref asyncdata, new AsyncWorkData(pb, cb), null) is null ?
                 MediaFoundationApi.PutWorkItem(pCallback, punkState)
                 : CommonHResults.E_ILLEGAL_METHOD_CALL;
        }

        public unsafe int EndRead(IMFAsyncResult pResult, out int pcbRead)
        {
            AsyncWorkData d = Interlocked.Exchange(ref asyncdata, null); // This will immediately null the field so that the next async operation can be queued
            if (d is null) // Invalid to call without context
            {
                pcbRead = 0;
                // mdcdi1315: This is invalid case, should not be passed through the result.
                return CommonHResults.E_ILLEGAL_METHOD_CALL; 
            }
            try
            {
                // Calling the heavy method inside the async execution.
                // Note that I do not call the Stream's dedicated ReadAsync method since we are already into an asyncronous context.
                pcbRead = stream.Read(new Span<byte>(d.NativePointer.ToPointer(), d.DataSize));
                pResult.SetStatus(CommonHResults.S_OK);
            }
            catch (IOException)
            {
                pcbRead = 0;
                pResult.SetStatus(CommonHResults.STG_E_READFAULT);
            }
            catch
            {
                pcbRead = 0;
                pResult.SetStatus(CommonHResults.E_FAIL);
            }
            return CommonHResults.S_OK;
        }

        public int BeginWrite(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
            => Interlocked.CompareExchange(ref asyncdata, new AsyncWorkData(pb, cb), null) is null
                ? MediaFoundationApi.PutWorkItem(pCallback, punkState)
                : CommonHResults.E_ILLEGAL_METHOD_CALL;

        public unsafe int EndWrite(IMFAsyncResult pResult, out int pcbWritten)
        {
            AsyncWorkData d = Interlocked.Exchange(ref asyncdata, null); // This will immediately null the field so that the next async operation can be queued
            if (d is null) // Invalid to call without context
            {
                pcbWritten = 0;
                // mdcdi1315: This is invalid case, should not be passed through the result.
                return CommonHResults.E_ILLEGAL_METHOD_CALL;
            }
            try
            {
                // Calling the heavy method inside the async execution.
                // Note that I do not call the Stream's dedicated WriteAsync method since we are already into an asyncronous context.
                stream.Write(new ReadOnlySpan<byte>(d.NativePointer.ToPointer(), pcbWritten = d.DataSize));
                pResult.SetStatus(CommonHResults.S_OK);
            }
            catch (IOException)
            {
                pcbWritten = 0;
                pResult.SetStatus(CommonHResults.E_FAIL);
            }
            catch (NotSupportedException)
            {
                pcbWritten = 0;
                pResult.SetStatus(CommonHResults.STG_E_WRITEFAULT);
            }
            return CommonHResults.S_OK;
        }

        #endregion

        #region IMFAttributes Interface Implementation

        public int GetItem(in Guid guidKey, nint pValue) => natively_allocated_attributes.GetItem(guidKey, pValue);

        public int GetItemType(in Guid guidKey, out int pType) => natively_allocated_attributes.GetItemType(guidKey, out pType);

        public int CompareItem(in Guid guidKey, nint value, out int pbResult) => natively_allocated_attributes.CompareItem(guidKey, value, out pbResult);

        public int Compare(nint pTheirs, int matchType, out int pbResult) => natively_allocated_attributes.Compare(pTheirs, matchType, out pbResult);

        public int GetUINT32(in Guid guidKey, out int punValue) => natively_allocated_attributes.GetUINT32(guidKey, out punValue);

        public int GetUINT64(in Guid guidKey, out long punValue) => natively_allocated_attributes.GetUINT64(guidKey, out punValue);

        public int GetDouble(in Guid guidKey, out double pfValue) => natively_allocated_attributes.GetDouble(guidKey, out pfValue);

        public int GetGUID(in Guid guidKey, out Guid pguidValue) => natively_allocated_attributes.GetGUID(guidKey, out pguidValue);

        public int GetStringLength(in Guid guidKey, out int pcchLength) => natively_allocated_attributes.GetStringLength(guidKey, out pcchLength);

        public int GetString(in Guid guidKey, nint pwszValue, int cchBufSize, out int pcchLength) => natively_allocated_attributes.GetString(guidKey, pwszValue, cchBufSize, out pcchLength);

        public int GetAllocatedString(in Guid guidKey, out nint ppwszValue, out int pcchLength) => natively_allocated_attributes.GetAllocatedString(guidKey, out ppwszValue, out pcchLength);

        public int GetBlobSize(in Guid guidKey, out int pcbBlobSize) => natively_allocated_attributes.GetBlobSize(guidKey, out pcbBlobSize);

        public int GetBlob(in Guid guidKey, nint pBuf, int cbBufSize, out int pcbBlobSize) => natively_allocated_attributes.GetBlob(guidKey, pBuf, cbBufSize, out pcbBlobSize);

        public int GetAllocatedBlob(in Guid guidKey, out nint ppBuf, out int pcbSize) => natively_allocated_attributes.GetAllocatedBlob(guidKey, out ppBuf, out pcbSize);

        public int GetUnknown(in Guid guidKey, in Guid riid, out nint ppv) => natively_allocated_attributes.GetUnknown(guidKey, riid, out ppv);

        public int SetItem(in Guid guidKey, nint value) => natively_allocated_attributes.SetItem(guidKey, value);

        public int DeleteItem(in Guid guidKey) => natively_allocated_attributes.DeleteItem(guidKey);

        public int DeleteAllItems() => natively_allocated_attributes.DeleteAllItems();

        public int SetUINT32(in Guid guidKey, int unValue) => natively_allocated_attributes.SetUINT32(guidKey, unValue);

        public int SetUINT64(in Guid guidKey, long unValue) => natively_allocated_attributes.SetUINT64(guidKey, unValue);

        public int SetDouble(in Guid guidKey, double fValue) => natively_allocated_attributes.SetDouble(guidKey, fValue);

        public int SetGUID(in Guid guidKey, in Guid guidValue) => natively_allocated_attributes.SetGUID(guidKey, guidValue);

        public int SetString(in Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue) => natively_allocated_attributes.SetString(guidKey, wszValue);

        public int SetBlob(in Guid guidKey, nint pBuf, int cbBufSize) => natively_allocated_attributes.SetBlob(guidKey, pBuf, cbBufSize);

        public int SetUnknown(in Guid guidKey, nint pUnknown) => natively_allocated_attributes.SetUnknown(guidKey, pUnknown);

        public int LockStore() => natively_allocated_attributes.LockStore();

        public int UnlockStore() => natively_allocated_attributes.UnlockStore();

        public int GetCount(out int pcItems) => natively_allocated_attributes.GetCount(out pcItems);

        public int GetItemByIndex(int unIndex, out Guid pGuidKey, nint pValue) => natively_allocated_attributes.GetItemByIndex(unIndex, out pGuidKey, pValue);

        public int CopyAllItems(nint pDest) => natively_allocated_attributes.CopyAllItems(pDest);

        #endregion

        // Resets the position of the wrapper to the position captured during when the wrapper was initializing.
        // This allows us to double-initialize the Media Foundation source reader (and any other object that uses this).
        public void ResetPosition()
        {
            try { stream.Position = wrapper_init_pos; } catch { }
        }

        // Releases the natively allocated IMFAttributes instance required to be exposed for the source reader.
        public void Dispose()
        {
            // Serialize access to this method - it can be sensitive.
            // Do not use the lock statement as it is flagged by C# as 'do not use for new designs'.
            Monitor.Enter(this);
            try
            {
                if (natively_allocated_attributes_ptr != IntPtr.Zero)
                {
                    ComActivation.ReleaseBoth(natively_allocated_attributes, natively_allocated_attributes_ptr);
                    natively_allocated_attributes = null;
                    natively_allocated_attributes_ptr = IntPtr.Zero;
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }
    }
}
