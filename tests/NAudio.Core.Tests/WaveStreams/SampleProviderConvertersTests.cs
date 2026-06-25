using System;
using System.IO;
using System.Text;
using NAudio.Dmo;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

// Regression tests for issue #639: WaveFileReader.ToSampleProvider() threw
// "Unsupported source encoding" for WAVE_FORMAT_EXTENSIBLE files, which is how
// multichannel / >16 bit / >4GB PCM and float audio is usually described.
[TestFixture]
[Category("UnitTest")]
public class SampleProviderConvertersTests
{
    [Test]
    public void Extensible16BitStereoPcmIsConverted()
    {
        // two stereo frames: L = +0.5, R = -0.5
        short[] samples = { 16384, -16384, 16384, -16384 };
        var data = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, data, 0, data.Length);

        var buffer = ReadAllSamples(44100, 16, 2, AudioMediaSubtypes.MEDIASUBTYPE_PCM, data, samples.Length);

        Assert.That(buffer[0], Is.EqualTo(0.5f).Within(0.0001));
        Assert.That(buffer[1], Is.EqualTo(-0.5f).Within(0.0001));
        Assert.That(buffer[2], Is.EqualTo(0.5f).Within(0.0001));
        Assert.That(buffer[3], Is.EqualTo(-0.5f).Within(0.0001));
    }

    [Test]
    public void Extensible24BitMultiChannelPcmIsConverted()
    {
        // one 6-channel frame, every sample = +0.5 (0x400000 little-endian = 00 00 40)
        const int channels = 6;
        var data = new byte[channels * 3];
        for (int ch = 0; ch < channels; ch++)
        {
            data[ch * 3 + 0] = 0x00;
            data[ch * 3 + 1] = 0x00;
            data[ch * 3 + 2] = 0x40;
        }

        var buffer = ReadAllSamples(48000, 24, channels, AudioMediaSubtypes.MEDIASUBTYPE_PCM, data, channels);

        for (int ch = 0; ch < channels; ch++)
        {
            Assert.That(buffer[ch], Is.EqualTo(0.5f).Within(0.0001), "channel " + ch);
        }
    }

    [Test]
    public void Extensible32BitFloatIsConverted()
    {
        float[] samples = { 0.25f, -0.75f, 1.0f, -1.0f };
        var data = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, data, 0, data.Length);

        var buffer = ReadAllSamples(44100, 32, 2, AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT, data, samples.Length);

        for (int n = 0; n < samples.Length; n++)
        {
            Assert.That(buffer[n], Is.EqualTo(samples[n]).Within(0.0001), "sample " + n);
        }
    }

    [Test]
    public void ExtensibleWithUnsupportedSubFormatStillThrows()
    {
        // AC-3 spdif is a valid extensible subformat that is not raw PCM or IEEE float
        var data = new byte[8];
        using var wav = BuildExtensibleWav(48000, 16, 2, AudioMediaSubtypes.MEDIASUBTYPE_DOLBY_AC3_SPDIF, data);
        using var reader = new WaveFileReader(wav);

        Assert.Throws<ArgumentException>(() => reader.ToSampleProvider());
    }

    private static float[] ReadAllSamples(int rate, int bits, int channels, Guid subFormat, byte[] data, int expectedSamples)
    {
        using var wav = BuildExtensibleWav(rate, bits, channels, subFormat, data);
        using var reader = new WaveFileReader(wav);

        var sampleProvider = reader.ToSampleProvider();
        Assert.That(sampleProvider.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
        Assert.That(sampleProvider.WaveFormat.SampleRate, Is.EqualTo(rate));
        Assert.That(sampleProvider.WaveFormat.Channels, Is.EqualTo(channels));

        var buffer = new float[expectedSamples];
        int read = sampleProvider.Read(buffer.AsSpan());
        Assert.That(read, Is.EqualTo(expectedSamples), "samples read");
        return buffer;
    }

    // Builds an in-memory WAV file with a WAVE_FORMAT_EXTENSIBLE fmt chunk and the given
    // SubFormat GUID, mirroring what a real multichannel / high bit depth WAV looks like.
    private static MemoryStream BuildExtensibleWav(int rate, int bits, int channels, Guid subFormat, byte[] data)
    {
        var ms = new MemoryStream();
        var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
        short blockAlign = (short)(channels * bits / 8);

        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(0); // RIFF size placeholder, patched below
        w.Write(Encoding.ASCII.GetBytes("WAVE"));

        w.Write(Encoding.ASCII.GetBytes("fmt "));
        w.Write(40);                       // fmt chunk length (16 + 2 cbSize + 22 ext)
        w.Write((ushort)0xFFFE);           // WAVE_FORMAT_EXTENSIBLE
        w.Write((short)channels);
        w.Write(rate);
        w.Write(rate * blockAlign);        // average bytes per second
        w.Write(blockAlign);
        w.Write((short)bits);
        w.Write((short)22);                // cbSize
        w.Write((short)bits);              // wValidBitsPerSample
        w.Write((1 << channels) - 1);      // dwChannelMask
        w.Write(subFormat.ToByteArray());  // SubFormat GUID

        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(data.Length);
        w.Write(data);
        w.Flush();

        long fileLength = ms.Length;
        ms.Position = 4;
        w.Write((uint)(fileLength - 8));
        w.Flush();

        ms.Position = 0;
        return ms;
    }
}
