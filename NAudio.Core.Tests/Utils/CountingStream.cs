using System;
using System.IO;

namespace NAudioTests.Utils
{
    /// <summary>
    /// Wraps an inner stream and counts the number of bytes returned by Read calls.
    /// Used by tests that need to assert bounded I/O (e.g. that a constructor doesn't
    /// scan an entire file).
    /// </summary>
    sealed class CountingStream : Stream
    {
        private readonly Stream inner;

        public CountingStream(Stream inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public long BytesRead { get; private set; }

        public void ResetCounter() => BytesRead = 0;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = inner.Read(buffer, offset, count);
            BytesRead += n;
            return n;
        }

        public override int Read(Span<byte> buffer)
        {
            int n = inner.Read(buffer);
            BytesRead += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
