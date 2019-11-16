using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudioBenchmark
{
    public class MultipleRuntimes : ManualConfig
    {
        public MultipleRuntimes()
        {
            Add(Job.Default.With(CsProjClassicNetToolchain.Net472));
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp21));
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp22));
            Add(Job.Default.With(CsProjCoreToolchain.NetCoreApp30));
        }
    }
}
