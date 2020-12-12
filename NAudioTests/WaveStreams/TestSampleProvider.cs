using System;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    class TestSampleProvider : ISampleProvider
    {
        private readonly int length;

        public TestSampleProvider(int sampleRate, int channels, int length = Int32.MaxValue)
        {
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            this.length = length;
        }

        public int Read(Span<float> buffer)
        {
            int n = 0;
            while (n < buffer.Length && Position < length)
            {
                buffer[n++] = (UseConstValue) ? ConstValue : Position;
                Position++;
            }
            return n;
        }

        public WaveFormat WaveFormat { get; set; }

        public int Position { get; set; }

        public bool UseConstValue { get; set; }
        public int ConstValue { get; set; }
    }
}
