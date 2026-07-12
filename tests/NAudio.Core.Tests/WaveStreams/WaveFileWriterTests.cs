using System;
using NUnit.Framework;
using NAudio.Wave;
using System.IO;
using NAudio.Utils;
using NAudio.Dmo;
using NAudio.Tests.Shared;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
[Category("UnitTest")]
public class WaveFileWriterTests
{
    [Test]
    public void ReaderShouldReadBackSameDataWrittenWithWrite()
    {
        var ms = new MemoryStream();
        var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1)))
        {
            writer.Write(testSequence, 0, testSequence.Length);
        }
        // check the Reader can read it
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(16000), "Sample Rate");
        Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(24), "Bits Per Sample");
        Assert.That(reader.WaveFormat.Channels, Is.EqualTo(1), "Channels");
        Assert.That(reader.Length, Is.EqualTo(testSequence.Length), "File Length");
        var buffer = new byte[600]; // 24 bit audio, block align is 3
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(testSequence.Length), "Data Length");
        for (int n = 0; n < read; n++)
        {
            Assert.That(buffer[n], Is.EqualTo(testSequence[n]), $"Byte {n}");
        }
    }


    [Test]
    public void FlushUpdatesHeaderEvenIfDisposeNotCalled()
    {
        var ms = new MemoryStream();
        var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE };
        var testSequence2 = new byte[] { 0x3, 0x4, 0x5 };
        var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormat(16000, 24, 1));
        writer.Write(testSequence, 0, testSequence.Length);
        writer.Flush();
        // BUT NOT DISPOSED
        // another write that was not flushed
        writer.Write(testSequence2, 0, testSequence2.Length);

        // check the Reader can read it
        ms.Position = 0;
        using (var reader = new WaveFileReader(ms))
        {
            Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(16000), "Sample Rate");
            Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(24), "Bits Per Sample");
            Assert.That(reader.WaveFormat.Channels, Is.EqualTo(1), "Channels");
            Assert.That(reader.Length, Is.EqualTo(testSequence.Length), "File Length");
            var buffer = new byte[600]; // 24 bit audio, block align is 3
            int read = reader.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(testSequence.Length), "Data Length");

            for (int n = 0; n < read; n++)
            {
                Assert.That(buffer[n], Is.EqualTo(testSequence[n]), $"Byte {n}");
            }
        }
        writer.Dispose(); // to stop the finalizer from moaning
    }


    [Test]
    public void StreamConstructorLeavesCallerSuppliedStreamOpen()
    {
        // Regression test for https://github.com/naudio/NAudio/issues/1040
        // The stream constructor must not dispose a stream the caller handed in.
        // Disposing the writer still finalizes the header (flush), but leaves the
        // MemoryStream usable - no IgnoreDisposeStream wrapper required.
        var ms = new MemoryStream();
        var testSequence = new byte[] { 0x1, 0x2, 0xFF, 0xFE }; // 2 samples of 16-bit mono (block align 2)
        using (var writer = new WaveFileWriter(ms, new WaveFormat(16000, 16, 1)))
        {
            writer.Write(testSequence, 0, testSequence.Length);
        }

        // If the writer had disposed ms, accessing it would throw ObjectDisposedException.
        Assert.That(ms.CanRead, Is.True, "Caller's stream should still be open");
        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.Length, Is.EqualTo(testSequence.Length), "Header finalized / data flushed");
        var buffer = new byte[testSequence.Length];
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(testSequence.Length), "Data length");
        Assert.That(buffer, Is.EqualTo(testSequence));
    }

    [Test]
    public void FilenameConstructorClosesTheUnderlyingFile()
    {
        // The filename constructor owns the FileStream it opened, so disposing the writer
        // must release the file handle (otherwise the file cannot be reopened/deleted).
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
        try
        {
            using (var writer = new WaveFileWriter(tempFile, new WaveFormat(16000, 16, 1)))
            {
                writer.WriteSample(0.5f);
            }
            // Re-opening for read+write share would fail if the writer still held the handle.
            using var reopened = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.That(reopened.Length, Is.GreaterThan(0));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void CreateWaveFileCreatesFileOfCorrectLength()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
        try
        {
            long length = 4200;
            var waveFormat = new WaveFormat(8000, 8, 2);
            WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(waveFormat, length));
            using var reader = new WaveFileReader(tempFile);
            Assert.That(reader.WaveFormat, Is.EqualTo(waveFormat), "WaveFormat");
            Assert.That(reader.Length, Is.EqualTo(length), "Length");
            var buffer = new byte[length + 20];
            int read = reader.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(length), "Read");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void CanUseWriteSampleToA16BitFile()
    {
        float amplitude = 0.25f;
        float frequency = 1000;
        using var writer = new WaveFileWriter(new MemoryStream(), new WaveFormat(16000, 16, 1));
        for (int n = 0; n < 1000; n++)
        {
            var sample = (float)(amplitude * Math.Sin((2 * Math.PI * n * frequency) / writer.WaveFormat.SampleRate));
            writer.WriteSample(sample);
        }
    }

    [Test]
    public void WriteSampleTo32BitExtensibleFloatRoundTripsCorrectly()
    {
        // Regression test for https://github.com/naudio/NAudio/issues/651
        // A 32-bit WaveFormatExtensible defaults to an IEEE-float subformat, so WriteSample
        // must write the float verbatim. Previously it truncated the normalised float to an
        // Int32 before scaling, so almost every sample was written as zero.
        var samples = new[] { 0.0f, 0.5f, -0.5f, 0.25f, -0.25f, 1.0f, -1.0f };
        var format = new WaveFormatExtensible(44100, 32, 1);
        Assert.That(format.SubFormat, Is.EqualTo(AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT), "Sanity: default 32-bit extensible is IEEE float");

        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), format))
        {
            foreach (var sample in samples)
            {
                writer.WriteSample(sample);
            }
        }

        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(32), "Bits Per Sample");
        Assert.That(reader.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Extensible), "Encoding");
        Assert.That(reader.Length, Is.EqualTo(samples.Length * 4), "Data length");

        var buffer = new byte[samples.Length * 4];
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length), "Bytes read");

        for (int n = 0; n < samples.Length; n++)
        {
            float actual = BitConverter.ToSingle(buffer, n * 4);
            Assert.That(actual, Is.EqualTo(samples[n]), $"Sample {n} ({samples[n]})");
        }
    }

    [Test]
    public void WriteSampleResolvesSubFormatFromReadBackExtensibleFormat()
    {
        // When an extensible format is read back from a stream it materialises as a
        // WaveFormatExtraData (not a WaveFormatExtensible), so WriteSample must still be able to
        // resolve the IEEE-float subformat when that format is reused to write a new file.
        var samples = new[] { 0.0f, 0.5f, -0.5f, 1.0f, -1.0f };

        var firstPass = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(firstPass), new WaveFormatExtensible(44100, 32, 1)))
        {
            foreach (var sample in samples) writer.WriteSample(sample);
        }

        firstPass.Position = 0;
        using var reader = new WaveFileReader(firstPass);
        var readBackFormat = reader.WaveFormat;
        Assert.That(readBackFormat, Is.InstanceOf<WaveFormatExtraData>(), "Read-back format type");
        Assert.That(readBackFormat.AsStandardWaveFormat().Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat), "Resolved subformat");

        // Reuse the read-back (WaveFormatExtraData) format to write a fresh file.
        var secondPass = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(secondPass), readBackFormat))
        {
            foreach (var sample in samples) writer.WriteSample(sample);
        }

        secondPass.Position = 0;
        using var reader2 = new WaveFileReader(secondPass);
        var buffer = new byte[samples.Length * 4];
        int read = reader2.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length), "Bytes read");
        for (int n = 0; n < samples.Length; n++)
        {
            Assert.That(BitConverter.ToSingle(buffer, n * 4), Is.EqualTo(samples[n]), $"Sample {n} ({samples[n]})");
        }
    }

    [Test]
    public void WriteShortSamplesTo32BitExtensibleFloatRoundTripsCorrectly()
    {
        // The WriteSamples(short[]) path shared the WriteSample bug: it wrote integer PCM into a
        // 32-bit extensible format even when that format's subformat is IEEE float.
        var samples = new short[] { 0, 16384, -16384, short.MaxValue, short.MinValue };
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), new WaveFormatExtensible(44100, 32, 1)))
        {
            writer.WriteSamples(samples, 0, samples.Length);
        }

        ms.Position = 0;
        using var reader = new WaveFileReader(ms);
        Assert.That(reader.Length, Is.EqualTo(samples.Length * 4), "Data length");
        var buffer = new byte[samples.Length * 4];
        int read = reader.Read(buffer, 0, buffer.Length);
        Assert.That(read, Is.EqualTo(buffer.Length), "Bytes read");
        for (int n = 0; n < samples.Length; n++)
        {
            float expected = samples[n] / (float)(short.MaxValue + 1);
            Assert.That(BitConverter.ToSingle(buffer, n * 4), Is.EqualTo(expected), $"Sample {n} ({samples[n]})");
        }
    }

    [Test]
    [Explicit]
    public void CanCreateWaveFileGreaterThan2Gb()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var dataLength = Int32.MaxValue + 1001L;
            WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100, 2), dataLength));
            Assert.That(new FileInfo(tempFile).Length, Is.EqualTo(dataLength + 46));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    [Explicit]
    public void FailsToCreateWaveFileGreaterThan4Gb()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var dataLength = UInt32.MaxValue - 10; // will be too big as not enough room for RIFF header, fmt chunk etc
            var ae = Assert.Throws<ArgumentException>(
                () =>
                    WaveFileWriter.CreateWaveFile(tempFile, new NullWaveStream(new WaveFormat(44100, 2), dataLength)));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
