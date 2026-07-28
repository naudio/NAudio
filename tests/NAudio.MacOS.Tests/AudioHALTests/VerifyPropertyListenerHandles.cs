
using System;

using NAudio.MacOS.CoreAudio;

using NUnit.Framework;

[TestFixture]
public class VerifyPropertyListenerHandles
{
    [Test]
    public void CanCreateDeviceControlListChanged()
    {
        var handle = AudioSystemObject.Instance.DefaultOutputDevice.ConstructControlListChangedEvent();
        handle.Dispose();
    }

    [Test]
    public void CanCreateDeviceIOStoppedAbnormally()
    {
        var handle = AudioSystemObject.Instance.DefaultOutputDevice.ConstructIOStoppedAbnormallyEvent();
        handle.Dispose();
    }

    [Test]
    public void CanCreateDeviceIsAliveChanged()
    {
        var handle = AudioSystemObject.Instance.DefaultOutputDevice.ConstructIsAliveChangedEvent();
        handle.Dispose();
    }

    [Test]
    public void CanCreateDeviceProcessorOverloaded()
    {
        var handle = AudioSystemObject.Instance.DefaultOutputDevice.ConstructProcessorOverloadedEvent();
        handle.Dispose();
    }

    [Test]
    public void CanCreateDeviceStreamsChanged()
    {
        var handle = AudioSystemObject.Instance.DefaultOutputDevice.ConstructStreamsChangedEvent(AudioObjectPropertyScopeConstants.Output);
        handle.Dispose();
    }

    [Test]
    public void CanCreateStreamVirtualFormatChanged()
    {
        var streams = AudioSystemObject.Instance.DefaultOutputDevice.GetStreams(AudioObjectPropertyScopeConstants.Output);
        if (streams.Length == 0)
        {
            Assert.Fail("No streams were found in the default output device. This is invalid.");
        }
        else
        {
            var testStream = streams[(int)(Random.Shared.NextSingle() * streams.Length)];
            var handle = testStream.ConstructVirtualFormatChangedEvent();
            handle.Dispose();
        }
    }
}