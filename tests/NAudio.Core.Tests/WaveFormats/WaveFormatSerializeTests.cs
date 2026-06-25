using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveFormats;

[TestFixture]
[Category("UnitTest")]
public class WaveFormatSerializeTests
{
    private static (int declaredLength, byte[] body) Serialize(WaveFormat format)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            format.Serialize(writer);
        }
        var bytes = ms.ToArray();
        var declaredLength = System.BitConverter.ToInt32(bytes, 0);
        var body = new byte[bytes.Length - 4];
        System.Array.Copy(bytes, 4, body, 0, body.Length);
        return (declaredLength, body);
    }

    [Test]
    public void PcmSerializesAs16ByteChunkWithoutCbSize()
    {
        var format = new WaveFormat(44100, 16, 2);
        var (declaredLength, body) = Serialize(format);

        Assert.That(declaredLength, Is.EqualTo(16), "PCM fmt chunk length");
        Assert.That(body.Length, Is.EqualTo(16), "PCM fmt body length (no cbSize field)");
    }

    [Test]
    public void NonPcmStillSerializesCbSize()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        var (declaredLength, body) = Serialize(format);

        Assert.That(declaredLength, Is.EqualTo(18), "non-PCM fmt chunk length includes cbSize");
        Assert.That(body.Length, Is.EqualTo(18), "non-PCM fmt body length includes cbSize");
    }

    [Test]
    public void PcmRoundTripsThroughSerialize()
    {
        var format = new WaveFormat(22050, 24, 1);
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            format.Serialize(writer);
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var roundTripped = new WaveFormat(reader);

        Assert.That(roundTripped, Is.EqualTo(format), "Round-tripped format equals original");
        Assert.That(roundTripped.ExtraSize, Is.EqualTo(0), "Extra size");
        Assert.That(ms.Position, Is.EqualTo(ms.Length), "Reader consumed exactly the chunk");
    }

    [Test]
    public void PcmRoundTripsThroughWaveFileWriter()
    {
        var format = new WaveFormat(16000, 16, 1);
        using var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), format))
        {
            writer.Write(new byte[64], 0, 64);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);

        Assert.That(reader.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Pcm));
        Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(16000));
        Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(16));
        Assert.That(reader.WaveFormat.Channels, Is.EqualTo(1));
        Assert.That(reader.Length, Is.EqualTo(64));
    }
}
