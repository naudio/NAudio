using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp;

[TestFixture]
[Category("UnitTest")]
public class DelayLineTests
{
    [Test]
    public void IntegerReadReturnsThePastSample()
    {
        var line = new DelayLine(8);
        for (var k = 0; k < 100; k++)
            line.Write(k);

        // Read(1) is the most recent write; Read(d) is d samples back.
        Assert.That(line.Read(1), Is.EqualTo(99f));
        Assert.That(line.Read(4), Is.EqualTo(96f));
        Assert.That(line.Read(line.MaxDelaySamples), Is.EqualTo(100f - line.MaxDelaySamples));
    }

    [Test]
    public void FractionalReadInterpolatesLinearly()
    {
        var line = new DelayLine(8);
        for (var k = 0; k < 100; k++)
            line.Write(k); // a linear ramp: value == sample index

        // On a linear ramp the interpolated read at delay d must equal 100 - d.
        for (var d = 1f; d <= line.MaxDelaySamples; d += 0.25f)
            Assert.That(line.Read(d), Is.EqualTo(100f - d).Within(1e-3f), $"d={d}");
    }

    [Test]
    public void FractionalReadAtTheMaximumDoesNotWrapToTheNewestSample()
    {
        // Regression: a fractional read at (and just below) MaxDelaySamples used
        // to interpolate against the *newest* sample because the internal buffer
        // had no slot for the older neighbour — a large discontinuity.
        var line = new DelayLine(16);
        for (var k = 0; k < 500; k++)
            line.Write(k);

        var max = line.MaxDelaySamples;
        var justBelow = line.Read(max - 0.5f);
        var atMax = line.Read(max);

        Assert.That(justBelow, Is.EqualTo(500f - (max - 0.5f)).Within(1e-3f));
        Assert.That(atMax, Is.EqualTo(500f - max).Within(1e-3f));
        // Continuous across the boundary (no glitch jump).
        Assert.That(MathF.Abs(justBelow - atMax), Is.LessThan(1f));
    }

    [Test]
    public void OutOfRangeReadsThrow()
    {
        var line = new DelayLine(8);
        line.Write(1f);
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(line.MaxDelaySamples + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(0.5f));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.Read(line.MaxDelaySamples + 0.01f));
    }
}
