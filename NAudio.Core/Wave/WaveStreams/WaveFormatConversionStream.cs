using System;
using System.Diagnostics;
using NAudio.Wave.Compression;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that passes through an ACM Codec
    /// </summary>
    public class WaveFormatConversionStream : WaveStream
    {
        private readonly WaveFormatConversionProvider conversionProvider;
        private readonly WaveFormat targetFormat;
        private readonly long length;
        private long position;
        private readonly WaveStream sourceStream;
        private bool isDisposed;

        /// <summary>
        /// Create a new WaveFormat conversion stream
        /// </summary>
        /// <param name="targetFormat">Desired output format</param>
        /// <param name="sourceStream">Source stream</param>
        public WaveFormatConversionStream(WaveFormat targetFormat, WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.targetFormat = targetFormat;
            conversionProvider = new WaveFormatConversionProvider(targetFormat, sourceStream);
            length = EstimateSourceToDest((int)sourceStream.Length);
            position = 0;
        }

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
            var pcmFormat = AcmStream.SuggestPcmFormat(sourceStream.WaveFormat);
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
                conversionProvider.Reposition();
            }
        }

        /// <summary>
        /// Converts source bytes to destination bytes
        /// </summary>
        [Obsolete("can be unreliable, use of this method not encouraged")]
        public int SourceToDest(int source)
        {
            return (int)EstimateSourceToDest(source);
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
        /// 
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to write to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns>Bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = conversionProvider.Read(buffer, offset, count);
            position += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Disposes this stream
        /// </summary>
        /// <param name="disposing">true if the user called this</param>
        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (disposing)
                {
                    sourceStream.Dispose();
                    conversionProvider.Dispose();
                }
                else
                {
                    // we've been called by the finalizer
                    Debug.Assert(false, "WaveFormatConversionStream was not disposed");
                }
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }
    }
}
