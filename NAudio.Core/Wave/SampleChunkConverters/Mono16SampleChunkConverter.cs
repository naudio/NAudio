using NAudio.Utils;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.SampleProviders
{
    class Mono16SampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private short[] sourceBuffer;
        private int sourceSamples;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 16 &&
                waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            sourceSample = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplePairsRequired);
            var readBuffer = MemoryMarshal.Cast<short, byte>(new Span<short>(sourceBuffer, 0, samplePairsRequired));
            sourceSamples = source.Read(readBuffer) / 2;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceBuffer[sourceSample++] / 32768.0f;
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
    }
}
