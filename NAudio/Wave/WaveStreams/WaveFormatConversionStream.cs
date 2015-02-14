using System;
using System.Diagnostics;
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
        private int preferredSourceReadSize;

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
            if (pcmFormat.SampleRate < 8000)
            {
                if (sourceStream.WaveFormat.Encoding == WaveFormatEncoding.G723)
                {
                    pcmFormat = new WaveFormat(8000, 16, 1);
                }
                else
                {
                    throw new InvalidOperationException("Invalid suggested output format, please explicitly provide a target format");
                }
            }
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
            /*try
            {
                // work out how many bytes the entire input stream will convert to
                length = conversionStream.SourceToDest((int)sourceStream.Length);
            }
            catch
            {
                Dispose();
                throw;
            }*/
            length = EstimateSourceToDest((int)sourceStream.Length);

            position = 0;
            preferredSourceReadSize = Math.Min(sourceStream.WaveFormat.AverageBytesPerSecond, conversionStream.SourceBuffer.Length);
            preferredSourceReadSize -= (preferredSourceReadSize%sourceStream.WaveFormat.BlockAlign);
        }

        /// <summary>
        /// Converts source bytes to destination bytes
        /// </summary>
        [Obsolete("can be unreliable, use of this method not encouraged")]
        public int SourceToDest(int source)
        {
            return (int) EstimateSourceToDest(source);
            //return conversionStream.SourceToDest(source);
        }

        private long EstimateSourceToDest(long source)
        {
            var dest = ((source * targetFormat.AverageBytesPerSecond) / sourceStream.WaveFormat.AverageBytesPerSecond);
            dest -= (dest % targetFormat.BlockAlign);
            return dest;
        }

        private long EstimateDestToSource(long dest)
        {
            var source = ((dest * sourceStream.WaveFormat.AverageBytesPerSecond) / targetFormat.AverageBytesPerSecond);
            source -= (source % sourceStream.WaveFormat.BlockAlign);
            return (int)source;
        }
        /// <summary>
        /// Converts destination bytes to source bytes
        /// </summary>
        [Obsolete("can be unreliable, use of this method not encouraged")]
        public int DestToSource(int dest)
        {
            return (int)EstimateDestToSource(dest);
            //return conversionStream.DestToSource(dest);
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

                // this relies on conversionStream DestToSource and SourceToDest being reliable
                var desiredSourcePosition = EstimateDestToSource(value);  //conversionStream.DestToSource((int) value); 
                sourceStream.Position = desiredSourcePosition;
                position = EstimateSourceToDest(sourceStream.Position);  //conversionStream.SourceToDest((int)sourceStream.Position);
                leftoverDestBytes = 0;
                leftoverDestOffset = 0;
                conversionStream.Reposition();
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

        private int leftoverDestBytes = 0;
        private int leftoverDestOffset = 0;
        private int leftoverSourceBytes = 0;
        //private int leftoverSourceOffset = 0; 

        /// <summary>
        /// Reads bytes from this stream
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset in buffer to read into</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (count % BlockAlign != 0)
            {
                //throw new ArgumentException("Must read complete blocks");
                count -= (count % BlockAlign);
            }

            while (bytesRead < count)
            {
                // first copy in any leftover destination bytes
                int readFromLeftoverDest = Math.Min(count - bytesRead, leftoverDestBytes);
                if (readFromLeftoverDest > 0)
                {
                    Array.Copy(conversionStream.DestBuffer, leftoverDestOffset, buffer, offset+bytesRead, readFromLeftoverDest);
                    leftoverDestOffset += readFromLeftoverDest;
                    leftoverDestBytes -= readFromLeftoverDest;
                    bytesRead += readFromLeftoverDest;
                }
                if (bytesRead >= count)
                {
                    // we've fulfilled the request from the leftovers alone
                    break;
                }

                // now we'll convert one full source buffer
                if (leftoverSourceBytes > 0)
                {
                    // TODO: still to be implemented: see moving the source position back below:
                }

                // always read our preferred size, we can always keep leftovers for the next call to Read if we get
                // too much
                int sourceBytesRead = sourceStream.Read(conversionStream.SourceBuffer, 0, preferredSourceReadSize);
                if (sourceBytesRead == 0)
                {
                    // we've reached the end of the input
                    break;
                }

                int sourceBytesConverted;
                int destBytesConverted = conversionStream.Convert(sourceBytesRead, out sourceBytesConverted);
                if (sourceBytesConverted == 0)
                {
                    Debug.WriteLine(String.Format("Warning: couldn't convert anything from {0}", sourceBytesRead));
                    // no point backing up in this case as we're not going to manage to finish playing this
                    break;
                }
                else if (sourceBytesConverted < sourceBytesRead)
                {
                    // cheat by backing up in the source stream (better to save the lefto
                    sourceStream.Position -= (sourceBytesRead - sourceBytesConverted);
                }

                if (destBytesConverted > 0)
                {
                    int bytesRequired = count - bytesRead;
                    int toCopy = Math.Min(destBytesConverted, bytesRequired);
                    
                    // save leftovers
                    if (toCopy < destBytesConverted)
                    {
                        leftoverDestBytes = destBytesConverted - toCopy;
                        leftoverDestOffset = toCopy;
                    }
                    Array.Copy(conversionStream.DestBuffer, 0, buffer, bytesRead + offset, toCopy);
                    bytesRead += toCopy;
                }
                else
                {
                    // possible error here
                    Debug.WriteLine(string.Format("sourceBytesRead: {0}, sourceBytesConverted {1}, destBytesConverted {2}", 
                        sourceBytesRead, sourceBytesConverted, destBytesConverted));
                    //Debug.Assert(false, "conversion stream returned nothing at all");
                    break;
                }
            }
            position += bytesRead;
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
    }
}
