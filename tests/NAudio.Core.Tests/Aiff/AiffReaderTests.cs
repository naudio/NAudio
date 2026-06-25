using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
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
}
