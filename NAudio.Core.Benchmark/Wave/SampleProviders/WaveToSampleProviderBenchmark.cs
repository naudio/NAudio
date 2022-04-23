using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Core.Benchmark.Wave.SampleProviders
{
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class WaveToSampleProviderBenchmark
    {
        BenchmarkProvider benchmarkSampleProvider;
        WaveToSampleProvider waveToSampleProvider;

        const int sampleCount = 4800;
        float[] buffer;

        [GlobalSetup]
        public void Setup()
        {
            benchmarkSampleProvider = new BenchmarkProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
            waveToSampleProvider = new WaveToSampleProvider(benchmarkSampleProvider);
            buffer = new float[sampleCount];
        }

        [Benchmark]
        public void Benchmark()
        {
            waveToSampleProvider.Read(buffer, 0, sampleCount);
        }
    }
}
