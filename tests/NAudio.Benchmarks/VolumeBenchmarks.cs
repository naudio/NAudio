using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;

namespace NAudio.Benchmarks;

/// <summary>
/// Raw-kernel benchmark for the scalar-multiply operation used by volume/pan/fade.
///
/// This benchmark uses a separate source and destination buffer to avoid the denormal
/// trap: in production <see cref="NAudio.Wave.SampleProviders.VolumeSampleProvider"/>
/// multiplies in place, but each Read pulls fresh samples from upstream so values never
/// accumulate. A benchmark that repeatedly multiplies the same buffer by 0.75 drives
/// values into denormal range (2^-126) within a few hundred iterations, which triggers
/// x86's 50-100× denormal-handling slowdown and makes the scalar number look ~8× worse
/// than it actually is. The out-of-place pattern below matches what TensorPrimitives
/// supports natively and gives a clean comparison.
/// </summary>
[MemoryDiagnoser]
public class VolumeBenchmarks
{
    [Params(480, 1920, 9600)]
    public int BufferSize;

    private float[] source = null!;
    private float[] destination = null!;
    private const float Volume = 0.75f;

    [GlobalSetup]
    public void Setup()
    {
        source = new float[BufferSize];
        destination = new float[BufferSize];
        var rng = new Random(1337);
        for (int i = 0; i < BufferSize; i++) source[i] = (float)(rng.NextDouble() * 2 - 1);
    }

    [Benchmark(Baseline = true)]
    public void Scalar()
    {
        var s = source.AsSpan();
        var d = destination.AsSpan();
        for (int n = 0; n < s.Length; n++) d[n] = s[n] * Volume;
    }

    [Benchmark]
    public void TensorPrimitivesMultiply()
    {
        TensorPrimitives.Multiply(source, Volume, destination);
    }
}
