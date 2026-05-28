
using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using NAudio.Utils;
using NAudio.Wasapi.CoreAudioApi;
using NAudio.MediaFoundation.Interfaces;
using NAudio.MediaFoundation.FileFormatDiscovery;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for .NET stream -> IMFByteStream <br />
    /// Newer implementation that does not utilize the IStream -> IMFByteStream shim. <br />
    /// Note: The caller is responsible for freeing the stream that is wrapped.
    /// </summary>
    // Helpful link for async handling: https://learn.microsoft.com/en-us/windows/win32/medfound/writing-an-asynchronous-method .
    [GeneratedComClass]
    internal sealed partial class MfByteStreamFromStream : IMFByteStream, IMFAttributes, IDisposable
    {
        private readonly Stream stream;
        private readonly long wrapperInitialPosition;
        private IntPtr nativeAttributesPtr;
        private IMFAttributes nativeAttributesRcw;

        public unsafe MfByteStreamFromStream(Stream stream)
        {
            this.stream = stream;
            try { wrapperInitialPosition = stream.Position; } catch { wrapperInitialPosition = 0; }
            (nativeAttributesPtr, nativeAttributesRcw) = MediaFoundationApi.CreateAttributes(1);
            // The only way for the below to throw an exception is a critical one,
            // which eitherwise will terminate soon the process, so we are OK and we do not need EH for this.
            AudioFileFormat fmt = new AudioFileFormatFinder().AddDefaultFileFormats().FindFormat(this.stream);
            if (fmt is not null)
            {
                Guid t = MfByteStreamAttributes.ContentType;
                nativeAttributesRcw.SetString(t, fmt.MimeType);
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

        public int EndRead(IntPtr result, out int pcbRead)
        {
            if (result == IntPtr.Zero)
            {
                pcbRead = 0;
                return HResult.STG_E_INVALIDPOINTER;
            }
            else
            {
                return FinalizeAsyncCall(result, out pcbRead);
            }
        }

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

        public int EndWrite(IntPtr result, out int pcbWritten)
        {
            if (result == IntPtr.Zero)
            {
                pcbWritten = 0;
                return HResult.STG_E_INVALIDPOINTER;
            }
            else
            {
                return FinalizeAsyncCall(result, out pcbWritten);
            }
        }

        // Some notes regarding how this works, provided here for future contributors:
        // -> 1. You cannot implement custom IMFAsyncResult objects, as those are incompatible with the work queue feature of MF.
        // This happens because CreateAsyncResult returns a COM object that is of type MFASYNCRESULT, which is what work queues support.
        // However, we cannot port the MFASYNCRESULT structure, as it's implementation details are private and fragile.
        // If you avoid this, you get access violation errors due to this. Even MS docs suggest you to use MFASYNCRESULT instead.
        // -> 2. To come around the limitations due to number 1 above, a custom IMFAsyncCallback had to be created to support the reading/writing
        // and then call the callback provided by the callback COM object.
        // -> 3. This method frees the async results once we have made our way to be enqueued into the work queue.
        // The ptr pointer is only held by our custom async result, which is also released, once our result is also released.
        // See Invoke method in MfByteStreamOnStreamAsyncCallback for more info.
        private int CreateAsyncCall(nint pb, int cb, nint callback, nint state, bool readMode)
        {
            IntPtr sizeHandlerCcw = IntPtr.Zero,
                completionResultPtr = IntPtr.Zero,
                workItemPtr = IntPtr.Zero;
            IMFAsyncResult completionResult = null, workItem = null;
            try
            {
                // We own this CCW ref; MFCreateAsyncResult AddRefs it as pObject, so it must be released
                // here or the CCW (and the managed handler it pins) leaks once per BeginRead/BeginWrite.
                sizeHandlerCcw = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(
                    new MfByteStreamAsyncCallSizeHandler(),
                    CreateComInterfaceFlags.None);

                (completionResultPtr, completionResult) = MediaFoundationApi.CreateAsyncResult(
                    callback, sizeHandlerCcw, state);

                (workItemPtr, workItem) = MediaFoundationApi.CreateAsyncResult(
                    new MfByteStreamOnStreamAsyncCallback(readMode ? new(Read) : new(Write), pb, cb),
                    completionResultPtr,
                    state);

                return MediaFoundationApi.PutWorkItem(workItemPtr);
            }
            catch (COMException cex)
            {
                return cex.GetHResult();
            }
            finally
            {
                ComActivation.ReleaseBoth(workItem, workItemPtr);
                ComActivation.ReleaseBoth(completionResult, completionResultPtr);
                if (sizeHandlerCcw != IntPtr.Zero)
                {
                    Marshal.Release(sizeHandlerCcw);
                }
            }
        }

        // Obtains the read or written bytes from the call and returns them to readOrWritten parameter.
        // Additionally, the result value is the HRESULT of our wrapped call.
        private int FinalizeAsyncCall(nint result, out int readOrWritten)
        {
            IMFAsyncResult resultCcw = (IMFAsyncResult)ComActivation.
                ComWrappers.GetOrCreateObjectForComInstance(result, CreateObjectFlags.UniqueInstance);
            try
            {
                int hr = resultCcw.GetObject(out nint sizeObject);

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
                        handler = (IMfByteStreamAsyncCallSizeHandler)ComActivation
                            .ComWrappers
                            .GetOrCreateObjectForComInstance(sizeObject, CreateObjectFlags.UniqueInstance);
                        readOrWritten = handler.GetDataSize();
                        return resultCcw.GetStatus();
                    }
                    finally
                    {
                        ComActivation.ReleaseBoth(handler, sizeObject);
                    }
                }
            }
            finally
            {
                ComActivation.ReleaseBoth(resultCcw, IntPtr.Zero);
            }
        }

        #endregion

        #region IMFAttributes Interface Implementation

        public int GetItem(in Guid guidKey, nint pValue) => nativeAttributesRcw.GetItem(guidKey, pValue);

        public int GetItemType(in Guid guidKey, out int pType) => nativeAttributesRcw.GetItemType(guidKey, out pType);

        public int CompareItem(in Guid guidKey, nint value, out int pbResult) => nativeAttributesRcw.CompareItem(guidKey, value, out pbResult);

        public int Compare(nint pTheirs, int matchType, out int pbResult) => nativeAttributesRcw.Compare(pTheirs, matchType, out pbResult);

        public int GetUINT32(in Guid guidKey, out int punValue) => nativeAttributesRcw.GetUINT32(guidKey, out punValue);

        public int GetUINT64(in Guid guidKey, out long punValue) => nativeAttributesRcw.GetUINT64(guidKey, out punValue);

        public int GetDouble(in Guid guidKey, out double pfValue) => nativeAttributesRcw.GetDouble(guidKey, out pfValue);

        public int GetGUID(in Guid guidKey, out Guid pguidValue) => nativeAttributesRcw.GetGUID(guidKey, out pguidValue);

        public int GetStringLength(in Guid guidKey, out int pcchLength) => nativeAttributesRcw.GetStringLength(guidKey, out pcchLength);

        public int GetString(in Guid guidKey, nint pwszValue, int cchBufSize, out int pcchLength) => nativeAttributesRcw.GetString(guidKey, pwszValue, cchBufSize, out pcchLength);

        public int GetAllocatedString(in Guid guidKey, out nint ppwszValue, out int pcchLength) => nativeAttributesRcw.GetAllocatedString(guidKey, out ppwszValue, out pcchLength);

        public int GetBlobSize(in Guid guidKey, out int pcbBlobSize) => nativeAttributesRcw.GetBlobSize(guidKey, out pcbBlobSize);

        public int GetBlob(in Guid guidKey, nint pBuf, int cbBufSize, out int pcbBlobSize) => nativeAttributesRcw.GetBlob(guidKey, pBuf, cbBufSize, out pcbBlobSize);

        public int GetAllocatedBlob(in Guid guidKey, out nint ppBuf, out int pcbSize) => nativeAttributesRcw.GetAllocatedBlob(guidKey, out ppBuf, out pcbSize);

        public int GetUnknown(in Guid guidKey, in Guid riid, out nint ppv) => nativeAttributesRcw.GetUnknown(guidKey, riid, out ppv);

        public int SetItem(in Guid guidKey, nint value) => nativeAttributesRcw.SetItem(guidKey, value);

        public int DeleteItem(in Guid guidKey) => nativeAttributesRcw.DeleteItem(guidKey);

        public int DeleteAllItems() => nativeAttributesRcw.DeleteAllItems();

        public int SetUINT32(in Guid guidKey, int unValue) => nativeAttributesRcw.SetUINT32(guidKey, unValue);

        public int SetUINT64(in Guid guidKey, long unValue) => nativeAttributesRcw.SetUINT64(guidKey, unValue);

        public int SetDouble(in Guid guidKey, double fValue) => nativeAttributesRcw.SetDouble(guidKey, fValue);

        public int SetGUID(in Guid guidKey, in Guid guidValue) => nativeAttributesRcw.SetGUID(guidKey, guidValue);

        public int SetString(in Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue) => nativeAttributesRcw.SetString(guidKey, wszValue);

        public int SetBlob(in Guid guidKey, nint pBuf, int cbBufSize) => nativeAttributesRcw.SetBlob(guidKey, pBuf, cbBufSize);

        public int SetUnknown(in Guid guidKey, nint pUnknown) => nativeAttributesRcw.SetUnknown(guidKey, pUnknown);

        public int LockStore() => nativeAttributesRcw.LockStore();

        public int UnlockStore() => nativeAttributesRcw.UnlockStore();

        public int GetCount(out int pcItems) => nativeAttributesRcw.GetCount(out pcItems);

        public int GetItemByIndex(int unIndex, out Guid pGuidKey, nint pValue) => nativeAttributesRcw.GetItemByIndex(unIndex, out pGuidKey, pValue);

        public int CopyAllItems(nint pDest) => nativeAttributesRcw.CopyAllItems(pDest);

        #endregion

        // Resets the position of the wrapper to the position captured during when the wrapper was initializing.
        // This allows us to double-initialize the Media Foundation source reader (and any other object that uses this).
        public void ResetPosition()
        {
            try { stream.Position = wrapperInitialPosition; } catch { }
        }

        // Releases the natively allocated IMFAttributes instance required to be exposed for the source reader.
        public void Dispose()
        {
            // Serialize access to this method - it can be sensitive.
            // Do not use the lock statement as it is flagged by C# as 'do not use for new designs'.
            Monitor.Enter(this);
            try
            {
                if (nativeAttributesPtr != IntPtr.Zero)
                {
                    ComActivation.ReleaseBoth(nativeAttributesRcw, nativeAttributesPtr);
                    nativeAttributesRcw = null;
                    nativeAttributesPtr = IntPtr.Zero;
                }
            }
            finally
            {
                Monitor.Exit(this);
            }
        }
    }
}
