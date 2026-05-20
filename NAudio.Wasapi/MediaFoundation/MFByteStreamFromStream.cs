
using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.Utils.FileFormatDiscovery;
using NAudio.MediaFoundation.Interfaces;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for .NET stream -> IMFByteStream <br />
    /// Newer implementation that does not utilize the IStream -> IMFByteStream shim. <br />
    /// Note: The caller is responsible for freeing the stream that is wrapped.
    /// </summary>
    // mdcdi1315: Helpful link for async handling: https://learn.microsoft.com/en-us/windows/win32/medfound/writing-an-asynchronous-method .
    [GeneratedComClass]
    internal sealed partial class MFByteStreamFromStream : IMFByteStream, IMFAttributes, IDisposable
    {
        private readonly Stream stream;
        private readonly long wrapper_init_pos;
        private IntPtr natively_allocated_attributes_ptr;
        private IMFAttributes natively_allocated_attributes;

        public unsafe MFByteStreamFromStream(Stream stream)
        {
            this.stream = stream;
            try { wrapper_init_pos = stream.Position; } catch { wrapper_init_pos = 0; }
            (natively_allocated_attributes_ptr, natively_allocated_attributes) = MediaFoundationApi.CreateAttributes(1);
            // The only way for the below to throw an exception is a critical one,
            // which eitherwise will terminate soon the process, so we are OK and we do not need EH for this.
            AudioFileFormat fmt = new AudioFileFormatFinder().AddDefaultFileFormats().FindFormat(this.stream);
            if (fmt is not null)
            {
                Guid t = MfByteStreamAttributes.ContentType;
                natively_allocated_attributes.SetString(t, fmt.MimeType);
            }
        }

        #region IMFByteStream Interface Implementation

        // mdcdi1315: Do not trust this.
        // Main rationale for deciding to not trust this is this:
        // Any source reader and sink writer may fail to properly implement this in the native code,
        // due to an omission or so, so let's try to be a bit less error-prone.
        public int Close() => HResult.S_OK;

        public int Flush()
        {
            try
            {
                // Only relevant when writing, ignore if unwritable
                if (stream.CanWrite) { stream.Flush(); }
            }
            catch (IOException)
            {
                // Flush is called during writing only, so IOException here could be mapped to STG_E_WRITEFAULT.
                return HResult.STG_E_WRITEFAULT;
            }
            return HResult.S_OK;
        }

        public int GetCapabilities(out int pdwCapabilities)
        {
            int c = 0;
            if (stream.CanSeek)
            {
                c |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_SEEKABLE;
            }
            if (stream.CanWrite)
            {
                c |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_WRITABLE;
            }
            if (stream.CanRead)
            {
                c |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_READABLE;
                c |= IMFByteStream.MFBYTESTREAM_CAPABILITY_DOES_NOT_USE_NETWORK;
            }
            pdwCapabilities = c;
            return HResult.S_OK;
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
                return HResult.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                pqwPosition = 0L;
                return HResult.E_FAIL;
            }
            return HResult.S_OK;
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
            return HResult.S_OK;
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
                return HResult.STG_E_UNIMPLEMENTEDFUNCTION;
            }
            catch (IOException)
            {
                pfEndOfStream = 0;
                return HResult.STG_E_SEEKERROR;
            }
            return HResult.S_OK;
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
                    return HResult.E_INVALIDARG;
                }
                return HResult.S_OK;
            }
            catch (IOException)
            {
                pqwCurrentPosition = 0;
                return HResult.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                pqwCurrentPosition = 0;
                return HResult.STG_E_UNIMPLEMENTEDFUNCTION;
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
                return HResult.STG_E_SEEKERROR;
            }
            catch (NotSupportedException)
            {
                return HResult.STG_E_UNIMPLEMENTEDFUNCTION;
            }
            return HResult.S_OK;
        }

        public int SetLength(long qwLength)
        {
            try
            {
                stream.SetLength(qwLength);
            }
            catch (NotSupportedException)
            {
                return HResult.STG_E_UNIMPLEMENTEDFUNCTION;
            }
            catch (IOException)
            {
                return HResult.STG_E_WRITEFAULT;
            }
            return HResult.S_OK;
        }

        public unsafe int Write(nint pb, int cb, out int pcbWritten)
        {
            if (pb == IntPtr.Zero)
            {
                pcbWritten = 0;
                return HResult.STG_E_INVALIDPOINTER;
            }
            else if (cb < 0)
            {
                pcbWritten = 0;
                return HResult.E_INVALIDARG;
            }
            try
            {
                stream.Write(new ReadOnlySpan<byte>(pb.ToPointer(), pcbWritten = cb));
                return HResult.S_OK;
            }
            catch (Exception e)
            {
                pcbWritten = 0;
                return e switch
                {
                    OutOfMemoryException => HResult.E_OUTOFMEMORY,
                    IOException => HResult.STG_E_WRITEFAULT,
                    UnauthorizedAccessException => HResult.STG_E_ACCESSDENIED,
                    NotImplementedException or NotSupportedException => HResult.STG_E_UNIMPLEMENTEDFUNCTION,
                    _ => HResult.E_FAIL,
                };
            }
        }

        public unsafe int Read(nint pb, int cb, out int pcbRead)
        {
            if (pb == IntPtr.Zero)
            {
                pcbRead = 0;
                return HResult.STG_E_INVALIDPOINTER;
            }
            else if (cb < 0)
            {
                pcbRead = 0;
                return HResult.E_INVALIDARG;
            }
            try
            {
                pcbRead = stream.Read(new Span<byte>(pb.ToPointer(), cb));
                return HResult.S_OK;
            }
            catch (Exception e)
            {
                pcbRead = 0;
                return e switch
                {
                    OutOfMemoryException => HResult.E_OUTOFMEMORY,
                    IOException => HResult.STG_E_READFAULT,
                    UnauthorizedAccessException => HResult.STG_E_ACCESSDENIED,
                    NotImplementedException or NotSupportedException => HResult.STG_E_UNIMPLEMENTEDFUNCTION,
                    _ => HResult.E_FAIL,
                };
            }
        }

        public int BeginRead(nint pb, int cb, IntPtr pCallback, nint punkState)
        {
            if (pb == IntPtr.Zero || pCallback == IntPtr.Zero)
            {
                return HResult.STG_E_INVALIDPOINTER;
            }
            else
            {
                return CreateAsyncCall(pb, cb, pCallback, punkState, true);
            }
        }

        public int EndRead(IntPtr result, out int pcbRead) => FinalizeAsyncCall(result, out pcbRead);

        public int BeginWrite(nint pb, int cb, IntPtr pCallback, nint punkState)
        {
            if (pb == IntPtr.Zero || pCallback == IntPtr.Zero)
            {
                return HResult.STG_E_INVALIDPOINTER;
            }
            else
            {
                return CreateAsyncCall(pb, cb, pCallback, punkState, false);
            }
        }

        public int EndWrite(IntPtr result, out int pcbWritten) => FinalizeAsyncCall(result, out pcbWritten);

        // Some notes regarding how this works, provided here for future contributors:
        // -> 1. You cannot implement custom IMFAsyncResult objects, as those are incompatible with the work queue feature of MF.
        // This happens because CreateAsyncResult returns a COM object that is of type MFASYNCRESULT, which is what work queues support.
        // However, we cannot port the MFASYNCRESULT structure, as it's implementation details are private and fragile.
        // If you avoid this, you get access violation errors due to this. Even MS docs suggest you to use MFASYNCRESULT instead.
        // -> 2. To come around the limitations due to number 1 above, a custom IMFAsyncCallback had to be created to support the reading/writing
        // and then call the callback provided by the callback COM object.
        // -> 3. This method frees the async results once we have made our way to be enqueued into the work queue.
        // The ptr pointer is only held by our custom async result, which is also released, once our result is also released.
        // See Invoke method in MFByteStreamOnStreamAsyncCallback for more info.
        private int CreateAsyncCall(nint pb, int cb, nint callback, nint state, bool readMode)
        {
            IntPtr ptr, ptr2 = IntPtr.Zero;
            IMFAsyncResult result = null, result2 = null;
            try
            {
                // Do not free 'ptr', seems that CreateAsyncResult does not call AddRef to them.
                (ptr, result) = MediaFoundationApi.CreateAsyncResult(
                    callback,
                    ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(
                        new MfByteStreamAsyncCallSizeHandler(),
                        CreateComInterfaceFlags.None
                    ),
                    state
                );
                (ptr2, result2) = MediaFoundationApi.CreateAsyncResult(
                    new MFByteStreamOnStreamAsyncCallback(readMode ? new(Read) : new(Write), pb, cb), ptr, state
                );
                return MediaFoundationApi.PutWorkItem(ptr2);
            }
            catch (COMException cex)
            {
                return cex.GetHResult();
            }
            finally
            {
                ComActivation.ReleaseBoth(result2, ptr2);
                ComActivation.ReleaseBoth(result, IntPtr.Zero);
            }
        }

        private int FinalizeAsyncCall(nint result, out int readOrWritten)
        {
            var rt = MediaFoundationApi.QueryAndCreateInterfaceInstance<IMFAsyncResult>(result);
            try
            {
                int hr = rt.GetObject(out nint sizeObject);

                if (HResult.IsError(hr))
                {
                    readOrWritten = 0;
                    return hr;
                }
                else
                {
                    IMfByteStreamAsyncCallSizeHandler handler = null;
                    try
                    {
                        handler = (IMfByteStreamAsyncCallSizeHandler)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(sizeObject, CreateObjectFlags.UniqueInstance);
                        readOrWritten = handler.GetDataSize();
                        return rt.GetStatus();
                    }
                    finally
                    {
                        ComActivation.ReleaseBoth(handler, sizeObject);
                    }
                }
            }
            finally
            {
                ComActivation.ReleaseBoth(rt, IntPtr.Zero);
            }
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
