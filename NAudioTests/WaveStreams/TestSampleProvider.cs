using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
    class TestSampleProvider : ISampleProvider
    {
        private int length;

        public TestSampleProvider(int sampleRate, int channels, int length = Int32.MaxValue)
        {
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            this.length = length;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int n = 0;
            while (n < count && Position < length)
            {
                buffer[n + offset] = (UseConstValue) ? ConstValue : Position;
                n++; Position++;
            }
            return n;
        }

        public WaveFormat WaveFormat { get; set; }

        public int Position { get; set; }

        public bool UseConstValue { get; set; }
        public int ConstValue { get; set; }
    }
}
