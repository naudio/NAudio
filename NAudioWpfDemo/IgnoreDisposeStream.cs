using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NAudioWpfDemo
{
    class IgnoreDisposeStream : Stream
    {
        public Stream SourceStream { get; private set; }
        public bool IgnoreDispose { get; set; }

        public IgnoreDisposeStream(Stream sourceStream)
        {
            SourceStream = sourceStream;
            IgnoreDispose = true;
        }

        public override bool CanRead
        {
            get { return SourceStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return SourceStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return SourceStream.CanWrite; }
        }

        public override void Flush()
        {
            SourceStream.Flush();
        }

        public override long Length
        {
            get { return SourceStream.Length; }
        }

        public override long Position
        {
            get
            {
                return SourceStream.Position;
            }
            set
            {
                SourceStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return SourceStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return SourceStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            SourceStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            SourceStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (!IgnoreDispose)
            {
                SourceStream.Dispose();
                SourceStream = null;
            }
        }
    }
}
