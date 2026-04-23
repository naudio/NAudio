using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;

namespace NAudio.Benchmarks;

/// <summary>
/// Raw-kernel benchmark for the mix-add operation (<c>dest[n] += src[n]</c>).
/// This is the inner loop of <see cref="NAudio.Wave.SampleProviders.MixingSampleProvider"/>
/// and <see cref="NAudio.Wave.WaveMixerStream32"/>.
///
/// BufferSize values map to common audio workloads:
///   480  = 10ms mono @ 48kHz  /  5ms stereo @ 48kHz
///   1920 = 10ms stereo @ 48kHz  (WASAPI default)
///   9600 = 100ms stereo @ 48kHz  (WaveOutEvent default)
/// </summary>
[MemoryDiagnoser]
public class MixAddBenchmarks
{
    [Params(480, 1920, 9600)]
    public int BufferSize;

    private float[] dest = null!;
    private float[] src = null!;

    [GlobalSetup]
    public void Setup()
    {
        dest = new float[BufferSize];
        src = new float[BufferSize];
        var rng = new Random(1337);
        for (int i = 0; i < BufferSize; i++)
        {
            dest[i] = (float)(rng.NextDouble() * 2 - 1);
            src[i] = (float)(rng.NextDouble() * 2 - 1);
        }
    }

    /// <summary>Current scalar NAudio implementation.</summary>
    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        var d = dest.AsSpan();
        var s = src.AsSpan();
        for (int n = 0; n < s.Length; n++) d[n] += s[n];
    }

    /// <summary>TensorPrimitives — JIT picks SSE/AVX2/AVX-512/NEON at runtime.</summary>
    [Benchmark]
    public void TensorPrimitivesAdd()
    {
        TensorPrimitives.Add(dest, src, dest);
    }
}
