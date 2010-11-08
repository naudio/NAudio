using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Dmo;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.FileFormats.Mp3
{
    /// <summary>
    /// MP3 Frame Compressor using the Windows Media MP3 Decoder DMO object
    /// </summary>
    public class DmoMp3FrameDecompressor : IDisposable
    {
        private WindowsMediaMp3Decoder mp3Decoder;
        private WaveFormat pcmFormat;
        private MediaBuffer inputMediaBuffer;
        private DmoOutputDataBuffer outputBuffer;

        public DmoMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.mp3Decoder = new WindowsMediaMp3Decoder();
            if (!mp3Decoder.MediaObject.SupportsInputWaveFormat(0, sourceFormat))
            {
                throw new ArgumentException("Unsupported input format");
            }
            mp3Decoder.MediaObject.SetInputWaveFormat(0, sourceFormat);

            // TODO: find out if it auto-calculates the output type without us needing to
            // set output type directly
            DmoMediaType? outputType = mp3Decoder.MediaObject.GetOutputType(0, 0);

            this.pcmFormat = outputType.Value.GetWaveFormat();

            inputMediaBuffer = new MediaBuffer(sourceFormat.AverageBytesPerSecond);
            outputBuffer = new DmoOutputDataBuffer(pcmFormat.AverageBytesPerSecond);
        }

        /// <summary>
        /// Converted PCM WaveFormat
        /// </summary>
        public WaveFormat OutputFormat { get { return pcmFormat; } }

        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        {
            // 1. copy into our DMO's input buffer
            inputMediaBuffer.LoadData(frame.RawData, frame.FrameLength);

            // 2. Give the input buffer to the DMO to process
            mp3Decoder.MediaObject.ProcessInput(0, inputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);

            outputBuffer.MediaBuffer.SetLength(0);
            outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;

            // 3. Now ask the DMO for some output data
            mp3Decoder.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new DmoOutputDataBuffer[] { outputBuffer });

            if (outputBuffer.Length == 0)
            {
                Debug.WriteLine("ResamplerDmoStream.Read: No output data available");
                return 0;
            }

            // 5. Now get the data out of the output buffer
            outputBuffer.RetrieveData(dest, destOffset);
            Debug.Assert(!outputBuffer.MoreDataAvailable, "have not implemented more data available yet");
            
            return outputBuffer.Length;
        }

        public void Dispose()
        {
            if (inputMediaBuffer != null)
            {
                inputMediaBuffer.Dispose();
                inputMediaBuffer = null;
            }
            outputBuffer.Dispose();
            if (mp3Decoder!= null)
            {
                mp3Decoder.Dispose();
                mp3Decoder = null;
            }
        }
    }
}
