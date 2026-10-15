using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveProviders;

[TestFixture]
[Category("UnitTest")]
public class SampleProviderBaseTests
{
    /// <summary>
    /// Writes an ascending ramp so tests can tell which sample landed where, and records the
    /// (offset, count) it was asked for so we can check the bridge passes them correctly.
    /// </summary>
    private class RampProvider : SampleProviderBase
    {
        private float next;

        public RampProvider(int sampleRate = 44100, int channels = 1) : base(sampleRate, channels) { }

        public int LastOffset { get; private set; } = -1;
        public int LastCount { get; private set; } = -1;
        public int ReadCalls { get; private set; }

        /// <summary>Samples to return per read; null means "all that were asked for".</summary>
        public int? SamplesToReturn { get; set; }

        public override int Read(float[] buffer, int offset, int count)
        {
            LastOffset = offset;
            LastCount = count;
            ReadCalls++;
            int toWrite = SamplesToReturn ?? count;
            for (int i = 0; i < toWrite; i++)
            {
                buffer[offset + i] = next++;
            }
            return toWrite;
        }
    }

    [Test]
    public void DefaultConstructorIs44100Mono()
    {
        var provider = new RampProvider();
        Assert.That(provider.WaveFormat.SampleRate, Is.EqualTo(44100));
        Assert.That(provider.WaveFormat.Channels, Is.EqualTo(1));
        Assert.That(provider.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
    }

    [Test]
    public void SetWaveFormatUpdatesTheFormat()
    {
        var provider = new RampProvider();
        provider.SetWaveFormat(48000, 2);
        Assert.That(provider.WaveFormat.SampleRate, Is.EqualTo(48000));
        Assert.That(provider.WaveFormat.Channels, Is.EqualTo(2));
    }

    [Test]
    public void SpanReadDelegatesToArrayReadAndCopiesBack()
    {
        ISampleProvider provider = new RampProvider();
        var buffer = new float[8];
        int read = provider.Read(buffer);

        Assert.That(read, Is.EqualTo(8));
        Assert.That(buffer, Is.EqualTo(new float[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
    }

    [Test]
    public void SpanReadAsksForZeroOffsetAndTheSpanLength()
    {
        var ramp = new RampProvider();
        ISampleProvider provider = ramp;
        provider.Read(new float[5]);

        Assert.That(ramp.LastOffset, Is.EqualTo(0));
        Assert.That(ramp.LastCount, Is.EqualTo(5), "count must be the span length, not the pooled array length");
    }

    [Test]
    public void SpanReadOnlyWritesIntoTheTargetSlice()
    {
        var ramp = new RampProvider();
        ISampleProvider provider = ramp;
        var buffer = new float[8];
        Array.Fill(buffer, -1f);

        int read = provider.Read(buffer.AsSpan(2, 4));

        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new float[] { -1, -1, 0, 1, 2, 3, -1, -1 }));
    }

    [Test]
    public void PartialReadLeavesTheRestOfTheSpanUntouched()
    {
        var ramp = new RampProvider { SamplesToReturn = 3 };
        ISampleProvider provider = ramp;
        var buffer = new float[6];
        Array.Fill(buffer, -1f);

        int read = provider.Read(buffer);

        Assert.That(read, Is.EqualTo(3));
        Assert.That(buffer, Is.EqualTo(new float[] { 0, 1, 2, -1, -1, -1 }));
    }

    [Test]
    public void EmptySpanReadReturnsZeroWithoutCallingTheDerivedClass()
    {
        var ramp = new RampProvider();
        ISampleProvider provider = ramp;

        Assert.That(provider.Read(Span<float>.Empty), Is.EqualTo(0));
        Assert.That(ramp.ReadCalls, Is.EqualTo(0));
    }

    [Test]
    public void OverlongReadThrowsRatherThanCorruptingTheCallersBuffer()
    {
        var ramp = new RampProvider { SamplesToReturn = 4 };
        ISampleProvider provider = ramp;

        // the pooled array is at least 4 long, so the derived class can write 4 samples even
        // though only 2 were asked for — the bridge must catch that rather than overrun
        Assert.Throws<InvalidOperationException>(() => provider.Read(new float[2]));
    }

    [Test]
    public void CanAlsoBeReadAsAWaveProvider()
    {
        IWaveProvider provider = new RampProvider();
        var buffer = new byte[16];

        int bytesRead = provider.Read(buffer);

        Assert.That(bytesRead, Is.EqualTo(16));
        for (int i = 0; i < 4; i++)
        {
            Assert.That(BitConverter.ToSingle(buffer, i * 4), Is.EqualTo((float)i));
        }
    }

    [Test]
    public void PlugsIntoTheBuiltInSampleProviderChain()
    {
        var provider = new RampProvider();
        var volume = new NAudio.Wave.SampleProviders.VolumeSampleProvider(provider) { Volume = 0.5f };
        var buffer = new float[4];

        int read = volume.Read(buffer);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new float[] { 0f, 0.5f, 1f, 1.5f }));
    }
}
