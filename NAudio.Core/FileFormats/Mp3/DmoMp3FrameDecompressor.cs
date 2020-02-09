using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Dmo;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.FileFormats.Mp3
{
    /// <summary>
    /// MP3 Frame decompressor using the Windows Media MP3 Decoder DMO object
    /// </summary>
    public class DmoMp3FrameDecompressor : IMp3FrameDecompressor
    {
        private WindowsMediaMp3Decoder mp3Decoder;
        private WaveFormat pcmFormat;
        private MediaBuffer inputMediaBuffer;
        private DmoOutputDataBuffer outputBuffer;
        private bool reposition;

        /// <summary>
        /// Initializes a new instance of the DMO MP3 Frame decompressor
        /// </summary>
        /// <param name="sourceFormat"></param>
        public DmoMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.mp3Decoder = new WindowsMediaMp3Decoder();
            if (!mp3Decoder.MediaObject.SupportsInputWaveFormat(0, sourceFormat))
            {
                throw new ArgumentException("Unsupported input format");
            }
            mp3Decoder.MediaObject.SetInputWaveFormat(0, sourceFormat);
            pcmFormat = new WaveFormat(sourceFormat.SampleRate, sourceFormat.Channels); // 16 bit
            if (!mp3Decoder.MediaObject.SupportsOutputWaveFormat(0, pcmFormat))
            {
                throw new ArgumentException(String.Format("Unsupported output format {0}", pcmFormat));
            }
            mp3Decoder.MediaObject.SetOutputWaveFormat(0, pcmFormat);

            // a second is more than enough to decompress a frame at a time
            inputMediaBuffer = new MediaBuffer(sourceFormat.AverageBytesPerSecond);
            outputBuffer = new DmoOutputDataBuffer(pcmFormat.AverageBytesPerSecond);
        }

        /// <summary>
        /// Converted PCM WaveFormat
        /// </summary>
        public WaveFormat OutputFormat { get { return pcmFormat; } }

        /// <summary>
        /// Decompress a single frame of MP3
        /// </summary>
        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        {
            // 1. copy into our DMO's input buffer
            inputMediaBuffer.LoadData(frame.RawData, frame.FrameLength);

            if (reposition)
            {
                mp3Decoder.MediaObject.Flush();
                reposition = false;
            }

            // 2. Give the input buffer to the DMO to process
            mp3Decoder.MediaObject.ProcessInput(0, inputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);

            outputBuffer.MediaBuffer.SetLength(0);
            outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;

            // 3. Now ask the DMO for some output data
            mp3Decoder.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new[] { outputBuffer });

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

        /// <summary>
        /// Alerts us that a reposition has occured so the MP3 decoder needs to reset its state
        /// </summary>
        public void Reset()
        {
            reposition = true;
        }

        /// <summary>
        /// Dispose of this obejct and clean up resources
        /// </summary>
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
