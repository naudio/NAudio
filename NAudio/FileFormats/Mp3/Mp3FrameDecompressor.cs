using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System.Diagnostics;

namespace NAudio.FileFormats.Mp3
{
    class Mp3FrameDecompressor : IDisposable
    {
        private AcmStream conversionStream;
        private WaveFormat pcmFormat;

        public Mp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.pcmFormat = AcmStream.SuggestPcmFormat(sourceFormat);
            conversionStream = new AcmStream(sourceFormat, pcmFormat);
        }

        public WaveFormat OutputFormat { get { return pcmFormat; } }

        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        {
            Array.Copy(frame.RawData, conversionStream.SourceBuffer, frame.FrameLength);
            int sourceBytesConverted = 0;
            int converted = conversionStream.Convert(frame.FrameLength, out sourceBytesConverted);
            if (sourceBytesConverted != frame.FrameLength)
            {
                throw new InvalidOperationException("Couldn't convert the whole MP3 frame");
            }
            Array.Copy(conversionStream.DestBuffer, 0, dest, destOffset, converted);
            return converted;
        }

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
