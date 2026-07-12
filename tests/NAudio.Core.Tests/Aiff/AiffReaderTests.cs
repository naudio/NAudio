using System;
using NUnit.Framework;
using System.IO;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Core.Tests.Aiff;

[TestFixture]
public class AiffReaderTests
{
    [Test]
    [Category("IntegrationTest")]
    public void ConvertAiffToWav()
    {
        string testFolder = @"C:\Users\Mark\Downloads\NAudio";
        if (!Directory.Exists(testFolder))
        {
            Assert.Ignore($"{testFolder} not found");
        }

        foreach (string file in Directory.GetFiles(testFolder, "*.aiff"))
        {
            string baseName = Path.GetFileNameWithoutExtension(file);
            string wavFile = Path.Combine(testFolder, baseName + ".wav");
            string aiffFile = Path.Combine(testFolder, file);
            Debug.WriteLine(String.Format("Converting {0} to wav", aiffFile));
            ConvertAiffToWav(aiffFile, wavFile);
        }
    }

    private static void ConvertAiffToWav(string aiffFile, string wavFile)
    {
        using var reader = new AiffFileReader(aiffFile);
        using var writer = new WaveFileWriter(wavFile, reader.WaveFormat);
        byte[] buffer = new byte[4096];
        int bytesRead = 0;
        do
        {
            bytesRead = reader.Read(buffer, 0, buffer.Length);
            writer.Write(buffer, 0, bytesRead);
        } while (bytesRead > 0);
    }

    // Regression: fuzz-found AIFF with COMM declaring sampleSize=6 bits ->
    // BlockAlign computes to 0, which used to throw DivideByZeroException
    // from set_Position during construction. See issue #1254.
    [Test]
    [Category("UnitTest")]
    public void MalformedAiffWithZeroBlockAlignThrowsInvalidData()
    {
        // 54-byte AIFF: COMM { channels=1, sampleFrames=2, sampleSize=6, sampleRate=0 }
        // followed by an SSND chunk. (channels * (sampleSize/8)) == 0.
        byte[] payload = new byte[]
        {
            0x46,0x4f,0x52,0x4d, 0x00,0x04,0x00,0x26, 0x41,0x49,0x46,0x46, 0x43,0x4f,0x4d,0x4d,
            0x00,0x00,0x00,0x12, 0x00,0x01, 0x00,0x00,0x00,0x02, 0x00,0x06,
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
            0x53,0x53,0x4e,0x44, 0x00,0x00,0x00,0x08, 0x40,0x0b,0xfa,0x00, 0x00,0x00,0x00,0x00
        };
        Assert.That(payload.Length, Is.EqualTo(54));

        Assert.Throws<InvalidDataException>(
            () => { using var _ = new AiffFileReader(new MemoryStream(payload)); });
    }

    // Regression: fuzz-found AIFF where the SSND chunk supplies fewer bytes
    // than a single sample frame. The 32-bit byte-swap loop in Read used
    // to access read[i+1..i+3] past the truncated read. See issue #1254.
    [Test]
    [Category("UnitTest")]
    public void MalformedAiffWithTruncatedSsndDoesNotThrowOnRead()
    {
        // 64-byte AIFF: COMM declares 32-bit samples (BlockAlign = 4 for mono),
        // but the SSND chunk's effective audio payload is only 2 bytes long.
        byte[] payload = new byte[]
        {
            0x46,0x4f,0x52,0x4d, 0x00,0x00,0x00,0x26, 0x41,0x49,0x46,0x46, 0x43,0x4f,0x4d,0x4d,
            0x00,0x00,0x00,0x12, 0x00,0x01, 0x00,0x0b,0x00,0x00, 0x00,0x20, 0x00,0x00,0x00,0xe6,
            0x00,0x00,0x00,0x19, 0x00,0x00, 0x53,0x53,0x4e,0x44, 0x00,0x00,0x00,0x0a, 0x00,0x00,
            0x00,0x00, 0x00,0x24, 0x24,0x24,0x24,0x24, 0x45,0x66,0x63,0xf9, 0x00,0x00,0x00,0x53
        };
        Assert.That(payload.Length, Is.EqualTo(64));

        using var reader = new AiffFileReader(new MemoryStream(payload));
        var buffer = new byte[4096];
        Assert.DoesNotThrow(() =>
        {
            while (reader.Read(buffer, 0, buffer.Length) > 0) { }
        });
    }

    // Regression: AIFF stores 8-bit PCM as signed two's-complement (unlike WAV's unsigned
    // 8-bit), so the reader must convert it for the shared unsigned 8-bit sample converter.
    // The on-disk bytes and expected float values below are from the issue report. See #1178.
    [Test]
    [Category("UnitTest")]
    public void Read8BitPcmTreatsSamplesAsSigned()
    {
        byte[] signedSoundData =
        {
            0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A,
            0x80, 0x83, 0x85, 0x88, 0x8A, 0x8D, 0x8F, 0x92
        };
        float[] expected =
        {
            0.078125f, 0.078125f, 0.078125f, 0.078125f, 0.078125f, 0.078125f, 0.078125f, 0.078125f,
            -1f, -0.9765625f, -0.9609375f, -0.9375f, -0.921875f, -0.8984375f, -0.8828125f, -0.859375f
        };

        var aiff = BuildAiff(44100, channels: 1, bitsPerSample: 8, soundData: signedSoundData);

        using var reader = new AiffFileReader(new MemoryStream(aiff));
        var samples = reader.ToSampleProvider();
        var actual = new float[signedSoundData.Length];
        int read = samples.Read(actual);

        Assert.That(read, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(actual[i], Is.EqualTo(expected[i]).Within(1e-6f), $"sample {i}");
        }
    }

    // Builds a minimal uncompressed AIFF (FORM/COMM/SSND) with the supplied sound data
    // placed verbatim, so a test can control the exact on-disk bytes.
    private static byte[] BuildAiff(int sampleRate, short channels, short bitsPerSample, byte[] soundData)
    {
        int numSampleFrames = soundData.Length / (channels * (bitsPerSample / 8));
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        void Tag(string s) => bw.Write(Encoding.ASCII.GetBytes(s));
        void WriteBE16(short v) { bw.Write((byte)(v >> 8)); bw.Write((byte)v); }
        void WriteBE32(int v)
        {
            bw.Write((byte)(v >> 24)); bw.Write((byte)(v >> 16)); bw.Write((byte)(v >> 8)); bw.Write((byte)v);
        }

        const int commSize = 18;                 // channels(2) + frames(4) + sampleSize(2) + rate(10)
        int ssndSize = 8 + soundData.Length;     // offset(4) + blockSize(4) + data
        int formSize = 4 + (8 + commSize) + (8 + ssndSize);

        Tag("FORM"); WriteBE32(formSize); Tag("AIFF");
        Tag("COMM"); WriteBE32(commSize);
        WriteBE16(channels); WriteBE32(numSampleFrames); WriteBE16(bitsPerSample);
        bw.Write(IEEE.ConvertToIeeeExtended(sampleRate));   // 10-byte 80-bit extended
        Tag("SSND"); WriteBE32(ssndSize); WriteBE32(0); WriteBE32(0);
        bw.Write(soundData);
        bw.Flush();

        return ms.ToArray();
    }
}
