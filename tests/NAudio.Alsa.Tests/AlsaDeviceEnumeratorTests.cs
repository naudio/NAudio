using System.Linq;
using NAudio.Wave.Alsa;
using NUnit.Framework;

namespace NAudio.Alsa.Tests;

[TestFixture]
public class AlsaDeviceEnumeratorTests : AlsaTestBase
{
    [Test]
    public void PlaybackListIsNonEmptyWithUsableNames()
    {
        var devices = AlsaDeviceEnumerator.GetPlaybackDevices();
        Assert.That(devices, Is.Not.Empty);
        Assert.That(devices.All(d => !string.IsNullOrEmpty(d.Name)), Is.True);
    }

    [Test]
    public void BidirectionalNullDeviceAppearsInBothDirections()
    {
        // The "null" PCM has no IOID hint, so it is both an output and an input.
        Assert.That(AlsaDeviceEnumerator.GetPlaybackDevices().Any(d => d.Name == "null"), Is.True);
        Assert.That(AlsaDeviceEnumerator.GetCaptureDevices().Any(d => d.Name == "null"), Is.True);
    }

    [Test]
    public void EnumeratedNameCanBeOpened()
    {
        var device = AlsaDeviceEnumerator.GetPlaybackDevices().First(d => d.Name == "null");
        Assert.DoesNotThrow(() =>
        {
            using var outp = new AlsaOut(device.Name);
        });
    }
}
