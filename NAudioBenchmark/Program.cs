using BenchmarkDotNet.Running;
using System;

namespace NAudioBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<VolumeSampleProviderBenchmark>();
            //var benchmark = new VolumeSampleProviderBenchmark();
            //benchmark.Setup();
            //benchmark.Benchmark();
            Console.ReadLine();
        }
    }
}
