using System;
using NAudio.Wave;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests;

[TestFixture]
public class AlsaFormatTests
{
    [Test]
    public void MapsIeeeFloat32()
        => Assert.That(AlsaFormat.FromWaveFormat(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)),
            Is.EqualTo(PCMFormat.SND_PCM_FORMAT_FLOAT_LE));

    [Test]
    public void MapsPcmBitDepths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AlsaFormat.FromWaveFormat(new WaveFormat(44100, 8, 2)),
                Is.EqualTo(PCMFormat.SND_PCM_FORMAT_U8));
            Assert.That(AlsaFormat.FromWaveFormat(new WaveFormat(44100, 16, 2)),
                Is.EqualTo(PCMFormat.SND_PCM_FORMAT_S16_LE));
            Assert.That(AlsaFormat.FromWaveFormat(new WaveFormat(44100, 24, 2)),
                Is.EqualTo(PCMFormat.SND_PCM_FORMAT_S24_3LE));
            Assert.That(AlsaFormat.FromWaveFormat(new WaveFormat(44100, 32, 2)),
                Is.EqualTo(PCMFormat.SND_PCM_FORMAT_S32_LE));
        });
    }

    [Test]
    public void UnsupportedEncodingThrows()
        => Assert.Throws<NotSupportedException>(
            () => AlsaFormat.FromWaveFormat(WaveFormat.CreateALawFormat(8000, 1)));

    [TestCase(16, 2, 4)]
    [TestCase(24, 2, 6)]
    [TestCase(32, 2, 8)]
    [TestCase(16, 1, 2)]
    public void FrameBytesIsBitsOverEightTimesChannels(int bits, int channels, int expected)
        => Assert.That(AlsaFormat.FrameBytes(new WaveFormat(44100, bits, channels)), Is.EqualTo(expected));
}
