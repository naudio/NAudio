using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.SampleProviders
{
    class Stereo8SampleChunkConverter : ISampleChunkConverter
    {
        int offset;
        byte[] sourceBuffer;
        int sourceBytes;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 8 &&
                waveFormat.Channels == 2;
        }


        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 2;
            sourceBuffer = GetSourceBuffer(sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = sourceBuffer[offset++] / 256f;
                sampleRight = sourceBuffer[offset++] / 256f;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }

        /// <summary>
        /// Helper function to avoid creating a new buffer every read
        /// </summary>
        byte[] GetSourceBuffer(int bytesRequired)
        {
            if (sourceBuffer == null || sourceBuffer.Length < bytesRequired)
            {
                sourceBuffer = new byte[bytesRequired];
            }
            return sourceBuffer;
        }
    }
}
