using System;
using System.Collections.Generic;
using System.IO;
using NAudio.SoundFile;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.SoundFile.Tests;

[TestFixture]
public class SoundFileRoundTripTests : SoundFileTestBase
{
    private string tempDir;

    [SetUp]
    public void CreateTempDir()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "naudio-soundfile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
    }

    [TearDown]
    public void DeleteTempDir()
    {
        if (tempDir != null && Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static (long frames, float[] data, WaveFormat format) ReadAll(SoundFileReader reader)
    {
        ISampleProvider sp = reader;
        var buffer = new float[reader.WaveFormat.Channels * 4096];
        var collected = new List<float>();
        int n;
        while ((n = sp.Read(buffer)) > 0)
        {
            collected.AddRange(new ReadOnlySpan<float>(buffer, 0, n).ToArray());
        }
        var data = collected.ToArray();
        return (data.Length / reader.WaveFormat.Channels, data, reader.WaveFormat);
    }

    [Test]
    public void WavRoundTripIsLossless()
    {
        var path = Path.Combine(tempDir, "tone.wav");
        var tone = new TonePcm16(48000, 0.5);
        long expectedFrames = 0;

        using (var writer = new SoundFileWriter(path, tone.WaveFormat, SoundFileMajorFormat.Wav))
        {
            var buf = new byte[4096];
            int read;
            while ((read = tone.Read(buf.AsSpan())) > 0)
            {
                writer.Write(buf.AsSpan(0, read));
                expectedFrames += read / tone.WaveFormat.BlockAlign;
            }
        }

        Assert.That(new FileInfo(path).Length, Is.GreaterThan(1000));

        using var reader = new SoundFileReader(path);
        var (frames, data, format) = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(format.SampleRate, Is.EqualTo(48000));
            Assert.That(format.Channels, Is.EqualTo(2));
            Assert.That(format.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
            Assert.That(frames, Is.EqualTo(expectedFrames));
            Assert.That(Rms(data), Is.GreaterThan(0.05));
        });
    }

    [Test]
    public void FlacRoundTripIsLosslessWhenSupported()
    {
        RequireFormat(SoundFileMajorFormat.Flac);
        var path = Path.Combine(tempDir, "tone.flac");
        var tone = new TonePcm16(48000, 0.5);

        SoundFileWriter.CreateSoundFile(path, tone, SoundFileMajorFormat.Flac,
            new SoundFileWriterOptions { CompressionLevel = 0.5 });

        using var reader = new SoundFileReader(path);
        var (frames, data, format) = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(format.SampleRate, Is.EqualTo(48000));
            // ~0.5 s of 48 kHz audio, FLAC is lossless so frame-exact.
            Assert.That(frames, Is.EqualTo(24000).Within(2));
            Assert.That(Rms(data), Is.GreaterThan(0.05));
        });
    }

    [Test]
    public void OggVorbisRoundTripWhenSupported()
    {
        RequireFormat(SoundFileMajorFormat.OggVorbis);
        var path = Path.Combine(tempDir, "tone.ogg");
        var tone = new TonePcm16(48000, 0.5);

        SoundFileWriter.CreateSoundFile(path, tone, SoundFileMajorFormat.OggVorbis,
            new SoundFileWriterOptions { VbrQuality = 0.6 });

        using var reader = new SoundFileReader(path);
        var (frames, data, _) = ReadAll(reader);

        Assert.Multiple(() =>
        {
            // Lossy: encoder/decoder delay shifts the count slightly.
            Assert.That(frames, Is.EqualTo(24000).Within(4096));
            Assert.That(Rms(data), Is.GreaterThan(0.02));
        });
    }

    [Test]
    public void OpusRoundTripWhenSupported()
    {
        RequireFormat(SoundFileMajorFormat.Opus);
        var path = Path.Combine(tempDir, "tone.opus");
        // Opus only supports 8/12/16/24/48 kHz.
        var tone = new TonePcm16(48000, 0.5);

        SoundFileWriter.CreateSoundFile(path, tone, SoundFileMajorFormat.Opus,
            new SoundFileWriterOptions { VbrQuality = 0.7 });

        using var reader = new SoundFileReader(path);
        var (frames, data, format) = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(format.SampleRate, Is.EqualTo(48000));
            // Opus has ~6.5 ms lookahead + frame padding; allow slack.
            Assert.That(frames, Is.EqualTo(24000).Within(8192));
            Assert.That(Rms(data), Is.GreaterThan(0.02));
        });
    }

    [Test]
    public void Mp3RoundTripWhenSupported()
    {
        RequireFormat(SoundFileMajorFormat.Mp3);
        var path = Path.Combine(tempDir, "tone.mp3");
        var tone = new TonePcm16(44100, 0.5);

        SoundFileWriter.CreateSoundFile(path, tone, SoundFileMajorFormat.Mp3,
            new SoundFileWriterOptions { VbrQuality = 0.5 });

        using var reader = new SoundFileReader(path);
        var (frames, data, format) = ReadAll(reader);

        Assert.Multiple(() =>
        {
            Assert.That(format.SampleRate, Is.EqualTo(44100));
            // MP3 (LAME) encoder/decoder delay + padding ~ a few thousand.
            Assert.That(frames, Is.EqualTo(22050).Within(8192));
            Assert.That(Rms(data), Is.GreaterThan(0.02));
        });
    }

    [Test]
    public void ExtensionIsInferredFromPath()
    {
        RequireFormat(SoundFileMajorFormat.Flac);
        var path = Path.Combine(tempDir, "inferred.flac");
        var tone = new TonePcm16(44100, 0.25);

        // No explicit major format — inferred from ".flac".
        SoundFileWriter.CreateSoundFile(path, tone);

        using var reader = new SoundFileReader(path);
        var (frames, _, format) = ReadAll(reader);
        Assert.Multiple(() =>
        {
            Assert.That(format.SampleRate, Is.EqualTo(44100));
            Assert.That(frames, Is.GreaterThan(10000));
        });
    }

    [Test]
    public void UnknownExtensionThrows()
    {
        var tone = new TonePcm16();
        Assert.Throws<ArgumentException>(() =>
            _ = new SoundFileWriter(Path.Combine(tempDir, "x.zzz"), tone.WaveFormat));
    }

    [Test]
    public void UnsupportedInputFormatThrows()
    {
        var path = Path.Combine(tempDir, "x.wav");
        Assert.Throws<NotSupportedException>(() =>
            _ = new SoundFileWriter(path, new WaveFormat(48000, 24, 2), SoundFileMajorFormat.Wav));
    }
}
