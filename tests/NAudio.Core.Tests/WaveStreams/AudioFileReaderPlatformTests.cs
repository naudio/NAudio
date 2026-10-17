using System;
using System.IO;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

/// <summary>
/// AudioFileReader's platform split is decided at runtime (OperatingSystem.IsWindows) rather than
/// by target framework, so the same assembly must read PCM WAV everywhere and refuse
/// Media Foundation / ACM formats with a helpful NotSupportedException off Windows. The Windows
/// decoding paths are covered by NAudio.Windows.Tests.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class AudioFileReaderPlatformTests
{
    [Test]
    public void ReadsPcmWavFromStreamOnAnyPlatform()
    {
        using var reader = new AudioFileReader(new MemoryStream(BuildPcmWav()));

        Assert.That(reader.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
        var buffer = new float[1024];
        Assert.That(reader.Read(buffer), Is.GreaterThan(0));
    }

    [Test]
    public void NonWavStreamThrowsNotSupportedOffWindows()
    {
        if (OperatingSystem.IsWindows())
            Assert.Ignore("Windows passes unrecognised streams to Media Foundation");

        // An MPEG-1 Layer III frame header: neither RIFF/WAVE nor AIFF, so it would go to
        // Media Foundation on Windows.
        var mp3ish = new byte[4096];
        mp3ish[0] = 0xFF; mp3ish[1] = 0xFB; mp3ish[2] = 0x90; mp3ish[3] = 0x64;

        var ex = Assert.Throws<NotSupportedException>(() => new AudioFileReader(new MemoryStream(mp3ish)));
        Assert.That(ex!.Message, Does.Contain("only supported by AudioFileReader on Windows"));
        Assert.That(ex.Message, Does.Contain("NAudio.SoundFile"));
    }

    [Test]
    public void Mp3FileThrowsNotSupportedOffWindows()
    {
        if (OperatingSystem.IsWindows())
            Assert.Ignore("Windows decodes MP3 files with Media Foundation");

        // The extension alone selects the Media Foundation path, before the file is opened.
        var ex = Assert.Throws<NotSupportedException>(() => new AudioFileReader("does-not-exist.mp3"));
        Assert.That(ex!.Message, Does.StartWith("MP3 is only supported by AudioFileReader on Windows"));
    }

    private static byte[] BuildPcmWav(int sampleCount = 4096, int sampleRate = 44100)
    {
        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(ms, new WaveFormat(sampleRate, 16, 1)))
        {
            for (int i = 0; i < sampleCount; i++)
                writer.WriteSample((float)Math.Sin(2 * Math.PI * 1000.0 * i / sampleRate) * 0.5f);
        }
        return ms.ToArray();
    }
}
