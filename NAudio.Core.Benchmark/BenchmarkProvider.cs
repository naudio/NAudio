using NAudio.Wave;

namespace NAudio.Core.Benchmark
{
    public class BenchmarkProvider : ISampleProvider, IWaveProvider
    {
        public WaveFormat WaveFormat { get; private set; }

        public BenchmarkProvider(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return count;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return count;
        }
    }
}
