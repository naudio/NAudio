using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Utils
{
    /// <summary>
    /// Pass-through stream that ignores Dispose
    /// Useful for dealing with MemoryStreams that you want to re-use
    /// </summary>
    public class IgnoreDisposeStream : Stream
    {
        /// <summary>
        /// The source stream all other methods fall through to
        /// </summary>
        public Stream SourceStream { get; private set; }

        /// <summary>
        /// If true the Dispose will be ignored, if false, will pass through to the SourceStream
        /// Set to true by default
        /// </summary>
        public bool IgnoreDispose { get; set; }

        /// <summary>
        /// Creates a new IgnoreDisposeStream
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
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
