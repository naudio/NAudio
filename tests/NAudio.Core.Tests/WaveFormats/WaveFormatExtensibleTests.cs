using System;
using System.IO;
using NAudio.Dmo;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveFormats;

[TestFixture]
[Category("UnitTest")]
public class WaveFormatExtensibleTests
{
    [Test]
    public void ConvenienceConstructorPicksSubFormatFromBitDepth()
    {
        var pcm = new WaveFormatExtensible(44100, 16, 2);
        Assert.That(pcm.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_PCM));
        Assert.That(pcm.ValidBitsPerSample, Is.EqualTo(16));

        var ieee = new WaveFormatExtensible(44100, 32, 2);
        Assert.That(ieee.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT));
    }

    [Test]
    public void ConvenienceConstructorDerivesDefaultChannelMask()
    {
        // No mask supplied for stereo => the low two channel bits are set
        var format = new WaveFormatExtensible(44100, 16, 2);
        Assert.That(format.ChannelMask, Is.EqualTo((int)Speakers.Stereo));
    }

    [Test]
    public void FullConstructorCanCreate32BitIntegerPcm()
    {
        // Previously impossible via the public API: the convenience constructor always
        // pins 32 bit to IEEE float.
        var format = new WaveFormatExtensible(48000, 32, 2,
            AudioMediaSubtypes.MEDIASUBTYPE_PCM, validBitsPerSample: 32, channelMask: 0);

        Assert.That(format.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_PCM));
        Assert.That(format.BitsPerSample, Is.EqualTo(32));
        Assert.That(format.ValidBitsPerSample, Is.EqualTo(32));
        Assert.That(format.Encoding, Is.EqualTo(WaveFormatEncoding.Extensible));
    }

    [Test]
    public void FullConstructorSupportsValidBitsLessThanContainer()
    {
        // 24 valid bits packed into a 32-bit container
        var format = new WaveFormatExtensible(96000, 32, 2,
            AudioMediaSubtypes.MEDIASUBTYPE_PCM, validBitsPerSample: 24, channelMask: 0);

        Assert.That(format.BitsPerSample, Is.EqualTo(32));
        Assert.That(format.ValidBitsPerSample, Is.EqualTo(24));
        Assert.That(format.BlockAlign, Is.EqualTo(2 * 4), "Block align derives from the 32-bit container");
    }

    [Test]
    public void SpeakersOverloadSetsChannelMask()
    {
        var format = new WaveFormatExtensible(48000, 32, 6,
            AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT, 32, Speakers.Surround51);

        Assert.That(format.ChannelMask, Is.EqualTo((int)Speakers.Surround51));
    }

    [Test]
    public void FullConstructorPreservesExplicitChannelMask()
    {
        var mask = (int)(Speakers.FrontLeft | Speakers.FrontRight | Speakers.LowFrequency);
        var format = new WaveFormatExtensible(48000, 16, 3,
            AudioMediaSubtypes.MEDIASUBTYPE_PCM, 16, mask);

        Assert.That(format.ChannelMask, Is.EqualTo(mask));
    }

    [Test]
    public void UseIeeeFloatConstructorPicksPcmSubFormat()
    {
        // The headline case the bit-depth constructor can't reach: 32-bit integer PCM
        var format = new WaveFormatExtensible(48000, 32, 2,
            useIeeeFloat: false, validBitsPerSample: 24, channelMask: (int)Speakers.Stereo);

        Assert.That(format.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_PCM));
        Assert.That(format.BitsPerSample, Is.EqualTo(32));
        Assert.That(format.ValidBitsPerSample, Is.EqualTo(24));
        Assert.That(format.ChannelMask, Is.EqualTo((int)Speakers.Stereo));
    }

    [Test]
    public void UseIeeeFloatConstructorPicksFloatSubFormat()
    {
        var format = new WaveFormatExtensible(48000, 32, 6,
            useIeeeFloat: true, validBitsPerSample: 32, channelMask: Speakers.Surround51);

        Assert.That(format.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT));
        Assert.That(format.ChannelMask, Is.EqualTo((int)Speakers.Surround51));
    }

    [Test]
    public void RoundTripsThroughSerialize()
    {
        var format = new WaveFormatExtensible(48000, 32, 2,
            AudioMediaSubtypes.MEDIASUBTYPE_PCM, validBitsPerSample: 24, channelMask: (int)Speakers.Stereo);

        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            format.Serialize(writer);
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        // chunk length precedes the body; FromFormatChunk expects it to be consumed first
        var chunkLength = reader.ReadInt32();
        var readBack = WaveFormat.FromFormatChunk(reader, chunkLength);

        Assert.That(readBack.Encoding, Is.EqualTo(WaveFormatEncoding.Extensible));
        Assert.That(readBack.ExtraSize, Is.EqualTo(22));

        var extra = (WaveFormatExtraData)readBack;
        // SubFormat GUID lives after wValidBitsPerSample (2) + dwChannelMask (4)
        var subFormat = new Guid(extra.ExtraData.AsSpan(6, 16));
        Assert.That(subFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_PCM));
        Assert.That(BitConverter.ToInt16(extra.ExtraData, 0), Is.EqualTo((short)24), "valid bits");
        Assert.That(BitConverter.ToInt32(extra.ExtraData, 2), Is.EqualTo((int)Speakers.Stereo), "channel mask");
    }
}
