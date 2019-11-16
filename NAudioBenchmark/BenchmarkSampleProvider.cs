using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudioBenchmark
{
    public class BenchmarkSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; private set; }

        public BenchmarkSampleProvider(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return count;
        }
    }
}
