using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Utils
{
    /// <summary>
    /// Pass-through stream that doesn't allow seeking
    /// Useful for dealing with MemoryStreams when you want to fake a none seekable stream.
    /// </summary>
    public class ForwardOnlyStream : Stream
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
        public ForwardOnlyStream(Stream sourceStream)
        {
            SourceStream = sourceStream;
            IgnoreDispose = true;
        }

        /// <summary>
        /// Can Read
        /// </summary>
        public override bool CanRead
        {
            get { return SourceStream.CanRead; }
        }

        /// <summary>
        /// Can Seek
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Can write to the underlying stream
        /// </summary>
        public override bool CanWrite
        {
            get { return SourceStream.CanWrite; }
        }

        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        public override void Flush()
        {
            SourceStream.Flush();
        }

        /// <summary>
        /// Gets the length of the underlying stream
        /// </summary>
        public override long Length
        {
            get { throw new NotSupportedException("Stream doesn't allow seeking."); }
        }

        /// <summary>
        /// Gets or sets the position of the underlying stream
        /// </summary>
        public override long Position
        {
            get
            {
               if(!AllowPositionRead)
                    throw new NotSupportedException("Stream doesn't allow seeking.");
                return SourceStream.Position;
            }
            set
            {
                throw new NotSupportedException("Stream doesn't allow seeking.");
            }
        }

        /// <summary>
        /// Gets or sets a value indication if reading the Position property is allowed
        /// </summary>
        /// <remarks>
        /// Some streams implement none seekable differently.  Sometimes they will track how far
        /// you have read into the steam for you, but other like the .Net Crypto streams do not.
        /// </remarks>
        public bool AllowPositionRead { get; set; }

        /// <summary>
        /// Reads from the underlying stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return SourceStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Seeks on the underlying stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Stream doesn't allow seeking.");
        }

        /// <summary>
        /// Sets the length of the underlying stream
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream doesn't allow seeking.");
        }

        /// <summary>
        /// Writes to the underlying stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            SourceStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Dispose - by default (IgnoreDispose = true) will do nothing,
        /// leaving the underlying stream undisposed
        /// </summary>
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
