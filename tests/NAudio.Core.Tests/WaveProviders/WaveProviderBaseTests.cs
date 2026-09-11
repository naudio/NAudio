using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveProviders;

[TestFixture]
[Category("UnitTest")]
public class WaveProviderBaseTests
{
    /// <summary>
    /// Writes an ascending ramp of bytes, and records the (offset, count) it was asked for.
    /// </summary>
    private class RampProvider : WaveProviderBase
    {
        private byte next;

        public RampProvider() : base(new WaveFormat(44100, 16, 2)) { }

        public int LastOffset { get; private set; } = -1;
        public int LastCount { get; private set; } = -1;
        public int ReadCalls { get; private set; }

        /// <summary>Bytes to return per read; null means "all that were asked for".</summary>
        public int? BytesToReturn { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            LastOffset = offset;
            LastCount = count;
            ReadCalls++;
            int toWrite = BytesToReturn ?? count;
            for (int i = 0; i < toWrite; i++)
            {
                buffer[offset + i] = next++;
            }
            return toWrite;
        }
    }

    [Test]
    public void ExposesTheWaveFormatItWasConstructedWith()
    {
        var provider = new RampProvider();
        Assert.That(provider.WaveFormat.SampleRate, Is.EqualTo(44100));
        Assert.That(provider.WaveFormat.BitsPerSample, Is.EqualTo(16));
        Assert.That(provider.WaveFormat.Channels, Is.EqualTo(2));
    }

    [Test]
    public void NullWaveFormatThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new NullFormatProvider());
    }

    private class NullFormatProvider : WaveProviderBase
    {
        public NullFormatProvider() : base(null) { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
    }

    [Test]
    public void SpanReadDelegatesToArrayReadAndCopiesBack()
    {
        IWaveProvider provider = new RampProvider();
        var buffer = new byte[6];

        int read = provider.Read(buffer);

        Assert.That(read, Is.EqualTo(6));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void SpanReadAsksForZeroOffsetAndTheSpanLength()
    {
        var ramp = new RampProvider();
        IWaveProvider provider = ramp;
        provider.Read(new byte[5]);

        Assert.That(ramp.LastOffset, Is.EqualTo(0));
        Assert.That(ramp.LastCount, Is.EqualTo(5), "count must be the span length, not the pooled array length");
    }

    [Test]
    public void SpanReadOnlyWritesIntoTheTargetSlice()
    {
        IWaveProvider provider = new RampProvider();
        var buffer = new byte[8];
        Array.Fill(buffer, (byte)0xFF);

        int read = provider.Read(buffer.AsSpan(2, 4));

        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0, 1, 2, 3, 0xFF, 0xFF }));
    }

    [Test]
    public void PartialReadLeavesTheRestOfTheSpanUntouched()
    {
        IWaveProvider provider = new RampProvider { BytesToReturn = 3 };
        var buffer = new byte[6];
        Array.Fill(buffer, (byte)0xFF);

        int read = provider.Read(buffer);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 0xFF, 0xFF, 0xFF }));
    }

    [Test]
    public void EmptySpanReadReturnsZeroWithoutCallingTheDerivedClass()
    {
        var ramp = new RampProvider();
        IWaveProvider provider = ramp;

        Assert.That(provider.Read(Span<byte>.Empty), Is.EqualTo(0));
        Assert.That(ramp.ReadCalls, Is.EqualTo(0));
    }

    [Test]
    public void OverlongReadThrowsRatherThanCorruptingTheCallersBuffer()
    {
        IWaveProvider provider = new RampProvider { BytesToReturn = 4 };

        Assert.Throws<InvalidOperationException>(() => provider.Read(new byte[2]));
    }
}
