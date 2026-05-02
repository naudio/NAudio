using BenchmarkDotNet.Attributes;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.Benchmarks;

/// <summary>
/// Throughput comparison between the spike <see cref="ArDftResampler"/> and NAudio's
/// existing managed <see cref="WdlResampler"/> at two common consumer-audio ratios:
///   - 96000 -&gt; 44100  (downsample, the canonical "delivery format" conversion)
///   - 44100 -&gt; 48000  (slight upsample, the canonical DAW session conversion)
///
/// Wdl is benchmarked twice: with its current default config (linear interpolation +
/// 2-tap IIR, used by <see cref="WdlResamplingSampleProvider"/>) and with a 512-tap sinc
/// kernel — i.e. roughly the closest WDL preset to ARDFTSRC's quality target. This
/// gives a fair speed/quality plot point comparison for the spike's evaluation pass.
/// </summary>
[MemoryDiagnoser]
public class SrcBenchmarks
{
    /// <summary>Workload: one second of mono audio at the source rate.</summary>
    [Params("96000->44100", "44100->48000")]
    public string Ratio = "96000->44100";

    private int srcRate;
    private int dstRate;
    private float[] input = null!;
    private float[] outBuf = null!;

    [GlobalSetup]
    public void Setup()
    {
        var parts = Ratio.Split("->");
        srcRate = int.Parse(parts[0]);
        dstRate = int.Parse(parts[1]);

        input = new float[srcRate];
        var rng = new Random(1337);
        for (int i = 0; i < input.Length; i++) input[i] = (float)(rng.NextDouble() * 2 - 1);

        outBuf = new float[dstRate + 4096];
    }

    [Benchmark(Baseline = true)]
    public float[] ArDft()
    {
        return new ArDftResampler(srcRate, dstRate).Process(input);
    }

    [Benchmark]
    public int WdlDefault()
    {
        ISampleProvider source = new ArraySampleProvider(input, srcRate);
        var resampler = new WdlResamplingSampleProvider(source, dstRate);
        return DrainAll(resampler, outBuf);
    }

    [Benchmark]
    public int WdlSinc512()
    {
        ISampleProvider source = new ArraySampleProvider(input, srcRate);
        var r = new WdlResampler();
        r.SetMode(true, 0, true, sinc_size: 512);
        r.SetFilterParms();
        r.SetFeedMode(false);
        r.SetRates(srcRate, dstRate);
        ISampleProvider resampler = new ConfiguredWdlSampleProvider(source, dstRate, r);
        return DrainAll(resampler, outBuf);
    }

    private static int DrainAll(ISampleProvider provider, float[] buffer)
    {
        int written = 0;
        const int block = 4096;
        while (written < buffer.Length)
        {
            int span = Math.Min(block, buffer.Length - written);
            int n = provider.Read(buffer.AsSpan(written, span));
            if (n == 0) break;
            written += n;
        }
        return written;
    }

    private sealed class ArraySampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private int position;
        public WaveFormat WaveFormat { get; }
        public ArraySampleProvider(float[] data, int sampleRate)
        {
            this.data = data;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        }
        public int Read(Span<float> buffer)
        {
            int avail = Math.Min(buffer.Length, data.Length - position);
            if (avail <= 0) return 0;
            data.AsSpan(position, avail).CopyTo(buffer);
            position += avail;
            return avail;
        }
    }

    // Mirrors WdlResamplingSampleProvider but takes a pre-configured WdlResampler so the
    // benchmark can choose the SRC mode without depending on its default config.
    private sealed class ConfiguredWdlSampleProvider : ISampleProvider
    {
        private readonly WdlResampler resampler;
        private readonly WaveFormat outFormat;
        private readonly ISampleProvider source;
        public ConfiguredWdlSampleProvider(ISampleProvider source, int newSampleRate, WdlResampler configured)
        {
            this.source = source;
            this.resampler = configured;
            outFormat = WaveFormat.CreateIeeeFloatWaveFormat(newSampleRate, source.WaveFormat.Channels);
        }
        public WaveFormat WaveFormat => outFormat;
        public int Read(Span<float> buffer)
        {
            int channels = outFormat.Channels;
            int framesRequested = buffer.Length / channels;
            int inNeeded = resampler.ResamplePrepare(framesRequested, channels, out Span<float> inBuffer);
            int inAvail = source.Read(inBuffer[..(inNeeded * channels)]) / channels;
            int outAvail = resampler.ResampleOut(buffer, inAvail, framesRequested, channels);
            return outAvail * channels;
        }
    }
}
</content>
</invoke>
