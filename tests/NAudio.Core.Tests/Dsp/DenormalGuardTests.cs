using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Dsp;

[TestFixture]
[Category("UnitTest")]
public class DenormalGuardTests
{
    [Test]
    public void FlushZeroesSubnormalRiskValuesOnly()
    {
        // Unchanged: zero, audible values, and values above the 1e-15 threshold.
        Assert.That(DenormalGuard.Flush(0f), Is.EqualTo(0f));
        Assert.That(DenormalGuard.Flush(1f), Is.EqualTo(1f));
        Assert.That(DenormalGuard.Flush(-0.5f), Is.EqualTo(-0.5f));
        Assert.That(DenormalGuard.Flush(1e-10f), Is.EqualTo(1e-10f));
        Assert.That(DenormalGuard.Flush(1e-15f), Is.EqualTo(1e-15f)); // threshold is exclusive

        // Flushed: tiny magnitudes that would decay into the subnormal range.
        Assert.That(DenormalGuard.Flush(5e-16f), Is.EqualTo(0f));
        Assert.That(DenormalGuard.Flush(-5e-16f), Is.EqualTo(0f));
        Assert.That(DenormalGuard.Flush(float.Epsilon), Is.EqualTo(0f));
        Assert.That(DenormalGuard.Flush(-float.Epsilon), Is.EqualTo(0f));
    }

    [Test]
    public void FeedbackTailNeverProducesSubnormals()
    {
        // A decaying reverb tail is the classic denormal trap. After the input
        // stops, every output sample must be exactly zero or a normal float —
        // never a subnormal (which stalls the audio thread on most CPUs).
        var fx = new ReverbEffect { RoomSize = 0.9f, Mix = 1f };
        fx.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));

        var buffer = new float[2 * 48000]; // 1 s
        buffer[0] = 1f;
        buffer[1] = 1f;
        fx.Process(buffer);

        foreach (var s in buffer)
            Assert.That(float.IsSubnormal(s), Is.False, $"subnormal in tail: {s:E}");
    }
}
