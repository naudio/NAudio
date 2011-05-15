using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave.SampleProviders
{
    class MonoFloatSampleChunkConverter : ISampleChunkConverter
    {
        int sourceSample;
        byte[] sourceBuffer;
        WaveBuffer sourceWaveBuffer;
        int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 4;
            sourceBuffer = GetSourceBuffer(sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 4;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
                sampleRight = sampleLeft;
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
