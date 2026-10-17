using System;
using System.IO;
using System.Numerics;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects;

[TestFixture]
[Category("UnitTest")]
public class CabinetEffectTests
{
    private static WaveFormat Mono48k => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
    private static WaveFormat Stereo48k => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    /// <summary>A cabinet-like IR: a decaying resonance with a little noise.</summary>
    private static float[] CabLikeIr(int length, int seed = 1)
    {
        var rng = new Random(seed);
        var ir = new float[length];
        for (var i = 0; i < length; i++)
            ir[i] = MathF.Exp(-i / 150f) * (MathF.Sin(i * 0.15f) + 0.2f * (float)(rng.NextDouble() * 2 - 1));
        return ir;
    }

    private static float[] ImpulseResponseOf(CabinetEffect effect, int length)
    {
        var buffer = new float[length];
        buffer[0] = 1f;
        effect.Process(buffer);
        return buffer;
    }

    private static Complex Dft(float[] x, double frequency, int sampleRate)
    {
        var sum = Complex.Zero;
        for (var n = 0; n < x.Length; n++)
            sum += x[n] * Complex.Exp(new Complex(0, -2 * Math.PI * frequency * n / sampleRate));
        return sum;
    }

    [Test]
    public void NoImpulseResponseIsPassThrough()
    {
        var fx = new CabinetEffect();
        fx.Configure(Stereo48k);
        var buffer = new[] { 0.1f, -0.2f, 0.3f, -0.4f };
        var expected = (float[])buffer.Clone();

        fx.Process(buffer);

        Assert.That(buffer, Is.EqualTo(expected));
        Assert.That(fx.ImpulseResponseLength, Is.Zero);
    }

    [Test]
    public void AppliesTheImpulseResponseWithNoLatency()
    {
        var ir = CabLikeIr(1000);
        var fx = new CabinetEffect { Normalize = false, TrimSilence = false };
        fx.SetImpulseResponse(ir, 48000);
        fx.Configure(Mono48k);

        var output = ImpulseResponseOf(fx, 1200);

        Assert.That(fx.LatencySamples, Is.Zero);
        Assert.That(fx.Mix, Is.EqualTo(1f));
        for (var i = 0; i < ir.Length; i++)
            Assert.That(output[i], Is.EqualTo(ir[i]).Within(1e-5f), $"sample {i}");
        for (var i = ir.Length; i < output.Length; i++)
            Assert.That(MathF.Abs(output[i]), Is.LessThan(1e-5f), $"sample {i}");
    }

    [Test]
    public void NormalizeGivesTheImpulseResponseUnitEnergy()
    {
        var ir = CabLikeIr(1000);
        for (var i = 0; i < ir.Length; i++)
            ir[i] *= 7f;
        var fx = new CabinetEffect { TrimSilence = false };
        fx.SetImpulseResponse(ir, 48000);
        fx.Configure(Mono48k);

        var output = ImpulseResponseOf(fx, 1200);

        var energy = 0.0;
        foreach (var v in output)
            energy += v * v;
        Assert.That(energy, Is.EqualTo(1.0).Within(1e-3));
    }

    [Test]
    public void TrimSilenceDropsPaddedTail()
    {
        var ir = new float[9600]; // 200 ms, of which only the first 1000 samples are audible
        CabLikeIr(1000).CopyTo(ir, 0);
        var fx = new CabinetEffect();
        fx.SetImpulseResponse(ir, 48000);
        fx.Configure(Mono48k);

        Assert.That(fx.ImpulseResponseLength, Is.LessThanOrEqualTo(1000));

        fx.TrimSilence = false;
        Assert.That(fx.ImpulseResponseLength, Is.EqualTo(9600));
    }

    [Test]
    public void MaxLengthCapsTheImpulseResponse()
    {
        var fx = new CabinetEffect { TrimSilence = false, MaxLength = TimeSpan.FromMilliseconds(10) };
        fx.SetImpulseResponse(CabLikeIr(4800), 48000);
        fx.Configure(Mono48k);

        Assert.That(fx.ImpulseResponseLength, Is.EqualTo(480));
        var output = ImpulseResponseOf(fx, 1000);
        for (var i = 480; i < output.Length; i++)
            Assert.That(MathF.Abs(output[i]), Is.LessThan(1e-5f));
    }

    [TestCase(44100, 48000)]
    [TestCase(96000, 48000)]
    [TestCase(48000, 44100)]
    public void ResamplingKeepsLevelAndPhase(int irRate, int streamRate)
    {
        // A little leading silence, as most IR files have, so the band-limited
        // resampled IR has room for its pre-ringing and should match exactly.
        var ir = new float[irRate / 20 + 64];
        CabLikeIr(irRate / 20, seed: 3).CopyTo(ir, 64);

        AssertResponseMatches(ir, irRate, streamRate, toleranceDb: 0.005, toleranceRadians: 0.002);
    }

    [TestCase(44100, 48000)]
    [TestCase(96000, 48000)]
    public void ResamplingAnIrThatStartsAtFullLevelStaysWithinAFifthOfADecibel(int irRate, int streamRate)
    {
        // Energy right at sample 0: the resampled IR's pre-ringing would fall before
        // time zero, and keeping zero latency means discarding it. The loss is small.
        AssertResponseMatches(CabLikeIr(irRate / 20, seed: 3), irRate, streamRate, toleranceDb: 0.2, toleranceRadians: 0.05);
    }

    private static void AssertResponseMatches(float[] ir, int irRate, int streamRate, double toleranceDb, double toleranceRadians)
    {
        var fx = new CabinetEffect { Normalize = false, TrimSilence = false };
        fx.SetImpulseResponse(ir, irRate);
        fx.Configure(WaveFormat.CreateIeeeFloatWaveFormat(streamRate, 1));

        var output = ImpulseResponseOf(fx, fx.ImpulseResponseLength + 64);

        foreach (var frequency in new[] { 100.0, 1000.0, 5000.0 })
        {
            var original = Dft(ir, frequency, irRate);
            var resampled = Dft(output, frequency, streamRate);
            var levelDb = 20 * Math.Log10(resampled.Magnitude / original.Magnitude);
            var phase = Math.Atan2(Math.Sin(resampled.Phase - original.Phase), Math.Cos(resampled.Phase - original.Phase));
            Assert.That(levelDb, Is.EqualTo(0).Within(toleranceDb), $"{frequency} Hz level");
            // One sample of delay would be 0.65 rad at 5 kHz, so even the looser tolerance catches it.
            Assert.That(phase, Is.EqualTo(0).Within(toleranceRadians), $"{frequency} Hz phase");
        }
    }

    [Test]
    public void MonoInputConvolvesTheChannelAverageOnce()
    {
        var ir = CabLikeIr(500);
        var mono = new CabinetEffect { Normalize = false, TrimSilence = false };
        mono.SetImpulseResponse(ir, 48000);
        mono.Configure(Mono48k);
        var stereo = new CabinetEffect { Normalize = false, TrimSilence = false, MonoInput = true };
        stereo.SetImpulseResponse(ir, 48000);
        stereo.Configure(Stereo48k);

        var frames = 2000;
        var monoBuffer = new float[frames];
        var stereoBuffer = new float[frames * 2];
        for (var i = 0; i < frames; i++)
        {
            stereoBuffer[2 * i] = MathF.Sin(i * 0.03f);
            stereoBuffer[2 * i + 1] = 0.5f * MathF.Sin(i * 0.11f);
            monoBuffer[i] = (stereoBuffer[2 * i] + stereoBuffer[2 * i + 1]) / 2;
        }
        mono.Process(monoBuffer);
        stereo.Process(stereoBuffer);

        for (var i = 0; i < frames; i++)
        {
            Assert.That(stereoBuffer[2 * i], Is.EqualTo(monoBuffer[i]).Within(1e-6f));
            Assert.That(stereoBuffer[2 * i + 1], Is.EqualTo(monoBuffer[i]).Within(1e-6f));
        }
    }

    [Test]
    public void LoadsAStereoWavAsOneImpulseResponsePerChannel()
    {
        var left = CabLikeIr(300, seed: 1);
        var right = CabLikeIr(300, seed: 2);
        var path = Path.Combine(Path.GetTempPath(), $"cab-{Guid.NewGuid():N}.wav");
        try
        {
            using (var writer = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)))
            {
                for (var i = 0; i < left.Length; i++)
                    writer.WriteSamples(new[] { left[i], right[i] }, 0, 2);
            }
            var fx = new CabinetEffect { Normalize = false, TrimSilence = false };
            fx.LoadImpulseResponse(path);
            fx.Configure(Stereo48k);

            var buffer = new float[800];
            buffer[0] = 1f;
            buffer[1] = 1f;
            fx.Process(buffer);

            for (var i = 0; i < left.Length; i++)
            {
                Assert.That(buffer[2 * i], Is.EqualTo(left[i]).Within(1e-5f), $"left {i}");
                Assert.That(buffer[2 * i + 1], Is.EqualTo(right[i]).Within(1e-5f), $"right {i}");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RejectsInvalidSettings()
    {
        var fx = new CabinetEffect();
        Assert.Throws<ArgumentException>(() => fx.PartitionSize = 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => fx.MaxLength = TimeSpan.Zero);
        Assert.Throws<ArgumentException>(() => fx.SetImpulseResponse(Array.Empty<float>(), 48000));
        Assert.Throws<ArgumentOutOfRangeException>(() => fx.SetImpulseResponse(new[] { 1f }, 0));
    }
}
