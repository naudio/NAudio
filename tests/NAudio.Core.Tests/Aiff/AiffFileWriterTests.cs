using System;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Tests.Shared;
using NUnit.Framework;

namespace NAudio.Core.Tests.Aiff;

[TestFixture]
[Category("UnitTest")]
public class AiffFileWriterTests
{
    [Test]
    public void ReaderShouldReadBackSameDataWrittenWithWrite()
    {
        var sourceData = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
        var format = new WaveFormat(16000, 24, 1);

        var roundTripped = WriteAndRead(format, writer => writer.Write(sourceData, 0, sourceData.Length));

        Assert.That(roundTripped, Is.EqualTo(sourceData));
    }

    [Test]
    public void WriteWithOffsetAndCountShouldWriteOnlyRequestedSlice()
    {
        var sourceData = new byte[] { 0xAA, 0x11, 0x22, 0x33, 0x44, 0xBB };
        var expectedSlice = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var format = new WaveFormat(16000, 16, 1);

        var roundTripped = WriteAndRead(format, writer => writer.Write(sourceData, 1, 4));

        Assert.That(roundTripped, Is.EqualTo(expectedSlice));
    }

    [Test]
    public void WriteSample16BitShouldRoundTripExpectedPcmValue()
    {
        var format = new WaveFormat(44100, 16, 1);

        var roundTripped = WriteAndRead(format, writer => writer.WriteSample(1.0f));

        Assert.That(roundTripped, Is.EqualTo(new byte[] { 0xFF, 0x7F }));
    }

    [Test]
    public void WriteSample24BitShouldRoundTripExpectedPcmValue()
    {
        var format = new WaveFormat(44100, 24, 1);

        var roundTripped = WriteAndRead(format, writer =>
        {
            writer.WriteSample(1.0f);
            writer.WriteSample(0.5f);
        });

        Assert.That(roundTripped, Is.EqualTo(new byte[] { 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0x3F }));
    }

    [Test]
    public void WriteSample32BitExtensibleShouldNotWriteSilenceForNonZeroSample()
    {
        var format = new WaveFormatExtensible(44100, 32, 1);

        var roundTripped = WriteAndRead(format, writer => writer.WriteSample(0.5f));

        Assert.That(roundTripped, Has.Some.Not.EqualTo((byte)0));
    }

    [Test]
    public void WriteSampleShouldSupportIeeeFloatFormat()
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

        var roundTripped = WriteAndRead(format, writer => writer.WriteSample(0.5f));

        Assert.That(roundTripped, Is.EqualTo(BitConverter.GetBytes(0.5f)));
    }

    [Test]
    public void WriteSamplesShortTo24BitShouldScaleTo24BitRange()
    {
        var format = new WaveFormat(44100, 24, 1);
        var samples = new short[] { 1, 2 };

        var roundTripped = WriteAndRead(format, writer => writer.WriteSamples(samples, 0, samples.Length));

        Assert.That(roundTripped, Is.EqualTo(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x00 }));
    }

    [Test]
    public void WriteSamplesShortTo32BitShouldScaleTo32BitRange()
    {
        var format = new WaveFormatExtensible(44100, 32, 1);
        var samples = new short[] { 1 };

        var roundTripped = WriteAndRead(format, writer => writer.WriteSamples(samples, 0, samples.Length));

        Assert.That(roundTripped, Is.EqualTo(new byte[] { 0x00, 0x00, 0x01, 0x00 }));
    }

    [Test]
    public void CreateAiffFileShouldCreateFileWithExpectedFormatAndLength()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".aiff");
        const int expectedLength = 3000;
        var format = new WaveFormat(22050, 16, 2);

        try
        {
            AiffFileWriter.CreateAiffFile(tempFile, new NullWaveStream(format, expectedLength));

            using var reader = new AiffFileReader(tempFile);
            Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(format.SampleRate));
            Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(format.BitsPerSample));
            Assert.That(reader.WaveFormat.Channels, Is.EqualTo(format.Channels));
            Assert.That(reader.Length, Is.EqualTo(expectedLength));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void StreamConstructorLeavesCallerSuppliedStreamOpen()
    {
        // The stream constructor must not dispose a stream the caller handed in. Disposing
        // the writer still finalizes the header, but leaves the MemoryStream usable.
        var ms = new MemoryStream();
        var sourceData = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60 };
        using (var writer = new AiffFileWriter(ms, new WaveFormat(16000, 24, 1)))
        {
            writer.Write(sourceData, 0, sourceData.Length);
        }

        // If the writer had disposed ms, accessing it would throw ObjectDisposedException.
        Assert.That(ms.CanRead, Is.True, "Caller's stream should still be open");
        ms.Position = 0;
        using var reader = new AiffFileReader(ms);
        var buffer = new byte[(int)reader.Length];
        var read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length));
        Assert.That(buffer, Is.EqualTo(sourceData));
    }

    // AIFF 8-bit PCM is signed; the bytes handed to Write are unsigned (WAV-style), so the
    // writer must flip the sign bit. Asserting on the raw on-disk SSND bytes (rather than a
    // round-trip, where the reader's matching flip would cancel the bug out). See #1178.
    [Test]
    public void Write8BitStoresSignedSamplesOnDisk()
    {
        var ms = new MemoryStream();
        var format = new WaveFormat(8000, 8, 1);
        var unsignedInput = new byte[] { 0x80, 0x8A, 0x00, 0xFF }; // 128 (silence), 138, 0, 255

        using (var writer = new AiffFileWriter(new IgnoreDisposeStream(ms), format))
        {
            writer.Write(unsignedInput, 0, unsignedInput.Length);
        }

        var onDisk = ExtractSsndSoundData(ms.ToArray());
        Assert.That(onDisk, Is.EqualTo(new byte[] { 0x00, 0x0A, 0x80, 0x7F })); // unsigned ^ 0x80
    }

    [Test]
    public void Write8BitDoesNotMutateCallerBuffer()
    {
        var ms = new MemoryStream();
        var format = new WaveFormat(8000, 8, 1);
        var input = new byte[] { 0x80, 0x8A, 0x00, 0xFF };
        var original = (byte[])input.Clone();

        using (var writer = new AiffFileWriter(new IgnoreDisposeStream(ms), format))
        {
            writer.Write(input, 0, input.Length);
        }

        Assert.That(input, Is.EqualTo(original));
    }

    [Test]
    public void EightBitRoundTripsThroughWriterAndReader()
    {
        var format = new WaveFormat(8000, 8, 1);
        var unsigned = new byte[] { 0x00, 0x40, 0x80, 0xC0, 0xFF, 0x8A };

        var roundTripped = WriteAndRead(format, writer => writer.Write(unsigned, 0, unsigned.Length));

        Assert.That(roundTripped, Is.EqualTo(unsigned));
    }

    // Locates the SSND chunk's sound data within a written AIFF file.
    private static byte[] ExtractSsndSoundData(byte[] aiff)
    {
        for (int i = 0; i + 16 <= aiff.Length; i++)
        {
            if (aiff[i] == (byte)'S' && aiff[i + 1] == (byte)'S' &&
                aiff[i + 2] == (byte)'N' && aiff[i + 3] == (byte)'D')
            {
                int ckSize = (aiff[i + 4] << 24) | (aiff[i + 5] << 16) | (aiff[i + 6] << 8) | aiff[i + 7];
                int dataLength = ckSize - 8; // minus the offset(4) + blockSize(4) fields
                return aiff.AsSpan(i + 16, dataLength).ToArray();
            }
        }
        throw new InvalidOperationException("SSND chunk not found");
    }

    private static byte[] WriteAndRead(WaveFormat format, Action<AiffFileWriter> writeAction)
    {
        var ms = new MemoryStream();

        using (var writer = new AiffFileWriter(new IgnoreDisposeStream(ms), format))
        {
            writeAction(writer);
        }

        ms.Position = 0;
        using var reader = new AiffFileReader(ms);
        var buffer = new byte[(int)reader.Length];
        var read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length));
        return buffer;
    }
}
