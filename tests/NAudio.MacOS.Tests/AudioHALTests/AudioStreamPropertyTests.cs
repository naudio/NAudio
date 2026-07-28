
using System;
using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

namespace NAudio.MacOS.Tests.AudioHALTests;

[TestFixture]
public class AudioStreamPropertyTests
{
    private AudioStream testStream;

    [SetUp]
    public void FindStream()
    {
        // Query the stream for each test, the HAL may invalidate the device and/or the streams.
        var streams = AudioSystemObject.Instance.DefaultOutputDevice.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            Assert.Fail("No streams were found in the default output device. This is invalid.");
        }
        else
        {
            testStream = streams[(int)(Random.Shared.NextSingle() * streams.Length)];
        }
    }

    [Test]
    public void StreamIsOutput()
    {
        Assert.That(
            testStream.Direction == AudioStreamDirection.Output,
            "The stream should be an output stream"
        );
    }

    [Test]
    public void CanQueryVirtualFormat()
    {
        Assert.DoesNotThrow(() => _ = testStream.VirtualFormat);
    }

    [Test]
    public void CanSetVirtualFormat()
    {
        Assert.DoesNotThrow(() =>
            {
                var old = testStream.VirtualFormat;
                testStream.VirtualFormat = new(48000, old.BitsPerSample, old.Channels);
                testStream.VirtualFormat = old;
            }
        );
    }

    [Test]
    public void CanQueryVirtualFormats()
    {
        Assert.DoesNotThrow(() => _ = testStream.VirtualFormats);
    }

    [Test]
    public void CanQueryPhysicalFormat()
    {
        Assert.DoesNotThrow(() => _ = testStream.PhysicalFormat);
    }

    [Test]
    public void CanQueryPhysicalFormats()
    {
        Assert.DoesNotThrow(() => _ = testStream.PhysicalFormats);
    }
}