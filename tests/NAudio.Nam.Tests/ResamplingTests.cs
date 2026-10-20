using System;
using NAudio.Nam.Internal;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Nam.Tests;

[TestFixture]
[Category("UnitTest")]
public class ResamplingTests
{
    private static readonly ResamplingBridge.BlockProcessor Identity = static _ => { };

    private static float[] Sine(int sampleRate, double frequency, int length, double amplitude = 0.5)
    {
        var x = new float[length];
        for (var i = 0; i < length; i++)
            x[i] = (float)(amplitude * Math.Sin(2 * Math.PI * frequency * i / sampleRate));
        return x;
    }

    /// <summary>Amplitude and signal-to-error (dB) of a least-squares sine fit at a known frequency.</summary>
    private static (double Amplitude, double SnrDb) FitSine(ReadOnlySpan<float> y, double frequency, int sampleRate)
    {
        double ss = 0, sc = 0, cc = 0, ys = 0, yc = 0;
        for (var i = 0; i < y.Length; i++)
        {
            var w = 2 * Math.PI * frequency * i / sampleRate;
            double s = Math.Sin(w), c = Math.Cos(w);
            ss += s * s; cc += c * c; sc += s * c; ys += y[i] * s; yc += y[i] * c;
        }
        var det = ss * cc - sc * sc;
        var a = (ys * cc - yc * sc) / det;
        var b = (yc * ss - ys * sc) / det;
        double signal = 0, error = 0;
        for (var i = 0; i < y.Length; i++)
        {
            var w = 2 * Math.PI * frequency * i / sampleRate;
            var fit = a * Math.Sin(w) + b * Math.Cos(w);
            signal += fit * fit;
            error += (y[i] - fit) * (y[i] - fit);
        }
        return (Math.Sqrt(a * a + b * b), 10 * Math.Log10(signal / error));
    }

    private static void ProcessInRandomBlocks(ResamplingBridge bridge, float[] signal, int seed, ResamplingBridge.BlockProcessor process)
    {
        var random = new Random(seed);
        for (var offset = 0; offset < signal.Length;)
        {
            var n = Math.Min(signal.Length - offset, random.Next(4) switch { 0 => 1, 1 => 4096, _ => 1 + random.Next(600) });
            bridge.Process(signal.AsSpan(offset, n), process);
            offset += n;
        }
    }

    [TestCase(44100, 48000)]
    [TestCase(96000, 48000)]
    [TestCase(32000, 48000)]
    [TestCase(22050, 48000)]
    [TestCase(48000, 44100)]
    public void BridgeKeepsLevelAndAccuracyWithoutUnderruns(int streamRate, int processRate)
    {
        var bridge = new ResamplingBridge(streamRate, processRate);
        var nyquist = Math.Min(streamRate, processRate) / 2.0;
        foreach (var frequency in new[] { 100.0, 1000.0, Math.Min(10000.0, 0.45 * nyquist) })
        {
            bridge.Reset();
            var signal = Sine(streamRate, frequency, streamRate);
            ProcessInRandomBlocks(bridge, signal, seed: (int)frequency, Identity);

            var (amplitude, snr) = FitSine(signal.AsSpan(streamRate / 10), frequency, streamRate);
            Assert.That(20 * Math.Log10(amplitude / 0.5), Is.EqualTo(0).Within(0.05), $"{frequency} Hz level");
            Assert.That(snr, Is.GreaterThan(70), $"{frequency} Hz signal-to-error");
            Assert.That(bridge.UnderrunSamples, Is.Zero, $"{frequency} Hz underruns");
        }
    }

    [TestCase(44100, 48000)]
    [TestCase(88200, 48000)]
    [TestCase(16000, 48000)]
    [TestCase(48000, 44100)]
    public void NoUnderrunsAcrossManyBlockPatterns(int streamRate, int processRate)
    {
        // The FIFO's silent lead-in is sized from a measured worst case; check it holds for
        // block patterns the measurement did not use.
        var bridge = new ResamplingBridge(streamRate, processRate);
        var signal = new float[streamRate];
        for (var seed = 0; seed < 20; seed++)
        {
            bridge.Reset();
            ProcessInRandomBlocks(bridge, signal, seed, Identity);
            Assert.That(bridge.UnderrunSamples, Is.Zero, $"seed {seed}");
        }
    }

    [TestCase(44100, 48000)]
    [TestCase(96000, 48000)]
    [TestCase(48000, 44100)]
    public void ReportedLatencyIsWhereAnImpulseComesOut(int streamRate, int processRate)
    {
        var bridge = new ResamplingBridge(streamRate, processRate);
        var signal = new float[4096];
        signal[100] = 1f;
        bridge.Process(signal, Identity);

        var peak = 0;
        for (var i = 1; i < signal.Length; i++)
        {
            if (Math.Abs(signal[i]) > Math.Abs(signal[peak]))
                peak = i;
        }
        Assert.That(peak - 100, Is.EqualTo(bridge.LatencySamples));
        Assert.That(bridge.LatencySamples * 1000.0 / streamRate, Is.LessThan(3.0), "latency in ms");
    }

    [Test]
    public void EffectResamplesWhenTheStreamRateDiffersFromTheModel()
    {
        var effect = new NamEffect(TestData.LoadA2());
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
        Assert.That(effect.IsResampling, Is.False);
        Assert.That(effect.LatencySamples, Is.Zero);

        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        Assert.That(effect.IsResampling, Is.True);
        Assert.That(effect.LatencySamples, Is.InRange(1, 132)); // under 3 ms
        TestContext.Out.WriteLine($"Latency at 44.1 kHz: {effect.LatencySamples} samples ({effect.LatencySamples / 44.1:F2} ms)");
    }

    [Test]
    public void EffectAt44100MatchesTheModelAt48000()
    {
        // A2 is nonlinear, so compare a steady tone's fundamental level and overall level
        // with the same tone run through the model at its native rate.
        var model = TestData.LoadA2();
        const double frequency = 220;

        var native = Sine(48000, frequency, 48000, 0.3);
        model.CreateProcessor().Process(native, native);

        var effect = new NamEffect(model);
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
        var resampled = Sine(44100, frequency, 44100, 0.3);
        for (var offset = 0; offset < resampled.Length; offset += 441)
            effect.Process(resampled.AsSpan(offset, Math.Min(441, resampled.Length - offset)));

        var nativeFit = FitSine(native.AsSpan(4800), frequency, 48000);
        var resampledFit = FitSine(resampled.AsSpan(4410), frequency, 44100);
        Assert.That(20 * Math.Log10(resampledFit.Amplitude / nativeFit.Amplitude), Is.EqualTo(0).Within(0.1), "fundamental level");
        Assert.That(Rms(resampled.AsSpan(4410)) / Rms(native.AsSpan(4800)), Is.EqualTo(1).Within(0.02), "overall level");
        Assert.That(effect.UnderrunSamples, Is.Zero);
    }

    [Test]
    public void EffectOutputDoesNotDependOnBlockSizeWhenResampling()
    {
        var model = TestData.LoadA2();
        var input = Sine(44100, 330, 22050, 0.4);

        float[] Render(int seed)
        {
            var effect = new NamEffect(model);
            effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
            var buffer = (float[])input.Clone();
            var random = new Random(seed);
            for (var offset = 0; offset < buffer.Length;)
            {
                var n = Math.Min(buffer.Length - offset, 1 + random.Next(seed == 0 ? 1 : 2000));
                effect.Process(buffer.AsSpan(offset, n));
                offset += n;
            }
            return buffer;
        }

        var reference = Render(seed: 1);
        Assert.That(TestData.MaxAbsDifference(Render(seed: 2), reference), Is.LessThan(1e-5f));
        Assert.That(TestData.MaxAbsDifference(Render(seed: 0), reference), Is.LessThan(1e-5f)); // one frame at a time
    }

    [Test]
    public void ResetRestartsTheResampledEffectFromSilence()
    {
        var effect = new NamEffect(TestData.LoadA2());
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
        var input = Sine(44100, 440, 8820, 0.4);
        var first = (float[])input.Clone();
        effect.Process(first);

        effect.Reset();
        var second = (float[])input.Clone();
        effect.Process(second);

        Assert.That(TestData.MaxAbsDifference(first, second), Is.LessThan(1e-6f));
    }

    [Test]
    public void ResampledProcessingDoesNotAllocate()
    {
        var effect = new NamEffect(TestData.LoadA2());
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        var buffer = new float[2048];
        var random = new Random(5);
        for (var i = 0; i < 50; i++)
            effect.Process(buffer.AsSpan(0, 2 * (1 + random.Next(1024))));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 200; i++)
            effect.Process(buffer.AsSpan(0, 2 * (1 + random.Next(1024))));

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }

    private static double Rms(ReadOnlySpan<float> x)
    {
        double sum = 0;
        foreach (var v in x)
            sum += v * v;
        return Math.Sqrt(sum / x.Length);
    }
}
