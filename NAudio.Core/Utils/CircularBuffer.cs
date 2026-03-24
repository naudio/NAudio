using System;
using System.Diagnostics;

namespace NAudio.Utils
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        private readonly byte[] buffer;
        private readonly object lockObject;
        private int writePosition;
        private int readPosition;
        private int byteCount;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            buffer = new byte[size];
            lockObject = new object();
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(ReadOnlySpan<byte> data)
        {
            lock (lockObject)
            {
                int count = data.Length;
                var bytesWritten = 0;
                if (count > buffer.Length - byteCount)
                {
                    count = buffer.Length - byteCount;
                }
                int writeToEnd = Math.Min(buffer.Length - writePosition, count);
                data.Slice(0, writeToEnd).CopyTo(buffer.AsSpan(writePosition));
                writePosition += writeToEnd;
                writePosition %= buffer.Length;
                bytesWritten += writeToEnd;
                if (bytesWritten < count)
                {
                    Debug.Assert(writePosition == 0);
                    data.Slice(bytesWritten, count - bytesWritten).CopyTo(buffer.AsSpan(writePosition));
                    writePosition += (count - bytesWritten);
                    bytesWritten = count;
                }
                byteCount += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Span to read into</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(Span<byte> data)
        {
            lock (lockObject)
            {
                int count = data.Length;
                if (count > byteCount)
                {
                    count = byteCount;
                }
                int bytesRead = 0;
                int readToEnd = Math.Min(buffer.Length - readPosition, count);
                buffer.AsSpan(readPosition, readToEnd).CopyTo(data);
                bytesRead += readToEnd;
                readPosition += readToEnd;
                readPosition %= buffer.Length;

                if (bytesRead < count)
                {
                    Debug.Assert(readPosition == 0);
                    buffer.AsSpan(readPosition, count - bytesRead).CopyTo(data.Slice(bytesRead));
                    readPosition += (count - bytesRead);
                    bytesRead = count;
                }

                byteCount -= bytesRead;
                Debug.Assert(byteCount >= 0);
                return bytesRead;
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => buffer.Length;

        /// <summary>
        /// Number of bytes currently stored in the circular buffer
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return byteCount;
                }
            }
        }

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            lock (lockObject)
            {
                ResetInner();
            }
        }

        private void ResetInner()
        {
            byteCount = 0;
            readPosition = 0;
            writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding bytes
        /// </summary>
        /// <param name="count">Bytes to advance</param>
        public void Advance(int count)
        {
            lock (lockObject)
            {
                if (count >= byteCount)
                {
                    ResetInner();
                }
                else
                {
                    byteCount -= count;
                    readPosition += count;
                    readPosition %= MaxLength;
                }
            }
        }
    }
}
