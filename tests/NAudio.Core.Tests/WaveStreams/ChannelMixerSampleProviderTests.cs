using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
[Category("UnitTest")]
public class ChannelMixerSampleProviderTests
{
    [Test]
    public void NullSourceThrows()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ChannelMixerSampleProvider(null, ChannelMixMatrix.MonoToStereo));
    }

    [Test]
    public void NullMatrixThrows()
    {
        var source = new TestSampleProvider(44100, 1);
        Assert.Throws<ArgumentNullException>(
            () => new ChannelMixerSampleProvider(source, null));
    }

    [Test]
    public void MatrixRowsMustMatchSourceChannels()
    {
        // StereoToMono expects 2 input rows, but the source is mono.
        var source = new TestSampleProvider(44100, 1);
        Assert.Throws<ArgumentException>(
            () => new ChannelMixerSampleProvider(source, ChannelMixMatrix.StereoToMono));
    }

    [Test]
    public void ZeroOutputColumnsThrows()
    {
        var source = new TestSampleProvider(44100, 1);
        var matrix = new float[1, 0];
        Assert.Throws<ArgumentException>(
            () => new ChannelMixerSampleProvider(source, matrix));
    }

    [Test]
    public void OutputFormatReflectsMatrixColumns()
    {
        var source = new TestSampleProvider(48000, 2);
        var mixer = new ChannelMixerSampleProvider(source, ChannelMixMatrix.StereoTo5_1);
        Assert.That(mixer.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
        Assert.That(mixer.WaveFormat.Channels, Is.EqualTo(6));
        Assert.That(mixer.WaveFormat.SampleRate, Is.EqualTo(48000));
    }

    [Test]
    public void MonoToStereoDuplicatesInput()
    {
        var source = new TestSampleProvider(44100, 1) { UseConstValue = true, ConstValue = 3 };
        var mixer = new ChannelMixerSampleProvider(source, ChannelMixMatrix.MonoToStereo);

        var buffer = new float[10];
        var read = mixer.Read(buffer.AsSpan());

        Assert.That(read, Is.EqualTo(10));
        foreach (var sample in buffer)
        {
            Assert.That(sample, Is.EqualTo(3f));
        }
    }

    [Test]
    public void StereoToMonoAveragesChannels()
    {
        // TestSampleProvider emits a ramp 0,1,2,3,... so frame n has left=2n, right=2n+1.
        var source = new TestSampleProvider(44100, 2);
        var mixer = new ChannelMixerSampleProvider(source, ChannelMixMatrix.StereoToMono);

        var buffer = new float[4];
        var read = mixer.Read(buffer.AsSpan());

        Assert.That(read, Is.EqualTo(4));
        for (int frame = 0; frame < 4; frame++)
        {
            var expected = (2 * frame + (2 * frame + 1)) * 0.5f;
            Assert.That(buffer[frame], Is.EqualTo(expected), "frame #" + frame);
        }
    }

    [Test]
    public void IdentityMatrixPassesChannelsThrough()
    {
        var identity = new float[,]
        {
            { 1.0f, 0.0f },
            { 0.0f, 1.0f },
        };
        var source = new TestSampleProvider(44100, 2);
        var mixer = new ChannelMixerSampleProvider(source, identity);

        var buffer = new float[8];
        var read = mixer.Read(buffer.AsSpan());

        Assert.That(read, Is.EqualTo(8));
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.That(buffer[i], Is.EqualTo((float)i), "sample #" + i);
        }
    }

    [Test]
    public void ReadOnlyFillsWholeBlocks()
    {
        // Output is stereo (2 columns); a 5-sample buffer holds 2 whole frames + 1 leftover.
        var source = new TestSampleProvider(44100, 1) { UseConstValue = true, ConstValue = 1 };
        var mixer = new ChannelMixerSampleProvider(source, ChannelMixMatrix.MonoToStereo);

        var buffer = new float[5];
        var read = mixer.Read(buffer.AsSpan());

        Assert.That(read, Is.EqualTo(4), "only whole output frames are produced");
        Assert.That(buffer[4], Is.EqualTo(0f), "trailing partial frame is left untouched");
    }

    [Test]
    public void ReadHonoursEndOfStream()
    {
        // Mono source with only 3 samples available.
        var source = new TestSampleProvider(44100, 1, length: 3);
        var mixer = new ChannelMixerSampleProvider(source, ChannelMixMatrix.MonoToStereo);

        var buffer = new float[20];
        var read = mixer.Read(buffer.AsSpan());

        // 3 input samples = 3 input frames -> 3 output frames -> 6 output samples.
        Assert.That(read, Is.EqualTo(6));
    }
}
