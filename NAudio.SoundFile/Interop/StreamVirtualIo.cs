using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NAudio.SoundFile
{
    /// <summary>
    /// Adapts a <see cref="System.IO.Stream"/> to libsndfile's
    /// <c>SF_VIRTUAL_IO</c> callback interface so files can be read from /
    /// written to any stream, not just a path on disk.
    /// </summary>
    /// <remarks>
    /// The callbacks are static <c>[UnmanagedCallersOnly]</c> methods (no
    /// delegate marshalling) so the type stays AOT-safe. The managed
    /// <see cref="Stream"/> is reached through a <see cref="GCHandle"/>
    /// passed to libsndfile as <c>user_data</c>; that handle, and this
    /// object, must outlive the <c>SNDFILE*</c> and are freed in
    /// <see cref="Dispose"/>. Exceptions never cross the native boundary —
    /// a failing callback returns <c>-1</c>, which libsndfile surfaces as a
    /// normal sndfile error.
    /// </remarks>
    internal sealed unsafe class StreamVirtualIo : IDisposable
    {
        private readonly Stream stream;
        private GCHandle selfHandle;
        private Exception pending;
        private bool disposed;

        // The caller always owns the backing stream (NAudio convention,
        // matching WaveFileReader's Stream constructor) — this adapter
        // never disposes it.
        public StreamVirtualIo(Stream stream)
        {
            this.stream = stream;
            selfHandle = GCHandle.Alloc(this);
        }

        /// <summary>The <c>user_data</c> pointer handed to <c>sf_open_virtual</c>.</summary>
        public IntPtr UserData => GCHandle.ToIntPtr(selfHandle);

        /// <summary>Whether the backing stream can satisfy seek / length queries.</summary>
        public bool CanSeek => stream.CanSeek;

        /// <summary>The <c>SF_VIRTUAL_IO</c> table libsndfile copies at open time.</summary>
        public static SfVirtualIo CreateVtable() => new()
        {
            GetFileLen = &GetFileLenCb,
            Seek = &SeekCb,
            Read = &ReadCb,
            Write = &WriteCb,
            Tell = &TellCb
        };

        private static StreamVirtualIo FromUserData(IntPtr userData)
            => (StreamVirtualIo)GCHandle.FromIntPtr(userData).Target;

        // Records the first failure so it can be rethrown on the managed
        // side after the sf_* call returns. Exceptions cannot cross the
        // native boundary and libsndfile has no "callback errored" return
        // convention, so we stop cleanly (EOF) and surface later. Only
        // Read/Write are faulted: a non-seekable stream legitimately fails
        // Seek/Tell/GetFileLen and libsndfile copes with those returning -1
        // (that is exactly how streamable FLAC/Ogg/Opus output works).
        private void Fault(Exception ex) => pending ??= ex;

        /// <summary>
        /// Rethrows the first exception a callback captured, if any. Called
        /// by the reader/writer after each native operation so a stream
        /// failure mid-decode is not silently mistaken for end-of-file.
        /// </summary>
        public void ThrowIfFaulted()
        {
            var ex = pending;
            if (ex != null)
            {
                pending = null;
                throw new SoundFileException($"Backing stream failed: {ex.Message}", 0, ex);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long GetFileLenCb(IntPtr userData)
        {
            var self = FromUserData(userData);
            try
            {
                return self.stream.CanSeek ? self.stream.Length : -1;
            }
            catch
            {
                return -1; // optional capability; libsndfile copes
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long SeekCb(long offset, int whence, IntPtr userData)
        {
            var self = FromUserData(userData);
            try
            {
                if (!self.stream.CanSeek)
                {
                    return -1;
                }

                var origin = whence switch
                {
                    SndFileInterop.SEEK_SET => SeekOrigin.Begin,
                    SndFileInterop.SEEK_CUR => SeekOrigin.Current,
                    SndFileInterop.SEEK_END => SeekOrigin.End,
                    _ => (SeekOrigin)(-1)
                };
                if (origin == (SeekOrigin)(-1))
                {
                    return -1;
                }

                return self.stream.Seek(offset, origin);
            }
            catch
            {
                return -1; // optional capability; libsndfile copes
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long ReadCb(IntPtr ptr, long count, IntPtr userData)
        {
            var self = FromUserData(userData);
            try
            {
                long total = 0;
                while (total < count)
                {
                    int chunk = (int)Math.Min(count - total, int.MaxValue);
                    var span = new Span<byte>((byte*)ptr + total, chunk);
                    int read = self.stream.Read(span);
                    if (read == 0)
                    {
                        break; // end of stream
                    }
                    total += read;
                }
                return total;
            }
            catch (Exception ex)
            {
                self.Fault(ex);
                return 0; // present as EOF; ThrowIfFaulted surfaces the real cause
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long WriteCb(IntPtr ptr, long count, IntPtr userData)
        {
            var self = FromUserData(userData);
            try
            {
                long total = 0;
                while (total < count)
                {
                    int chunk = (int)Math.Min(count - total, int.MaxValue);
                    var span = new ReadOnlySpan<byte>((byte*)ptr + total, chunk);
                    self.stream.Write(span);
                    total += chunk;
                }
                return total;
            }
            catch (Exception ex)
            {
                self.Fault(ex);
                return 0; // short write → libsndfile errors; cause surfaced later
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static long TellCb(IntPtr userData)
        {
            var self = FromUserData(userData);
            try
            {
                return self.stream.Position;
            }
            catch
            {
                return -1; // optional capability; libsndfile copes
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (selfHandle.IsAllocated)
            {
                selfHandle.Free();
            }
        }
    }
}
