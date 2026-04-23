using BenchmarkDotNet.Attributes;
using NAudio.Dsp;

namespace NAudio.Benchmarks;

/// <summary>
/// Measures the current static <see cref="FastFourierTransform.FFT"/> path. The two shapes
/// mirror how NAudio itself uses FFT today:
///   - <see cref="ForwardFftOnly"/> — just the FFT kernel.
///   - <see cref="WindowedThenFft"/> — Hamming window applied per sample in a loop, then FFT.
///     This is what <c>SampleAggregator</c> does; each window call recomputes <c>cos</c>.
/// Sizes chosen to bracket audio-typical spectrum analysis (512 ≈ 12 ms @ 44.1 kHz; 1024 ≈ 23 ms;
/// 4096 ≈ 93 ms).
/// </summary>
[MemoryDiagnoser]
public class FftBenchmarks
{
    [Params(256, 1024, 4096)]
    public int FftSize;

    private int m;
    private float[] samples = null!;
    private Complex[] fftBuffer = null!;
    private FftProcessor processor = null!;
    private FftProcessor processorWithWindow = null!;
    private Complex[] halfSpectrum = null!;

    [GlobalSetup]
    public void Setup()
    {
        m = (int)Math.Log2(FftSize);
        samples = new float[FftSize];
        fftBuffer = new Complex[FftSize];
        processor = new FftProcessor(FftSize);
        processorWithWindow = new FftProcessor(FftSize, FftWindowType.Hamming);
        halfSpectrum = new Complex[FftSize / 2 + 1];
        var rng = new Random(42);
        for (int i = 0; i < FftSize; i++) samples[i] = (float)(rng.NextDouble() * 2 - 1);
    }

    /// <summary>FFT kernel alone — input already packed into Complex[], imaginary = 0.</summary>
    [Benchmark(Baseline = true)]
    public void ForwardFftOnly()
    {
        // Pack real samples into Complex buffer (imaginary = 0). Mirrors typical usage where
        // the caller has a fresh audio buffer each call.
        for (int i = 0; i < FftSize; i++)
        {
            fftBuffer[i].X = samples[i];
            fftBuffer[i].Y = 0f;
        }
        FastFourierTransform.FFT(true, m, fftBuffer);
    }

    /// <summary>Per-sample Hamming window + FFT. Mirrors SampleAggregator.Add.</summary>
    [Benchmark]
    public void WindowedThenFft()
    {
        for (int i = 0; i < FftSize; i++)
        {
            fftBuffer[i].X = (float)(samples[i] * FastFourierTransform.HammingWindow(i, FftSize));
            fftBuffer[i].Y = 0f;
        }
        FastFourierTransform.FFT(true, m, fftBuffer);
    }

    /// <summary>Real FFT via FftProcessor, no window. Half-size complex FFT + unpack pass.</summary>
    [Benchmark]
    public void FftProcessorRealForward()
    {
        processor.RealForward(samples, halfSpectrum);
    }

    /// <summary>Real FFT via FftProcessor with precomputed Hamming window table.</summary>
    [Benchmark]
    public void FftProcessorRealForwardWindowed()
    {
        processorWithWindow.RealForward(samples, halfSpectrum);
    }
}
