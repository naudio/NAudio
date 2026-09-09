using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Core.Tests.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveFormats;

/// <summary>
/// Reading a fmt chunk used to hand back a <see cref="WaveFormatExtraData"/> whatever the
/// encoding was, so a WAVE_FORMAT_EXTENSIBLE file arrived as a bag of bytes that callers had
/// to unpack by hand, while the very same block read through
/// <see cref="WaveFormat.MarshalFromPtr"/> arrived as a <see cref="WaveFormatExtensible"/>.
/// Both routes now make the same choice.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class WaveFormatTypeSelectionTests
{
    private static WaveFormat ReadBackThroughFile(WaveFormat format)
    {
        var bytes = WaveFileBuilder.Build(format, new byte[format.BlockAlign * 4]);
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        return reader.WaveFormat;
    }

    private static WaveFormat ReadBackThroughPointer(WaveFormat format)
    {
        IntPtr pointer = WaveFormat.MarshalToPtr(format);
        try
        {
            return WaveFormat.MarshalFromPtr(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static TestCaseData[] ShippedFormats() =>
    [
        new TestCaseData(new WaveFormat(44100, 16, 2), typeof(WaveFormat)).SetName("Pcm"),
        new TestCaseData(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2), typeof(WaveFormat)).SetName("IeeeFloat"),
        new TestCaseData(WaveFormat.CreateMuLawFormat(8000, 1), typeof(WaveFormat)).SetName("MuLaw"),
        new TestCaseData(new WaveFormatExtensible(48000, 24, 2, 0x3), typeof(WaveFormatExtensible)).SetName("Extensible"),
        new TestCaseData(new AdpcmWaveFormat(22050, 1), typeof(AdpcmWaveFormat)).SetName("Adpcm"),
        new TestCaseData(new Gsm610WaveFormat(), typeof(Gsm610WaveFormat)).SetName("Gsm610"),
        new TestCaseData(new Mp3WaveFormat(44100, 2, 1152, 128000), typeof(Mp3WaveFormat)).SetName("Mp3"),
    ];

    [TestCaseSource(nameof(ShippedFormats))]
    public void ReadingAWavFileYieldsTheMostSpecificFormatType(WaveFormat format, Type expected)
    {
        var readBack = ReadBackThroughFile(format);

        Assert.That(readBack, Is.InstanceOf(expected));
        Assert.That(readBack.Encoding, Is.EqualTo(format.Encoding));
        Assert.That(readBack.SampleRate, Is.EqualTo(format.SampleRate));
        Assert.That(readBack.Channels, Is.EqualTo(format.Channels));
        Assert.That(readBack.BitsPerSample, Is.EqualTo(format.BitsPerSample));
        Assert.That(readBack.BlockAlign, Is.EqualTo(format.BlockAlign));
        Assert.That(readBack.AverageBytesPerSecond, Is.EqualTo(format.AverageBytesPerSecond));
        Assert.That(readBack.ExtraSize, Is.EqualTo(format.ExtraSize));
    }

    /// <summary>
    /// The point of the unification: a format decoded from a file and the same format decoded
    /// from a native WAVEFORMATEX block come back as the same type.
    /// </summary>
    [TestCaseSource(nameof(ShippedFormats))]
    public void FileAndPointerDecodeToTheSameType(WaveFormat format, Type expected)
    {
        Assert.That(ReadBackThroughPointer(format).GetType(),
            Is.EqualTo(ReadBackThroughFile(format).GetType()));
    }

    /// <summary>
    /// Whatever type it comes back as, writing it out again has to reproduce the fmt chunk
    /// byte for byte - a subclass must not declare a cbSize it doesn't then write.
    /// </summary>
    [TestCaseSource(nameof(ShippedFormats))]
    public void ReadBackFormatSerializesToTheSameFmtChunk(WaveFormat format, Type expected)
    {
        Assert.That(Serialize(ReadBackThroughFile(format)), Is.EqualTo(Serialize(format)));
    }

    private static byte[] Serialize(WaveFormat format)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            format.Serialize(writer);
        }
        return ms.ToArray();
    }

    [Test]
    public void PcmWithNoExtraDataIsAPlainWaveFormat()
    {
        var readBack = ReadBackThroughFile(new WaveFormat(44100, 16, 2));

        Assert.That(readBack.GetType(), Is.EqualTo(typeof(WaveFormat)), "exact type");
        Assert.That(readBack.ExtraSize, Is.Zero);
    }

    /// <summary>
    /// A PCM fmt chunk carrying a cbSize of 0 (the 18-byte WAVEFORMATEX form rather than the
    /// canonical 16-byte one) still has no extra data to keep hold of.
    /// </summary>
    [Test]
    public void PcmWithAnExplicitZeroCbSizeIsAPlainWaveFormat()
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((short)WaveFormatEncoding.Pcm);
            writer.Write((short)1);   // channels
            writer.Write(8000);       // sample rate
            writer.Write(16000);      // average bytes per second
            writer.Write((short)2);   // block align
            writer.Write((short)16);  // bits per sample
            writer.Write((short)0);   // cbSize
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        var readBack = WaveFormat.FromFormatChunk(reader, (int)ms.Length);

        Assert.That(readBack.GetType(), Is.EqualTo(typeof(WaveFormat)), "exact type");
        Assert.That(readBack.SampleRate, Is.EqualTo(8000));
        Assert.That(readBack.ExtraSize, Is.Zero);
        Assert.That(ms.Position, Is.EqualTo(ms.Length), "whole chunk consumed");
    }

    /// <summary>
    /// An encoding NAudio has no subclass for still keeps its extra bytes verbatim, which is
    /// what <see cref="WaveFormatExtraData"/> is for.
    /// </summary>
    [Test]
    public void UnrecognisedEncodingKeepsItsExtraDataVerbatim()
    {
        var extra = new byte[] { 1, 2, 3, 4, 5, 6 };
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)WaveFormatEncoding.Vorbis1);
            writer.Write((short)2);    // channels
            writer.Write(44100);       // sample rate
            writer.Write(16000);       // average bytes per second
            writer.Write((short)1);    // block align
            writer.Write((short)0);    // bits per sample
            writer.Write((short)extra.Length);
            writer.Write(extra);
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        var readBack = WaveFormat.FromFormatChunk(reader, (int)ms.Length);

        Assert.That(readBack, Is.InstanceOf<WaveFormatExtraData>());
        Assert.That(((WaveFormatExtraData)readBack).ExtraData, Is.EqualTo(extra));
        Assert.That(ms.Position, Is.EqualTo(ms.Length), "whole chunk consumed");
    }

    /// <summary>
    /// The parser has always trusted the chunk length over a cbSize that disagrees with it,
    /// and the type it picks has to follow suit.
    /// </summary>
    [Test]
    public void ChunkLengthWinsOverAnUnderReportedCbSize()
    {
        var source = new WaveFormatExtensible(48000, 24, 2, 0x3);
        var chunk = Serialize(source);
        // Lie about cbSize; the chunk length still says all 22 extension bytes are there.
        BitConverter.TryWriteBytes(chunk.AsSpan(20), (short)0);

        using var reader = new BinaryReader(new MemoryStream(chunk));
        int chunkLength = reader.ReadInt32();
        var readBack = WaveFormat.FromFormatChunk(reader, chunkLength) as WaveFormatExtensible;

        Assert.That(readBack, Is.Not.Null);
        Assert.That(readBack.SubFormat, Is.EqualTo(source.SubFormat));
        Assert.That(readBack.ChannelMask, Is.EqualTo(0x3));
        Assert.That(readBack.ValidBitsPerSample, Is.EqualTo(24));
    }

    [Test]
    public void AdpcmReadFromAFileKeepsItsCoefficients()
    {
        var source = new AdpcmWaveFormat(22050, 1);
        var readBack = (AdpcmWaveFormat)ReadBackThroughFile(source);

        Assert.That(readBack.SamplesPerBlock, Is.EqualTo(source.SamplesPerBlock));
        Assert.That(readBack.NumCoefficients, Is.EqualTo(source.NumCoefficients));
        Assert.That(readBack.Coefficients, Is.EqualTo(source.Coefficients));
    }

    [Test]
    public void Mp3ReadFromAFileKeepsItsMpegLayer3Fields()
    {
        var source = new Mp3WaveFormat(44100, 2, 1152, 128000);
        var readBack = (Mp3WaveFormat)ReadBackThroughFile(source);

        Assert.That(readBack.id, Is.EqualTo(source.id), "wID");
        Assert.That(readBack.flags, Is.EqualTo(source.flags), "fdwFlags");
        Assert.That(readBack.blockSize, Is.EqualTo(1152), "nBlockSize");
        Assert.That(readBack.framesPerBlock, Is.EqualTo(1), "nFramesPerBlock");
        Assert.That(readBack.codecDelay, Is.EqualTo(0), "nCodecDelay");
    }

    [Test]
    public void ExtensibleReadFromAFileNeedsNoUnpackingToBecomeStandard()
    {
        var readBack = ReadBackThroughFile(new WaveFormatExtensible(44100, 32, 1));

        Assert.That(readBack, Is.InstanceOf<WaveFormatExtensible>());
        Assert.That(readBack.AsStandardWaveFormat().Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
    }

    [Test]
    public void FormatChunkShorterThanPcmWaveFormatIsRejected()
    {
        using var reader = new BinaryReader(new MemoryStream(new byte[16]));

        Assert.Throws<InvalidDataException>(() => WaveFormat.FromFormatChunk(reader, 15));
    }

    [Test]
    public void TruncatedExtraDataKeepsWhatArrivedAndCorrectsCbSize()
    {
        // Chunk length claims 8 extra bytes but only 3 follow the header.
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)WaveFormatEncoding.Vorbis1);
            writer.Write((short)1);   // channels
            writer.Write(8000);       // sample rate
            writer.Write(1000);       // average bytes per second
            writer.Write((short)1);   // block align
            writer.Write((short)0);   // bits per sample
            writer.Write((short)8);   // cbSize
            writer.Write(new byte[] { 7, 8, 9 });
        }
        ms.Position = 0;
        using var reader = new BinaryReader(ms);

        var readBack = WaveFormat.FromFormatChunk(reader, 26);

        Assert.That(readBack.ExtraSize, Is.EqualTo(3), "cbSize corrected to the bytes present");
        Assert.That(((WaveFormatExtraData)readBack).ExtraData, Is.EqualTo(new byte[] { 7, 8, 9 }));
    }
}
