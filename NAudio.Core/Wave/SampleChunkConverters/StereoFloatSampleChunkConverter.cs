using NAudio.Utils;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.SampleProviders
{
    class StereoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private float[] sourceBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 2;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplePairsRequired * 2);
            var sourceByteBuffer = MemoryMarshal.Cast<float, byte>(new Span<float>(sourceBuffer, 0, samplePairsRequired * 2));
            sourceSamples = source.Read(sourceByteBuffer) / 4;
            sourceSample = 0;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceBuffer[sourceSample++];
                sampleRight = sourceBuffer[sourceSample++];
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }
}
