using System;
using NAudio.Wave.Compression;

namespace NAudio.Wave
{
    /// <summary>
    /// MP3 Frame Decompressor using ACM
    /// </summary>
    public class AcmMp3FrameDecompressor : IMp3FrameDecompressor
    {
        private readonly AcmStream conversionStream;
        private readonly WaveFormat pcmFormat;
        private bool disposed;

        /// <summary>
        /// Creates a new ACM frame decompressor
        /// </summary>
        /// <param name="sourceFormat">The MP3 source format</param>
        public AcmMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.pcmFormat = AcmStream.SuggestPcmFormat(sourceFormat);
            try
            {
                conversionStream = new AcmStream(sourceFormat, pcmFormat);
            }
            catch (Exception)
            {
                disposed = true;
                throw;
            }
        }

        /// <summary>
        /// Output format (PCM)
        /// </summary>
        public WaveFormat OutputFormat { get { return pcmFormat; } }

        /// <summary>
        /// Decompresses a frame into the supplied span.
        /// </summary>
        public int DecompressFrame(Mp3Frame frame, Span<byte> dest)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame), "You must provide a non-null Mp3Frame to decompress");
            }
            frame.RawData.AsSpan(0, frame.FrameLength).CopyTo(conversionStream.SourceBuffer);
            int converted = conversionStream.Convert(frame.FrameLength, out int sourceBytesConverted);
            if (sourceBytesConverted != frame.FrameLength)
            {
                throw new InvalidOperationException(String.Format("Couldn't convert the whole MP3 frame (converted {0}/{1})",
                    sourceBytesConverted, frame.FrameLength));
            }
            conversionStream.DestBuffer.AsSpan(0, converted).CopyTo(dest);
            return converted;
        }

        /// <summary>
        /// Decompresses a frame.
        /// </summary>
        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
            => DecompressFrame(frame, dest.AsSpan(destOffset));

        /// <summary>
        /// Resets the MP3 Frame Decompressor after a reposition operation
        /// </summary>
        public void Reset()
        {
            conversionStream.Reposition();
        }

        /// <summary>
        /// Disposes of this MP3 frame decompressor
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
				if(conversionStream != null)
					conversionStream.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer ensuring that resources get released properly
        /// </summary>
        ~AcmMp3FrameDecompressor()
        {
            System.Diagnostics.Debug.Assert(false, "AcmMp3FrameDecompressor Dispose was not called");
            Dispose();
        }
    }
}
