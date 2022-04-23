using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NAudio.Wave.SampleProviders;

namespace NAudio.Core.Benchmark.Wave.SampleProviders
{
    [SimpleJob(RuntimeMoniker.Net472, baseline: true)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class VolumeSampleProviderBenchmark
    {
        BenchmarkProvider benchmarkSampleProvider;
        VolumeSampleProvider volumeSampleProvider;

        const int sampleCount = 4800;
        float[] buffer;

        [GlobalSetup]
        public void Setup()
        {
            benchmarkSampleProvider = new BenchmarkProvider(new NAudio.Wave.WaveFormat(48000, 1));
            volumeSampleProvider = new VolumeSampleProvider(benchmarkSampleProvider)
            {
                Volume = 0.5f
            };
            buffer = new float[sampleCount];
        }

        [Benchmark]
        public void Benchmark()
        {
            volumeSampleProvider.Read(buffer, 0, sampleCount);
        }
    }
}
