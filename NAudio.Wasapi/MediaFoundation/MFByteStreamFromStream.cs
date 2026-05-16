
using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

using NAudio.Utils;
using NAudio.MediaFoundation.Interfaces;

using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for .NET stream -> IMFByteStream <br />
    /// Newer implementation that does not utilize the IStream -> IMFByteStream shim. <br />
    /// Note: The caller is responsible for freeing the stream that is wrapped.
    /// </summary>
    [GeneratedComClass]
    internal sealed partial class MFByteStreamFromStream : IMFByteStream
    {
        private uint queue_token;
        private readonly Stream stream;
        private readonly long wrapper_init_pos;
        private volatile AsyncWorkData asyncdata;

        public MFByteStreamFromStream(Stream stream)
        {
            MediaFoundationException.ThrowIfFailed(
                MediaFoundationInterop.MFAllocateWorkQueueEx(MediaFoundationInterop.MF_STANDARD_WORKQUEUE, out queue_token)
            );
            asyncdata = null;
            this.stream = stream;
            try { wrapper_init_pos = stream.Position; } catch { wrapper_init_pos = 0; }
        }

        // Asyncronous work data for an asyncronous request.
        // These data are both used for R/W operations.
        private sealed class AsyncWorkData
        {
            public byte[] AllocatedDotNetBuffer;
            public IntPtr NativePointer;
            public int DataSize;

            public AsyncWorkData(byte[] buffer, IntPtr native, int cb)
            {
                AllocatedDotNetBuffer = buffer;
                NativePointer = native;
                DataSize = cb;
            }
        }

        public int Close()
        {
            // Possibly this is what done by the IStream -> IMFByteStream wrapper provided by Microsoft
            // This valid but flaky technique also allows us to double-initialize the Media Foundation reader
            try { stream.Position = wrapper_init_pos; } catch { }
            if (queue_token != 0U)
            {
                int result = MediaFoundationInterop.MFUnlockWorkQueue(queue_token);
                queue_token = 0U;
                return result;
            }
            else
                return CommonHResults.S_OK;
        }

        public int Flush()
        {
            try
            {
                stream.Flush();
            }
            catch (System.IO.IOException)
            {
                return CommonHResults.E_FAIL;
            }
            return CommonHResults.S_OK;
        }

        public int GetCapabilities(out int pdwCapabilities)
        {
            int cps = 0;
            if (stream.CanSeek)
            {
                cps |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_SEEKABLE;
            }
            if (stream.CanWrite)
            {
                cps |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_WRITABLE;
            }
            if (stream.CanRead)
            {
                cps |= IMFByteStream.MFBYTESTREAM_CAPABILITY_IS_READABLE;
                cps |= IMFByteStream.MFBYTESTREAM_CAPABILITY_DOES_NOT_USE_NETWORK;
            }
            pdwCapabilities = cps;
            return CommonHResults.S_OK;
        }

        public int GetCurrentPosition(out long pqwPosition)
        {
            try
            {
                pqwPosition = stream.Position;
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
            catch (System.IO.IOException)
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
            catch (System.IO.IOException)
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
                _ = stream.Seek(qwPosition, System.IO.SeekOrigin.Begin);
            }
            catch (System.IO.IOException)
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
            catch (System.IO.IOException)
            {
                return CommonHResults.STG_E_WRITEFAULT;
            }
            return CommonHResults.S_OK;
        }

        public unsafe int Write(nint pb, int cb, out int pcbWritten)
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(cb);
            try
            {
                Unsafe.CopyBlockUnaligned(ref rented[0], ref Unsafe.AsRef<byte>(pb.ToPointer()), (uint)cb);
                stream.Write(rented.AsSpan(0, pcbWritten = cb));
                return CommonHResults.S_OK;
            }
            catch (System.IO.IOException)
            {
                pcbWritten = 0;
                return CommonHResults.E_FAIL;
            }
            catch (OutOfMemoryException)
            {
                pcbWritten = 0;
                return CommonHResults.E_OUTOFMEMORY;
            }
            catch (NotSupportedException)
            {
                pcbWritten = 0;
                return CommonHResults.STG_E_READFAULT;
            }
            catch
            {
                pcbWritten = 0;
                return CommonHResults.E_FAIL;
            } finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public unsafe int Read(nint pb, int cb, out int pcbRead)
        {
            byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(cb);
            try
            {
                int read = stream.Read(rented);
                if (read == 0)
                {
                    pcbRead = 0;
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(pb.ToPointer()), ref rented[0], (uint)read);
                    pcbRead = read;
                }
                return CommonHResults.S_OK;
            }
            catch (System.IO.IOException)
            {
                pcbRead = 0;
                return CommonHResults.E_FAIL;
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
            catch
            {
                pcbRead = 0;
                return CommonHResults.E_FAIL;
            } finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public int BeginRead(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
        {
            byte[] temp;
            var sp = System.Buffers.ArrayPool<byte>.Shared;
            try
            {
                temp = sp.Rent(cb);
            }
            catch (OutOfMemoryException)
            {
                // Fail fast
                return CommonHResults.E_OUTOFMEMORY;
            }
            if (Interlocked.CompareExchange(ref asyncdata, new AsyncWorkData(temp, pb, cb), null) != null)
            {
                sp.Return(temp);
                return CommonHResults.E_ILLEGAL_METHOD_CALL;
            }
            return MediaFoundationInterop.MFPutWorkItem(queue_token, pCallback, punkState);
        }

        public unsafe int EndRead(nint pResult, out int pcbRead)
        {
            AsyncWorkData d = Interlocked.Exchange(ref asyncdata, null); // This will immediately null the field so that the next async operation can be queued
            if (d is null) // Invalid to call without context
            {
                pcbRead = 0;
                return CommonHResults.E_ILLEGAL_METHOD_CALL; 
            }
            try
            {
                // Calling the heavy method inside the async execution.
                // Note that I do not call the Stream's dedicated ReadAsync method since we are already into an asyncronous context.
                pcbRead = stream.Read(d.AllocatedDotNetBuffer, 0, d.DataSize);
                fixed (byte* p = d.AllocatedDotNetBuffer)
                {
                    Unsafe.CopyBlockUnaligned(d.NativePointer.ToPointer(), p, (uint)pcbRead);
                }
                return CommonHResults.S_OK;
            }
            catch (System.IO.IOException)
            {
                pcbRead = 0;
                return CommonHResults.STG_E_READFAULT;
            }
            catch
            {
                pcbRead = 0;
                return CommonHResults.E_FAIL;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(d.AllocatedDotNetBuffer);
            }
        }

        public int BeginWrite(nint pb, int cb, IMFAsyncCallback pCallback, nint punkState)
        {
            byte[] temp;
            var sp = System.Buffers.ArrayPool<byte>.Shared;
            try
            {
                temp = sp.Rent(cb);
            }
            catch (OutOfMemoryException)
            {
                // Fail fast
                return CommonHResults.E_OUTOFMEMORY;
            }
            if (Interlocked.CompareExchange(ref asyncdata, new AsyncWorkData(temp, pb, cb), null) != null)
            {
                sp.Return(temp);
                return CommonHResults.E_ILLEGAL_METHOD_CALL;
            }
            return MediaFoundationInterop.MFPutWorkItem(queue_token, pCallback, punkState);
        }

        public unsafe int EndWrite(nint pResult, out int pcbWritten)
        {
            AsyncWorkData d = Interlocked.Exchange(ref asyncdata, null); // This will immediately null the field so that the next async operation can be queued
            if (d is null) // Invalid to call without context
            {
                pcbWritten = 0;
                return CommonHResults.E_ILLEGAL_METHOD_CALL; 
            }
            try
            {
                pcbWritten = d.DataSize;
                fixed (byte* pc = d.AllocatedDotNetBuffer)
                {
                    Unsafe.CopyBlockUnaligned(d.NativePointer.ToPointer(), pc, (uint)pcbWritten);
                }
                // Calling the heavy method inside the async execution.
                // Note that I do not call the Stream's dedicated WriteAsync method since we are already into an asyncronous context.
                stream.Write(d.AllocatedDotNetBuffer.AsSpan(0, pcbWritten));
                return CommonHResults.S_OK;
            }
            catch (System.IO.IOException)
            {
                pcbWritten = 0;
                return CommonHResults.E_FAIL;
            }
            catch (NotSupportedException)
            {
                pcbWritten = 0;
                return CommonHResults.STG_E_WRITEFAULT;
            }
            finally
            {
                // Either case or failure this buffer will be returned to the array pool
                System.Buffers.ArrayPool<byte>.Shared.Return(d.AllocatedDotNetBuffer);
            }
        }
    }
}
