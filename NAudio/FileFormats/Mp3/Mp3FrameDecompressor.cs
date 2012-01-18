using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.Diagnostics;

namespace NAudio.Wave
{
    /// <summary>
    /// MP3 Frame Decompressor using ACM
    /// </summary>
    public class AcmMp3FrameDecompressor : IDisposable, IMp3FrameDecompressor
    {
        private AcmStream conversionStream;
        private WaveFormat pcmFormat;

        /// <summary>
        /// Creates a new ACM frame decompressor
        /// </summary>
        /// <param name="sourceFormat">The MP3 source format</param>
        public AcmMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.pcmFormat = AcmStream.SuggestPcmFormat(sourceFormat);
            conversionStream = new AcmStream(sourceFormat, pcmFormat);
        }

        /// <summary>
        /// Output format (PCM)
        /// </summary>
        public WaveFormat OutputFormat { get { return pcmFormat; } }

        /// <summary>
        /// Decompresses a frame
        /// </summary>
        /// <param name="frame">The MP3 frame</param>
        /// <param name="dest">destination buffer</param>
        /// <param name="destOffset">Offset within destination buffer</param>
        /// <returns>Bytes written into destination buffer</returns>
        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        {
            Array.Copy(frame.RawData, conversionStream.SourceBuffer, frame.FrameLength);
            int sourceBytesConverted = 0;
            int converted = conversionStream.Convert(frame.FrameLength, out sourceBytesConverted);
            if (sourceBytesConverted != frame.FrameLength)
            {
                throw new InvalidOperationException(String.Format("Couldn't convert the whole MP3 frame (converted {0}/{1})",
                    sourceBytesConverted, frame.FrameLength));
            }
            Array.Copy(conversionStream.DestBuffer, 0, dest, destOffset, converted);
            return converted;
        }

        /// <summary>
        /// Disposes of this MP3 frame decompressor
        /// </summary>
        public void Dispose()
        {
            if (this.conversionStream != null)
            {
                this.conversionStream.Dispose();
                this.conversionStream = null;
            }
        }
    }
}
