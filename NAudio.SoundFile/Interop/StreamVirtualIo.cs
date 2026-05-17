using System;
using System.IO;
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
        private readonly bool ownsStream;
        private GCHandle selfHandle;
        private bool disposed;

        public StreamVirtualIo(Stream stream, bool ownsStream)
        {
            this.stream = stream;
            this.ownsStream = ownsStream;
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

        [UnmanagedCallersOnly]
        private static long GetFileLenCb(IntPtr userData)
        {
            try
            {
                var self = FromUserData(userData);
                return self.stream.CanSeek ? self.stream.Length : -1;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static long SeekCb(long offset, int whence, IntPtr userData)
        {
            try
            {
                var self = FromUserData(userData);
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
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static long ReadCb(IntPtr ptr, long count, IntPtr userData)
        {
            try
            {
                var self = FromUserData(userData);
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
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static long WriteCb(IntPtr ptr, long count, IntPtr userData)
        {
            try
            {
                var self = FromUserData(userData);
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
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static long TellCb(IntPtr userData)
        {
            try
            {
                return FromUserData(userData).stream.Position;
            }
            catch
            {
                return -1;
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
            if (ownsStream)
            {
                stream.Dispose();
            }
        }
    }
}
