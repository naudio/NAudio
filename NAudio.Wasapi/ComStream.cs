using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.MediaFoundation.Interfaces;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Source-generated CCW that adapts a managed <see cref="Stream"/> to the COM
    /// <c>IStream</c> interface, for handing off to native Media Foundation via
    /// <c>MFCreateMFByteStreamOnStream</c>.
    /// </summary>
    /// <remarks>
    /// Phase 2e' Step 5 migrated this class from <c>[ComImport] ComTypes.IStream</c> to a
    /// source-generated <c>NAudio.MediaFoundation.Interfaces.IStream</c> so the CCW dispatch
    /// is whole-program-analysable and the type works under
    /// <c>BuiltInComInteropSupport=false</c>. The previous <c>byte[]</c>-typed Read/Write
    /// parameters are now raw <see cref="IntPtr"/>; the Stat method writes a
    /// <see cref="StorageStat"/> blob via <see cref="Marshal.StructureToPtr{T}(T, IntPtr, bool)"/>.
    ///
    /// CCW-handoff hazard (Phase 2f H3): callers passing this CCW to native typed as
    /// <c>IStream*</c> MUST <c>Marshal.QueryInterface</c> for <c>IID_IStream</c> first —
    /// see <see cref="NAudio.MediaFoundation.MediaFoundationApi.CreateByteStream(ComStream)"/>.
    /// </remarks>
    [GeneratedComClass]
    internal partial class ComStream : Stream, IStream
    {
        // STG_E_INVALIDFUNCTION (winerror.h)
        private const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);

        private Stream stream;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public ComStream(Stream stream)
            : this(stream, true)
        {
        }

        internal ComStream(Stream stream, bool synchronizeStream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (synchronizeStream)
                stream = Synchronized(stream);
            this.stream = stream;
        }

        // === IStream implementation ===

        public unsafe int Read(IntPtr pv, int cb, IntPtr pcbRead)
        {
            if (!CanRead)
                return STG_E_INVALIDFUNCTION;
            int val = stream.Read(new Span<byte>((void*)pv, cb));
            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt32(pcbRead, val);
            return HResult.S_OK;
        }

        public unsafe int Write(IntPtr pv, int cb, IntPtr pcbWritten)
        {
            if (!CanWrite)
                return STG_E_INVALIDFUNCTION;
            stream.Write(new ReadOnlySpan<byte>((void*)pv, cb));
            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt32(pcbWritten, cb);
            return HResult.S_OK;
        }

        public int Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            SeekOrigin origin = (SeekOrigin)dwOrigin;
            long val = stream.Seek(dlibMove, origin);
            if (plibNewPosition != IntPtr.Zero)
                Marshal.WriteInt64(plibNewPosition, val);
            return HResult.S_OK;
        }

        public int SetSize(long libNewSize)
        {
            stream.SetLength(libNewSize);
            return HResult.S_OK;
        }

        public int CopyTo(IntPtr pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            // Not implemented in original — preserve behaviour (return S_OK with zero counts).
            if (pcbRead != IntPtr.Zero) Marshal.WriteInt64(pcbRead, 0);
            if (pcbWritten != IntPtr.Zero) Marshal.WriteInt64(pcbWritten, 0);
            return HResult.S_OK;
        }

        public int Commit(int grfCommitFlags)
        {
            stream.Flush();
            return HResult.S_OK;
        }

        public int Revert()
        {
            return HResult.S_OK;
        }

        public int LockRegion(long libOffset, long cb, int dwLockType)
        {
            return HResult.S_OK;
        }

        public int UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            return HResult.S_OK;
        }

        public int Stat(IntPtr pstatstg, int grfStatFlag)
        {
            const int STGM_READ = 0x00000000;
            const int STGM_WRITE = 0x00000001;
            const int STGM_READWRITE = 0x00000002;
            const int STGTY_STREAM = 2;

            if (pstatstg == IntPtr.Zero)
                return HResult.E_INVALIDARG;

            var stat = new StorageStat
            {
                pwcsName = IntPtr.Zero,
                type = STGTY_STREAM,
                cbSize = Length,
            };

            if (CanWrite && CanRead)
                stat.grfMode |= STGM_READWRITE;
            else if (CanRead)
                stat.grfMode |= STGM_READ;
            else if (CanWrite)
                stat.grfMode |= STGM_WRITE;
            else
                return STG_E_INVALIDFUNCTION;

            Marshal.StructureToPtr(stat, pstatstg, false);
            return HResult.S_OK;
        }

        public int Clone(out IntPtr ppstm)
        {
            ppstm = IntPtr.Zero;
            return STG_E_INVALIDFUNCTION;
        }

        // === Stream overrides (unchanged) ===

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            return stream.Read(buffer);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            stream.Write(buffer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (stream == null)
                return;
            stream.Dispose();
            stream = null;
        }

        public override void Close()
        {
            base.Close();
            if (stream == null)
                return;
            stream.Close();
            stream = null;
        }
    }
}
