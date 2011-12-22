using System;
using NAudio.Wave.Compression;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that passes through an ACM Codec
    /// </summary>
    public class WaveFormatConversionStream : WaveStream
    {
        private AcmStream conversionStream;
        private WaveStream sourceStream;
        private WaveFormat targetFormat;
        private long length;
        private long position;
        private int blockAlign;

        /// <summary>
        /// Creates a stream that can convert to PCM
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
        /// <returns>A PCM stream</returns>
        public static WaveStream CreatePcmStream(WaveStream sourceStream)
        {
            if (sourceStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                return sourceStream;
            }
            WaveFormat pcmFormat = AcmStream.SuggestPcmFormat(sourceStream.WaveFormat);
            return new WaveFormatConversionStream(pcmFormat, sourceStream);
        }

        /// <summary>
        /// Create a new WaveFormat conversion stream
        /// </summary>
        /// <param name="targetFormat">Desired output format</param>
        /// <param name="sourceStream">Source stream</param>
        public WaveFormatConversionStream(WaveFormat targetFormat, WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.targetFormat = targetFormat;

            conversionStream = new AcmStream(sourceStream.WaveFormat, targetFormat);
            // work out how many bytes the entire input stream will convert to
            length = SourceToDest((int)sourceStream.Length);
            blockAlign = SourceToDest(sourceStream.BlockAlign);
            position = 0;
        }

        /// <summary>
        /// Converts source bytes to destination bytes
        /// </summary>
        public int SourceToDest(int source)
        {
            return conversionStream.SourceToDest(source);
        }

        /// <summary>
        /// Converts destination bytes to source bytes
        /// </summary>
        public int DestToSource(int dest)
        {
            //return (dest * sourceStream.BlockAlign) / blockAlign;
            return conversionStream.DestToSource(dest);
        }

        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// Gets or sets the current position in the stream
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                // make sure we don't get out of sync
                value -= (value % BlockAlign);
                sourceStream.Position = conversionStream.DestToSource((int)value);
                position = value;
            }
        }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return targetFormat;
            }
        }


        /// <summary>
        /// Reads bytes from this stream
        /// </summary>
        /// <param name="array">Buffer to read into</param>
        /// <param name="offset">Offset in array to read into</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] array, int offset, int count)
        {
            int bytesRead = 0;
            if (count % BlockAlign != 0)
            {
                //throw new ApplicationException("Must read complete blocks");
                count -= (count % BlockAlign);
            }

            while (bytesRead < count)
            {
                int destBytesRequired = count - bytesRead;
                int sourceBytes = DestToSource(destBytesRequired);

                sourceBytes = Math.Min(conversionStream.SourceBuffer.Length, sourceBytes);
                // temporary fix for alignment problems
                // TODO: a better solution is to save any extra we convert for the next read

                /* MRH: ignore this for now - need to check ramifications
                 * if (DestToSource(SourceToDest(sourceBytes)) != sourceBytes)
                {
                    if (bytesRead == 0)
                        throw new ApplicationException("Not a one-to-one conversion");
                    break;
                }*/

                int sourceBytesRead = sourceStream.Read(conversionStream.SourceBuffer, 0, sourceBytes);
                if (sourceBytesRead == 0)
                {
                    break;
                }
                int silenceBytes = 0;
                if (sourceBytesRead % sourceStream.BlockAlign != 0)
                {
                    // we have been returned something that cannot be converted - a partial
                    // buffer. We will increase the size we supposedly read, and zero out
                    // the end.
                    sourceBytesRead -= (sourceBytesRead % sourceStream.BlockAlign);
                    sourceBytesRead += sourceStream.BlockAlign;
                    silenceBytes = SourceToDest(sourceStream.BlockAlign);
                }

                int sourceBytesConverted = 0;
                int bytesConverted = conversionStream.Convert(sourceBytesRead, out sourceBytesConverted);
                if (sourceBytesConverted < sourceBytesRead)
                {
                    // MRH: would normally throw an exception here
                    // back up - is this the right thing to do, not sure
                    sourceStream.Position -= (sourceBytesRead - sourceBytesConverted);
                }

                if (bytesConverted > 0)
                {
                    position += bytesConverted;
                    int availableSpace = array.Length - bytesRead - offset;
                    int toCopy = Math.Min(bytesConverted, availableSpace);
                    //System.Diagnostics.Debug.Assert(toCopy == bytesConverted);
                    // TODO: save leftovers
                    Array.Copy(conversionStream.DestBuffer, 0, array, bytesRead + offset, toCopy);
                    bytesRead += toCopy;
                    if (silenceBytes > 0)
                    {
                        // clear out the final bit
                        Array.Clear(array, bytesRead - silenceBytes, silenceBytes);
                    }
                }
                else
                {
                    break;
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Disposes this stream
        /// </summary>
        /// <param name="disposing">true if the user called this</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (conversionStream != null)
                {
                    conversionStream.Dispose();
                    conversionStream = null;
                }
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveFormatConversionStream was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the block alignment for this stream
        /// </summary>
        public override int BlockAlign
        {
            get
            {
                return blockAlign;
            }
        }

    }
}
