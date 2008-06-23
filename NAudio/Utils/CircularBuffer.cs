using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace NAudio.Utils
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        byte[] buffer;
        int writePosition;
        int readPosition;
        int byteCount;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            buffer = new byte[size];
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of bytes to write</param>
        public void Write(byte[] data, int offset, int count)
        {
            int bytesWritten = 0;
            if (count > buffer.Length - byteCount)
            {
                throw new ArgumentException("Not enough space in buffer");
            }
            // write to end
            int writeToEnd = Math.Min(buffer.Length - writePosition, count);
            Array.Copy(data, offset, buffer, writePosition, writeToEnd);
            writePosition += writeToEnd;
            writePosition %= buffer.Length;
            bytesWritten += writeToEnd;
            if (bytesWritten < count)
            {
                Debug.Assert(writePosition == 0);
                // must have wrapped round. Write to start
                Array.Copy(data, offset + bytesWritten, buffer, writePosition, count - bytesWritten);
                writePosition += (count - bytesWritten);
                bytesWritten = count;
            }
            byteCount += bytesWritten;
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">Bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(byte[] data, int offset, int count)
        {
            if (count > byteCount)
            {
                count = byteCount;
            }
            int bytesRead = 0;
            int readToEnd = Math.Min(buffer.Length - readPosition, count);
            Array.Copy(buffer, readPosition, data, offset, readToEnd);
            bytesRead += readToEnd;
            readPosition += readToEnd;
            readPosition %= buffer.Length;

            if (bytesRead < count)
            {
                // must have wrapped round. Read from start
                Debug.Assert(readPosition == 0);
                Array.Copy(buffer, readPosition, data, offset + bytesRead, count - bytesRead);
                readPosition += (count - bytesRead);
                bytesRead = count;
            }

            byteCount -= bytesRead;
            Debug.Assert(byteCount >= 0);
            return bytesRead;

        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength
        {
            get { return buffer.Length; }
        }

        /// <summary>
        /// Number of valid bytes currently in the circular buffer
        /// </summary>
        public int Count
        {
            get { return byteCount; }
        }

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
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
            if (count >= byteCount)
            {
                Reset();
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
