using System;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NAudio.Windows.Tests.DirectSound;

[TestFixture]
public class DirectSoundTests
{
    [Test]
    [Category("IntegrationTest")]
    public void CanEnumerateDevices()
    {
        foreach (var device in DirectSoundOut.Devices)
        {
            Debug.WriteLine(String.Format("{0} {1} {2}", device.Description, device.ModuleName, device.Guid));
        }
    }

    [Test]
    [Category("IntegrationTest")]
    public void CanPlaySilenceThroughDefaultDevice()
    {
        // Regression test: the secondary buffer's WAVEFORMATEX used to come from a GCHandle
        // pinned on the managed WaveFormat instance. WaveFormat has carried no [StructLayout]
        // since #1432, so Auto layout could reorder its fields and every playback failed in
        // InitializeDirectSound with DirectSoundException 0x80004001. With the fix the buffer
        // is created from WaveFormat.MarshalToPtr and audio is actually served.
        var format = new WaveFormat(44100, 16, 2);
        using var silence = new RawSourceWaveStream(
            new MemoryStream(new byte[format.AverageBytesPerSecond]), format);
        PlayAndAssertAudioServed(silence);
    }

    [Test]
    [Category("IntegrationTest")]
    public void CanPlayWavFileWhoseFormatCarriesExtraData()
    {
        // Regression test (second half of the pinned-format bug): a WaveFileReader's format is
        // a WaveFormatExtraData, whose byte[] field made the old GCHandle.Alloc(..., Pinned)
        // throw ArgumentException("Object contains references") before the buffer was created.
        var path = Path.Combine(Path.GetTempPath(), "naudio-directsound-test.wav");
        try
        {
            var format = new WaveFormat(44100, 16, 2);
            using (var writer = new WaveFileWriter(path, format))
            {
                writer.Write(new byte[format.AverageBytesPerSecond], 0, format.AverageBytesPerSecond);
            }
            using var reader = new WaveFileReader(path);
            Assert.That(reader.WaveFormat, Is.InstanceOf<WaveFormatExtraData>(),
                "this test needs a source whose format carries the extraData field");
            PlayAndAssertAudioServed(reader);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void PlayAndAssertAudioServed(IWaveProvider source)
    {
        Exception stopException = null;
        using var output = new DirectSoundOut(100);
        output.PlaybackStopped += (_, args) => stopException = args.Exception;
        output.Init(source);
        output.Play();
        Assert.That(SpinWait.SpinUntil(() => output.GetPosition() > 0, 1000),
            "no audio was ever served (GetPosition stayed at zero)");
        Assert.That(stopException, Is.Null, $"playback stopped with an exception: {stopException}");
    }
}
