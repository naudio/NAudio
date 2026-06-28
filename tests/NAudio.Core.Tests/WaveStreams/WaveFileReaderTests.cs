using System.IO;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;
using System.Diagnostics;
using System;
using NAudio.Core.Tests.Utils;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
public class WaveFileReaderTests
{
    [Test]
    [Category("UnitTest")]
    public void TestEmptyFile()
    {
        // arrange
        var fileContents = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, // "RIFF"
            0x26, 0x00, 0x00, 0x00, // ChunkSize = 38
            0x57, 0x41, 0x56, 0x45, // "WAVE"
            0x66, 0x6d, 0x74, 0x20, // "fmt "
            0x12, 0x00, 0x00, 0x00, // Subchunk1Size = 18
            0x07, 0x00, 0x02, 0x00, // AudioFormat = 7, NumChannels = 2
            0x40, 0x1f, 0x00, 0x00, // SampleRate = 8000
            0x80, 0x3e, 0x00, 0x00, // ByteRate = 16000
            0x02, 0x00, 0x08, 0x00, // BlockAlign = 2, BitsPerSample = 8
            0x00, 0x00,             // ExtraParamSize = 0
            0x64, 0x61, 0x74, 0x61, // "data"
            0x00, 0x00, 0x00, 0x00, // Subchunk2Size = 0
        };
        using var inputStream = new MemoryStream(fileContents);
        using var reader = new WaveFileReader(inputStream);
        Assert.That(reader.WaveFormat.AverageBytesPerSecond, Is.EqualTo(16000));
        Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(8));
        Assert.That(reader.WaveFormat.Channels, Is.EqualTo(2));
        Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(8000));

        // empty-but-valid WAV: no audio samples and no extra chunks
        Assert.That(reader.Length, Is.EqualTo(0));
        Assert.That(reader.Chunks.Count, Is.EqualTo(0));
    }

    [Test]
    [Category("UnitTest")]
    public void CanAccessSamplesIndividuallyInAMonoFile()
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(8000, 16, 1)))
        {
            writer.WriteSample(0.1f);
            writer.WriteSample(0.2f);
            writer.WriteSample(0.3f);
            writer.WriteSample(0.4f);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(reader.ReadNextSampleFrame()[0], Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(reader.ReadNextSampleFrame(), Is.Null);
    }

    [Test]
    [Category("UnitTest")]
    public void CanAccessSamplesIndividuallyInAStereoFile()
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(8000, 16, 2)))
        {
            writer.WriteSample(0.1f);
            writer.WriteSample(0.2f);
            writer.WriteSample(0.3f);
            writer.WriteSample(0.4f);

        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var f1 = reader.ReadNextSampleFrame();
        Assert.That(f1[0], Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(f1[1], Is.EqualTo(0.2f).Within(0.0001f));
        var f2 = reader.ReadNextSampleFrame();
        Assert.That(f2[0], Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(f2[1], Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(reader.ReadNextSampleFrame(), Is.Null);
    }

    [Test]
    [Category("UnitTest")]
    public void CanAccessSamplesIndividuallyInAStereo24BitFile()
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(44100, 24, 2)))
        {
            writer.WriteSample(0.1f);
            writer.WriteSample(0.2f);
            writer.WriteSample(0.3f);
            writer.WriteSample(0.4f);

        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        var f1 = reader.ReadNextSampleFrame();
        Assert.That(f1[0], Is.EqualTo(0.1f).Within(0.0001f));
        Assert.That(f1[1], Is.EqualTo(0.2f).Within(0.0001f));
        var f2 = reader.ReadNextSampleFrame();
        Assert.That(f2[0], Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(f2[1], Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(reader.ReadNextSampleFrame(), Is.Null);
    }

    [Test]
    [Category("IntegrationTest")]
    public void CanLoadAndReadVariousProblemWavFiles()
    {
        string testDataFolder = @"C:\Users\Mark\Downloads\NAudio";
        if (!Directory.Exists(testDataFolder))
        {
            Assert.Ignore($"{testDataFolder} not found");
        }
        foreach (string file in Directory.GetFiles(testDataFolder, "*.wav"))
        {
            string wavFile = Path.Combine(testDataFolder, file);
            Debug.WriteLine(String.Format("Opening {0}", wavFile));
            using var reader = new WaveFileReader(wavFile);
            byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            int total = 0;
            do
            {
                bytesRead = reader.Read(buffer, 0, buffer.Length);
                total += bytesRead;
            } while (bytesRead > 0);
            Debug.WriteLine(String.Format("Read {0} bytes", total));
        }
    }

    [Test]
    [Category("UnitTest")]
    public void DisposeOfStreamWhenConstructedFromFilePath()
    {
        string tempFilePath = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllText(tempFilePath, "Some test content");
        try
        {
            WaveFileReader waveReader = new WaveFileReader(tempFilePath);

            Assert.Fail("Expected exception System.FormatException was not thrown for file missing a header.");
        }
        catch (FormatException ex)
        {
            Assert.That(ex, Is.Not.Null);
        }
        finally
        {
            System.IO.File.Delete(tempFilePath);
        }
    }

    [Test]
    [Category("UnitTest")]
    public void ChunksPropertyIsAlwaysNonNullForValidWav()
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(8000, 16, 1)))
        {
            writer.WriteSample(0.5f);
        }
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.Chunks, Is.Not.Null);
        Assert.That(reader.Chunks.Count, Is.EqualTo(0));
    }

    [Test]
    [Category("UnitTest")]
    public void ReaderExposesUnknownChunksViaChunksProperty()
    {
        var bytes = WaveFileBuilder.Build(
            new WaveFormat(8000, 16, 1),
            new byte[] { 1, 2, 3, 4 },
            new WaveFileBuilder.Chunk("junk", new byte[] { 9, 8, 7 }),
            new WaveFileBuilder.Chunk("meta", new byte[] { 0x42 }));
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        Assert.That(reader.Chunks.Count, Is.EqualTo(2));
        Assert.That(reader.Chunks.Contains("junk"), Is.True);
        Assert.That(reader.Chunks.Contains("meta"), Is.True);
    }

    [Test]
    [Category("UnitTest")]
    public void ChunksBeforeDataAreDiscoveredWithoutLosingDataPosition()
    {
        // fmt -> unknown chunk -> data order is valid per RIFF; ensure the reader survives it
        var audio = new byte[] { 1, 0, 2, 0, 3, 0, 4, 0 };
        var bytes = WaveFileBuilder.Build(
            new WaveFormat(8000, 16, 1),
            audio,
            new WaveFileBuilder.Chunk("pre ", new byte[] { 1, 2, 3, 4 }, beforeData: true));
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        Assert.That(reader.Chunks.Contains("pre "), Is.True);
        Assert.That(reader.Length, Is.EqualTo(audio.Length));

        // verify audio still reads back intact
        var buffer = new byte[audio.Length];
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(audio.Length));
        Assert.That(buffer, Is.EqualTo(audio));
    }

    [Test]
    [Category("UnitTest")]
    public void ReadingChunkDataDoesNotDisturbAudioPlayhead()
    {
        var audio = new byte[64];
        for (int i = 0; i < audio.Length; i++) audio[i] = (byte)i;
        var bytes = WaveFileBuilder.Build(
            new WaveFormat(8000, 16, 1),
            audio,
            new WaveFileBuilder.Chunk("test", new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
        using var reader = new WaveFileReader(new MemoryStream(bytes));

        // read half the audio, pull chunk data, then read the rest — it must be contiguous
        var first = new byte[audio.Length / 2];
        Assert.That(reader.Read(first, 0, first.Length), Is.EqualTo(first.Length));
        var chunkData = reader.Chunks.GetData(reader.Chunks[0]);
        var second = new byte[audio.Length - first.Length];
        Assert.That(reader.Read(second, 0, second.Length), Is.EqualTo(second.Length));

        Assert.That(chunkData, Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
        var combined = new byte[audio.Length];
        Buffer.BlockCopy(first, 0, combined, 0, first.Length);
        Buffer.BlockCopy(second, 0, combined, first.Length, second.Length);
        Assert.That(combined, Is.EqualTo(audio));
    }

    [Test]
    [Category("UnitTest")]
    public void ChunkDataIsLazyAndCanBeReadMultipleTimes()
    {
        var bytes = WaveFileBuilder.Build(
            new WaveFormat(8000, 16, 1),
            new byte[] { 1, 0 },
            new WaveFileBuilder.Chunk("test", new byte[] { 7, 7, 7 }));
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        var a = reader.Chunks.GetData(reader.Chunks[0]);
        var b = reader.Chunks.GetData(reader.Chunks[0]);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.SameAs(b)); // fresh array each call
    }

    // Regression: fuzz-found WAV where the fmt chunk declares nBlockAlign=0.
    // The Position setter used to throw DivideByZeroException during
    // construction. See issue #1254.
    [Test]
    [Category("UnitTest")]
    public void MalformedWavWithZeroBlockAlignThrowsInvalidData()
    {
        // 67-byte WAV: fmt { format=0x3301, channels=0x0001, BlockAlign=0 }
        byte[] payload = new byte[]
        {
            0x52,0x49,0x46,0x46, 0x28,0x00,0x00,0x00, 0x57,0x41,0x56,0x45, 0x66,0x6d,0x74,0x20,
            0x10,0x00,0x00,0x00, 0x01,0x33,0x01,0x00, 0x40,0x1f,0x00,0x00, 0x40,0x1f,0x00,0x40,
            0x00,0x00,0x08,0x00, 0x64,0x61,0x74,0x61, 0xe4,0x00,0x00,0x00, 0x00,0x57,0x41,0x56,
            0x45,0x66,0x6d,0x74, 0x20,0x10,0x00,0x00, 0x00,0x01,0x33,0x01, 0x00,0x40,0x1f,0x00,
            0x00,0x00,0x01
        };
        Assert.That(payload.Length, Is.EqualTo(67));

        Assert.Throws<InvalidDataException>(
            () => { using var _ = new WaveFileReader(new MemoryStream(payload)); });
    }

    // Regression: a fmt chunk can declare more extra (cbSize) bytes than NAudio's fixed
    // 100-byte buffer. The reader used to throw ArgumentException; it must now discard the
    // surplus and carry on reading the rest of the file. See issue #482.
    [Test]
    [Category("UnitTest")]
    public void OversizedFmtExtraDataIsDiscardedNotThrown()
    {
        const int extraSize = 200; // larger than WaveFormatExtraData's 100-byte buffer
        var audio = new byte[] { 1, 0, 2, 0, 3, 0, 4, 0 };

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(0); // RIFF size placeholder, patched below
            w.Write(Encoding.ASCII.GetBytes("WAVE"));

            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(18 + extraSize);      // fmt chunk length
            w.Write((short)1);            // PCM
            w.Write((short)2);            // channels
            w.Write(16000);               // sample rate
            w.Write(64000);               // average bytes per second
            w.Write((short)4);            // block align
            w.Write((short)16);           // bits per sample
            w.Write((short)extraSize);    // cbSize
            w.Write(new byte[extraSize]); // oversized extra data

            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(audio.Length);
            w.Write(audio);

            long fileLength = ms.Length;
            ms.Position = 4;
            w.Write((uint)(fileLength - 8));
        }
        ms.Position = 0;

        using var reader = new WaveFileReader(ms);

        Assert.That(reader.WaveFormat.Channels, Is.EqualTo(2));
        Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(16000));
        Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(16));
        Assert.That(reader.WaveFormat.AverageBytesPerSecond, Is.EqualTo(64000));
        Assert.That(reader.WaveFormat.ExtraSize, Is.EqualTo(0)); // surplus discarded
        Assert.That(reader.Length, Is.EqualTo(audio.Length));
    }

    [Test]
    [Category("UnitTest")]
    public void UnknownChunkOfTypeFactDoesNotBreakReader()
    {
        // fact chunk is commonly present in non-PCM WAV files; we don't have a special interpreter
        // for it but the reader should surface it as a raw chunk.
        var bytes = WaveFileBuilder.Build(
            new WaveFormat(8000, 16, 1),
            new byte[] { 1, 0, 2, 0 },
            new WaveFileBuilder.Chunk("fact", BitConverter.GetBytes(2)));
        using var reader = new WaveFileReader(new MemoryStream(bytes));
        var fact = reader.Chunks.Find("fact");
        Assert.That(fact, Is.Not.Null);
        var data = reader.Chunks.GetData(fact);
        Assert.That(BitConverter.ToInt32(data, 0), Is.EqualTo(2));
    }


    // Regression: some encoders (e.g. FFmpeg writing WAV to a non-seekable pipe) leave a
    // placeholder data chunk size that is far larger than the bytes actually present
    // (such as 0xFFFF1000). Reading from a FileStream tolerated this (seeking past EOF is
    // allowed) but a MemoryStream threw because the position exceeded the stream length.
    // The reader must clamp to the available bytes and expose the real length. See issue #1090.
    [Test]
    [Category("UnitTest")]
    public void OversizedDataChunkLengthIsClampedToAvailableBytes()
    {
        var audio = new byte[] { 1, 0, 2, 0, 3, 0, 4, 0 }; // 4 mono 16-bit samples

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(0); // RIFF size placeholder, patched below
            w.Write(Encoding.ASCII.GetBytes("WAVE"));

            w.Write(Encoding.ASCII.GetBytes("fmt "));
            new WaveFormat(8000, 16, 1).Serialize(w);

            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(0xFFFF1000u); // bogus placeholder size, much larger than the actual data
            w.Write(audio);

            long fileLength = ms.Length;
            ms.Position = 4;
            w.Write((uint)(fileLength - 8));
        }
        ms.Position = 0;

        using var reader = new WaveFileReader(ms);

        // length is clamped to what's actually present, not the bogus declared size
        Assert.That(reader.Length, Is.EqualTo(audio.Length));

        var buffer = new byte[audio.Length];
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(audio.Length));
        Assert.That(buffer, Is.EqualTo(audio));
        Assert.That(reader.Read(buffer, 0, buffer.Length), Is.EqualTo(0));
    }
}
