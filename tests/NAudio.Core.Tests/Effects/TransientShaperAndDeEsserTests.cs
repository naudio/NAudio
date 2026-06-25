using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

[TestFixture]
[Category("UnitTest")]
public class TransientShaperEffectTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static float[] DecayingBurst(int n)
    {
        var b = new float[n];
        for (var i = 0; i < n; i++)
            b[i] = MathF.Exp(-i / 600f) * MathF.Sin(i * (2f * MathF.PI * 1000f / 48000f));
        return b;
    }

    private static float Peak(float[] b, int start, int end)
    {
        var p = 0f;
        for (var i = start; i < end; i++)
            p = MathF.Max(p, MathF.Abs(b[i]));
        return p;
    }

    private static double Energy(float[] b, int start, int end)
    {
        double e = 0;
        for (var i = start; i < end; i++)
            e += b[i] * (double)b[i];
        return e;
    }

    [Test]
    public void PositiveAttackSharpensTheOnset()
    {
        var flat = new TransientShaperEffect { AttackDb = 0f };
        flat.Configure(Mono);
        var a = DecayingBurst(4000);
        flat.Process(a);

        var boosted = new TransientShaperEffect { AttackDb = 12f };
        boosted.Configure(Mono);
        var b = DecayingBurst(4000);
        boosted.Process(b);

        Assert.That(Peak(b, 0, 400), Is.GreaterThan(Peak(a, 0, 400) * 1.05f));
        foreach (var s in b)
            Assert.That(float.IsFinite(s), Is.True);
    }

    [Test]
    public void NegativeSustainTightensTheTail()
    {
        var flat = new TransientShaperEffect { SustainDb = 0f };
        flat.Configure(Mono);
        var a = DecayingBurst(4000);
        flat.Process(a);

        var tight = new TransientShaperEffect { SustainDb = -12f };
        tight.Configure(Mono);
        var b = DecayingBurst(4000);
        tight.Process(b);

        Assert.That(Energy(b, 2000, 3800), Is.LessThan(Energy(a, 2000, 3800) * 0.9));
    }
}

[TestFixture]
[Category("UnitTest")]
public class DeEsserEffectTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    private static float Rms(float[] b)
    {
        double s = 0;
        foreach (var v in b)
            s += v * (double)v;
        return MathF.Sqrt((float)(s / b.Length));
    }

    [Test]
    public void ReducesLoudSibilance()
    {
        var de = new DeEsserEffect { CrossoverFrequency = 6000f, ThresholdDb = -30f, Ratio = 6f };
        de.Configure(Mono);

        var buffer = new float[48000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = 0.3f * MathF.Sin(i * (2f * MathF.PI * 200f / 48000f))
                      + 0.6f * MathF.Sin(i * (2f * MathF.PI * 9000f / 48000f));
        var inputRms = Rms(buffer);

        de.Process(buffer);

        foreach (var s in buffer)
            Assert.That(float.IsFinite(s), Is.True);
        Assert.That(de.GainReductionDb, Is.GreaterThan(0f));
        Assert.That(Rms(buffer), Is.LessThan(inputRms));
    }

    [Test]
    public void NonSibilantSignalPassesThroughAtUnityEnergy()
    {
        var de = new DeEsserEffect { CrossoverFrequency = 6000f, ThresholdDb = -30f };
        de.Configure(Mono);

        var buffer = new float[48000];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = 0.3f * MathF.Sin(i * (2f * MathF.PI * 200f / 48000f));
        var inputRms = Rms(buffer);

        de.Process(buffer);

        Assert.That(de.GainReductionDb, Is.LessThan(1f));
        Assert.That(Rms(buffer), Is.EqualTo(inputRms).Within(inputRms * 0.2f));
    }
}
