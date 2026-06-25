using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

[TestFixture]
[Category("UnitTest")]
public class CompressorEffectTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static float[] Sine(int samples, float amplitude, float hz = 1000f)
    {
        var b = new float[samples];
        for (var i = 0; i < samples; i++)
            b[i] = amplitude * MathF.Sin(i * (2f * MathF.PI * hz / 48000f));
        return b;
    }

    [Test]
    public void SignalWellBelowThresholdIsUntouched()
    {
        var comp = new CompressorEffect { ThresholdDb = -18f, Ratio = 4f };
        comp.Configure(Mono);

        var buffer = Sine(1024, 0.05f); // ≈ -26 dBFS, below threshold − knee
        var expected = (float[])buffer.Clone();
        comp.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
        Assert.That(comp.GainReductionDb, Is.EqualTo(0f));
    }

    [Test]
    public void LoudSignalIsCompressed()
    {
        var comp = new CompressorEffect
        {
            ThresholdDb = -18f,
            Ratio = 4f,
            AttackMs = 5f,
            ReleaseMs = 50f
        };
        comp.Configure(Mono);

        var buffer = Sine(48000, 1.0f);
        comp.Process(buffer);

        float peak = 0f;
        for (var i = buffer.Length - 4800; i < buffer.Length; i++)
            peak = MathF.Max(peak, MathF.Abs(buffer[i]));

        Assert.That(comp.GainReductionDb, Is.GreaterThan(0f));
        Assert.That(peak, Is.LessThan(0.95f));
    }

    [Test]
    public void RmsDetectorRunsCleanly()
    {
        var comp = new CompressorEffect { Detector = DetectorMode.Rms, ThresholdDb = -24f };
        comp.Configure(Mono);

        var buffer = Sine(8192, 0.8f);
        comp.Process(buffer);

        foreach (var s in buffer)
            Assert.That(float.IsFinite(s), Is.True);
        Assert.That(comp.GainReductionDb, Is.GreaterThan(0f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class LimiterEffectTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    [Test]
    public void ReportsLookAheadLatency()
    {
        var limiter = new LimiterEffect { LookaheadMs = 5f };
        limiter.Configure(Mono);
        Assert.That(limiter.LatencySamples, Is.EqualTo(240)); // 5 ms @ 48 kHz
    }

    [Test]
    public void HoldsSteadySignalAtTheCeiling()
    {
        var limiter = new LimiterEffect { CeilingDb = -6f, ReleaseMs = 50f, LookaheadMs = 5f };
        limiter.Configure(Mono);
        var ceilingLinear = MathF.Pow(10f, -6f / 20f);

        var buffer = new float[48000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = MathF.Sin(i * (2f * MathF.PI * 1000f / 48000f));
        limiter.Process(buffer);

        float peak = 0f;
        for (var i = 4800; i < buffer.Length; i++)
            peak = MathF.Max(peak, MathF.Abs(buffer[i]));

        Assert.That(peak, Is.LessThan(ceilingLinear * 1.05f));
        Assert.That(limiter.GainReductionDb, Is.GreaterThan(0f));
    }

    [Test]
    public void QuietSignalPassesThroughDelayed()
    {
        var limiter = new LimiterEffect { CeilingDb = -0.3f, LookaheadMs = 2f };
        limiter.Configure(Mono);
        var latency = limiter.LatencySamples;

        var input = new float[4096];
        for (var i = 0; i < input.Length; i++)
            input[i] = 0.1f * MathF.Sin(i * 0.05f);
        var buffer = (float[])input.Clone();
        limiter.Process(buffer);

        for (var i = latency + 8; i < input.Length; i++)
            Assert.That(buffer[i], Is.EqualTo(input[i - latency]).Within(1e-5f));
    }

    [Test]
    public void ClampsNegativeTimesToZero()
    {
        var limiter = new LimiterEffect();
        limiter.ReleaseMs = -5f;
        Assert.That(limiter.ReleaseMs, Is.EqualTo(0f));
        limiter.LookaheadMs = -5f;
        Assert.That(limiter.LookaheadMs, Is.EqualTo(0f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class GateEffectTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static float[] Sine(int samples, float amplitude)
    {
        var b = new float[samples];
        for (var i = 0; i < samples; i++)
            b[i] = amplitude * MathF.Sin(i * (2f * MathF.PI * 1000f / 48000f));
        return b;
    }

    [Test]
    public void OpensForSignalAboveThreshold()
    {
        var gate = new GateEffect { ThresholdDb = -40f, AttackMs = 1f };
        gate.Configure(Mono);

        var buffer = Sine(48000, 0.8f);
        gate.Process(buffer);

        float peak = 0f;
        for (var i = buffer.Length - 4800; i < buffer.Length; i++)
            peak = MathF.Max(peak, MathF.Abs(buffer[i]));

        Assert.That(peak, Is.GreaterThan(0.6f));
        Assert.That(gate.GainReductionDb, Is.LessThan(3f));
    }

    [Test]
    public void ClosesForSignalBelowThreshold()
    {
        var gate = new GateEffect
        {
            ThresholdDb = -40f,
            RangeDb = -80f,
            HoldMs = 10f,
            ReleaseMs = 50f
        };
        gate.Configure(Mono);

        var input = Sine(48000, 0.0005f); // ≈ -66 dBFS, below threshold
        var buffer = (float[])input.Clone();
        gate.Process(buffer);

        float peak = 0f;
        for (var i = buffer.Length - 4800; i < buffer.Length; i++)
            peak = MathF.Max(peak, MathF.Abs(buffer[i]));

        Assert.That(peak, Is.LessThan(0.0005f * 0.05f)); // heavily attenuated
        Assert.That(gate.GainReductionDb, Is.GreaterThan(20f));
    }
}
