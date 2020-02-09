using System;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Helper stream that lets us read from compressed audio files with large block alignment
    /// as though we could read any amount and reposition anywhere
    /// </summary>
    public class BlockAlignReductionStream : WaveStream
    {
        private WaveStream sourceStream;
        private long position;
        private readonly CircularBuffer circularBuffer;
        private long bufferStartPosition;
        private byte[] sourceBuffer;
        private readonly object lockObject = new object();

        /// <summary>
        /// Creates a new BlockAlignReductionStream
        /// </summary>
        /// <param name="sourceStream">the input stream</param>
        public BlockAlignReductionStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            circularBuffer = new CircularBuffer(sourceStream.WaveFormat.AverageBytesPerSecond * 4);
        }

        private byte[] GetSourceBuffer(int size)
        {
            if (sourceBuffer == null || sourceBuffer.Length < size)
            {
                // let's give ourselves some leeway
                sourceBuffer = new byte[size * 2];
            }
            return sourceBuffer;
        }

        /// <summary>
        /// Block alignment of this stream
        /// </summary>
        public override int BlockAlign
        {
            get
            {
                // can position to sample level
                return (WaveFormat.BitsPerSample / 8) * WaveFormat.Channels;
            }
        }

        /// <summary>
        /// Wave Format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// Length of this Stream
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// Current position within stream
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                lock (lockObject)
                {
                    if (position != value)
                    {
                        if (position % BlockAlign != 0)
                            throw new ArgumentException("Position must be block aligned");
                        long sourcePosition = value - (value % sourceStream.BlockAlign);
                        if (sourceStream.Position != sourcePosition)
                        {
                            sourceStream.Position = sourcePosition;
                            circularBuffer.Reset();
                            bufferStartPosition = sourceStream.Position;
                        }
                        position = value;
                    }
                }
            }
        }

        private long BufferEndPosition
        {
            get
            {

                return bufferStartPosition + circularBuffer.Count;
            }
        }

        /// <summary>
        /// Disposes this WaveStream
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "BlockAlignReductionStream was not Disposed");
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads data from this stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (lockObject)
            {
                // 1. attempt to fill the circular buffer with enough data to meet our request
                while (BufferEndPosition < position + count)
                {
                    int sourceReadCount = count;
                    if (sourceReadCount % sourceStream.BlockAlign != 0)
                    {
                        sourceReadCount = (count + sourceStream.BlockAlign) - (count % sourceStream.BlockAlign);
                    }

                    int sourceRead = sourceStream.Read(GetSourceBuffer(sourceReadCount), 0, sourceReadCount);
                    circularBuffer.Write(GetSourceBuffer(sourceReadCount), 0, sourceRead);
                    if (sourceRead == 0)
                    {
                        // assume we have run out of data
                        break;
                    }
                }

                // 2. discard any unnecessary stuff from the start
                if (bufferStartPosition < position)
                {
                    circularBuffer.Advance((int)(position - bufferStartPosition));
                    bufferStartPosition = position;
                }

                // 3. now whatever is in the buffer we can return
                int bytesRead = circularBuffer.Read(buffer, offset, count);
                position += bytesRead;
                // anything left in buffer is at start position
                bufferStartPosition = position;

                return bytesRead;
            }
        }
    }
}
