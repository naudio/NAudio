using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

[TestFixture]
[Category("UnitTest")]
public class GainEffectTests
{
    [Test]
    public void SixDbGainRoughlyDoublesAmplitude()
    {
        var effect = new GainEffect { GainDb = 20f * MathF.Log10(2f) };
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        var buffer = new float[64];
        Array.Fill(buffer, 1f);
        effect.Process(buffer);

        foreach (var sample in buffer)
            Assert.That(sample, Is.EqualTo(2f).Within(1e-3f));
    }

    [Test]
    public void UnityGainIsTransparent()
    {
        var effect = new GainEffect();
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));

        var buffer = new float[] { 0.1f, -0.2f, 0.3f, -0.4f };
        var expected = (float[])buffer.Clone();
        effect.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class PanEffectTests
{
    private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    [Test]
    public void CentreIsConstantPower()
    {
        var effect = new PanEffect();
        effect.Configure(Stereo);

        var buffer = new[] { 1f, 1f };
        effect.Process(buffer);

        Assert.That(buffer[0], Is.EqualTo(0.70710677f).Within(1e-4f));
        Assert.That(buffer[1], Is.EqualTo(0.70710677f).Within(1e-4f));
    }

    [Test]
    public void HardLeftSilencesRightChannel()
    {
        var effect = new PanEffect { Pan = -1f };
        effect.Configure(Stereo);

        var buffer = new[] { 1f, 1f, 1f, 1f };
        effect.Process(buffer);

        Assert.That(buffer[0], Is.EqualTo(1f).Within(1e-4f));
        Assert.That(buffer[1], Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void NonStereoPassesThrough()
    {
        var effect = new PanEffect { Pan = -1f };
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        var buffer = new[] { 0.5f, 0.5f, 0.5f };
        effect.Process(buffer);

        foreach (var sample in buffer)
            Assert.That(sample, Is.EqualTo(0.5f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class StereoWidthEffectTests
{
    private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    [Test]
    public void ZeroWidthCollapsesToMono()
    {
        var effect = new StereoWidthEffect { Width = 0f };
        effect.Configure(Stereo);

        var buffer = new[] { 1f, 3f };
        effect.Process(buffer);

        Assert.That(buffer[0], Is.EqualTo(2f).Within(1e-6f));
        Assert.That(buffer[1], Is.EqualTo(2f).Within(1e-6f));
    }

    [Test]
    public void UnityWidthIsTransparent()
    {
        var effect = new StereoWidthEffect();
        effect.Configure(Stereo);

        var buffer = new[] { 0.2f, -0.6f, 0.9f, 0.1f };
        var expected = (float[])buffer.Clone();
        effect.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class DcBlockerEffectTests
{
    [Test]
    public void RemovesAConstantOffset()
    {
        var effect = new DcBlockerEffect();
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        var buffer = new float[2048];
        float last = 0f;
        for (var block = 0; block < 20; block++)
        {
            Array.Fill(buffer, 1f);
            effect.Process(buffer);
            last = buffer[^1];
        }

        Assert.That(MathF.Abs(last), Is.LessThan(1e-2f));
    }

    [Test]
    public void ClampsNegativeCutoffToZero()
    {
        var effect = new DcBlockerEffect();
        effect.CutoffFrequency = -5f;
        Assert.That(effect.CutoffFrequency, Is.EqualTo(0f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class MonoMakerEffectTests
{
    private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    [Test]
    public void CollapsesLowFrequencySideContent()
    {
        // Frequency above Nyquist clamps the side low-pass wide open, so the whole
        // side band is removed: a hard-panned (pure-side) signal becomes mono.
        var effect = new MonoMakerEffect { Frequency = 30000f };
        effect.Configure(Stereo);

        var buffer = new float[4096];
        for (var i = 0; i + 1 < buffer.Length; i += 2)
        {
            var s = MathF.Sin(i * 0.01f);
            buffer[i] = s;       // L
            buffer[i + 1] = -s;  // R  (pure side, zero mid)
        }
        effect.Process(buffer);

        float maxResidual = 0f;
        for (var i = buffer.Length - 256; i < buffer.Length; i++)
            maxResidual = MathF.Max(maxResidual, MathF.Abs(buffer[i]));

        Assert.That(maxResidual, Is.LessThan(0.1f));
    }

    [Test]
    public void NonStereoPassesThrough()
    {
        var effect = new MonoMakerEffect();
        effect.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        var buffer = new[] { 0.4f, -0.4f, 0.4f };
        var expected = (float[])buffer.Clone();
        effect.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
    }
}

[TestFixture]
[Category("UnitTest")]
public class EqualizerTests
{
    private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

    [Test]
    public void ZeroGainPeakingBandIsTransparent()
    {
        var eq = new Equalizer(EqualizerBand.Peaking(1000f, 1f, 0f));
        eq.Configure(Mono);

        var buffer = new float[512];
        var expected = new float[buffer.Length];
        for (var i = 0; i < buffer.Length; i++)
            expected[i] = buffer[i] = MathF.Sin(i * 0.05f);

        eq.Process(buffer);

        for (var i = 0; i < buffer.Length; i++)
            Assert.That(buffer[i], Is.EqualTo(expected[i]).Within(1e-4f));
    }

    [Test]
    public void NoBandsIsExactPassThrough()
    {
        var eq = new Equalizer();
        eq.Configure(Mono);

        var buffer = new[] { 0.1f, -0.2f, 0.3f };
        var expected = (float[])buffer.Clone();
        eq.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected));
    }

    [Test]
    public void UpdatingABandRetunesWithoutBlowingUp()
    {
        var eq = new Equalizer(EqualizerBand.Peaking(1000f, 1f, 0f));
        eq.Configure(Mono);

        eq.Bands[0].GainDb = 12f;
        eq.Update();

        var buffer = new float[8192];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = MathF.Sin(i * (2f * MathF.PI * 1000f / 48000f));
        eq.Process(buffer);

        float peak = 0f;
        for (var i = buffer.Length - 512; i < buffer.Length; i++)
        {
            Assert.That(float.IsFinite(buffer[i]), Is.True);
            peak = MathF.Max(peak, MathF.Abs(buffer[i]));
        }

        Assert.That(peak, Is.GreaterThan(1.05f)); // +12 dB at the centre lifts a unit tone
    }
}

[TestFixture]
[Category("UnitTest")]
public class GraphicEqualizerTests
{
    [Test]
    public void OctaveLayoutHasTenBands()
    {
        var eq = new GraphicEqualizer(GraphicEqualizerLayout.TenBandOctave);
        Assert.That(eq.BandCount, Is.EqualTo(10));
        Assert.That(eq.GetCentreFrequency(0), Is.EqualTo(31.5f));
        Assert.That(eq.GetCentreFrequency(9), Is.EqualTo(16000f));
    }

    [Test]
    public void ThirdOctaveLayoutHasThirtyOneBands()
    {
        var eq = new GraphicEqualizer(GraphicEqualizerLayout.ThirtyOneBandThirdOctave);
        Assert.That(eq.BandCount, Is.EqualTo(31));
    }

    [Test]
    public void SetBandGainRoundTrips()
    {
        var eq = new GraphicEqualizer();
        eq.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        eq.SetBandGain(3, -6f);

        Assert.That(eq.GetBandGain(3), Is.EqualTo(-6f));
    }

    [Test]
    public void FlatResponseIsTransparent()
    {
        var eq = new GraphicEqualizer();
        eq.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

        var buffer = new float[256];
        var expected = new float[buffer.Length];
        for (var i = 0; i < buffer.Length; i++)
            expected[i] = buffer[i] = MathF.Sin(i * 0.03f);

        eq.Process(buffer);

        for (var i = 0; i < buffer.Length; i++)
            Assert.That(buffer[i], Is.EqualTo(expected[i]).Within(1e-3f));
    }
}
