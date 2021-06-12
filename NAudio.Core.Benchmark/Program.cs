using BenchmarkDotNet.Running;
using System;

namespace NAudio.Core.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Wave.SampleProviders.WaveToSampleProviderBenchmark>();
            BenchmarkRunner.Run<Wave.SampleProviders.VolumeSampleProviderBenchmark>();
            Console.ReadKey();
        }
    }
}
