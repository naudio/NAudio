using BenchmarkDotNet.Attributes;
using NAudio.Wave.SampleProviders;
using System;

#if NETCOREAPP3_0
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace NAudioBenchmark
{
    [Config(typeof(MultipleRuntimes))]
    public class VolumeSampleProviderBenchmark
    {
        BenchmarkSampleProvider benchmarkSampleProvider;
        VolumeSampleProvider volumeSampleProvider;

        const int sampleCount = 1005;
        float[] buffer;

        [GlobalSetup]
        public void Setup()
        {
            benchmarkSampleProvider = new BenchmarkSampleProvider(new NAudio.Wave.WaveFormat(48000, 1));
            volumeSampleProvider = new VolumeSampleProvider(benchmarkSampleProvider)
            {
                Volume = 0.5f
            };
            buffer = new float[1005];
        }

        [Benchmark]
        public void Benchmark()
        {
            volumeSampleProvider.Read(buffer, 0, sampleCount);
        }
    }
}
