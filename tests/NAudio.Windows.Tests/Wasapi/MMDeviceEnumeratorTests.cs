using System;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudio.Windows.Tests.Utils;

namespace NAudio.Windows.Tests.Wasapi;

[TestFixture]
[Category("IntegrationTest")]
[Platform("Win")] // runtime safety net . NAudio.Windows.Tests only targets a -windows TFM, but [Platform] keeps it honest on any future test host
public class MMDeviceEnumeratorTests
{
    [Test]
    public void CanCreateMMDeviceEnumeratorInVista()
    {
        OSUtils.RequireVista();
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
    }

    [Test]
    public void CanEnumerateDevicesInVista()
    {
        OSUtils.RequireVista();
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All);

        foreach (MMDevice device in devices)
        {
            if (device.State != DeviceState.NotPresent)
            {
                Debug.WriteLine(String.Format("{0}, {1}", device.FriendlyName, device.State));
            }
            else
            {
                Debug.WriteLine(String.Format("{0}, {1}", device.ID, device.State));
            }
        }
    }

    [Test]
    public void CanEnumerateCaptureDevices()
    {
        OSUtils.RequireVista();
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All);

        foreach (MMDevice device in devices)
        {
            if (device.State != DeviceState.NotPresent)
            {
                Debug.WriteLine(String.Format("{0}, {1}", device.FriendlyName, device.State));
            }
            else
            {
                Debug.WriteLine(String.Format("{0}, {1}", device.ID, device.State));
            }
        }
    }

    [Test]
    public void CanGetDefaultAudioEndpoint()
    {
        OSUtils.RequireVista();
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        Assert.That(defaultAudioEndpoint, Is.Not.Null);
    }

    [Test]
    public void CanActivateDefaultAudioEndpoint()
    {
        OSUtils.RequireVista();
        MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        AudioClient audioClient = defaultAudioEndpoint.CreateAudioClient();
        Assert.That(audioClient, Is.Not.Null);
    }

    [Test]
    public void DisposingDeviceAfterReadingPropertiesIsIdempotent()
    {
        // Reading a property-backed value (DeviceFriendlyName) lazily opens the
        // device's property store. Dispose must release it, and be safe to call
        // more than once. Regression cover for #1145.
        OSUtils.RequireVista();
        var enumerator = new MMDeviceEnumerator();
        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
        {
            var name = device.DeviceFriendlyName;
            Assert.That(name, Is.Not.Null);
            device.Dispose();
            Assert.DoesNotThrow(() => device.Dispose());
        }
    }

    [Test]
    public void ThrowsNotSupportedExceptionInXP()
    {
        OSUtils.RequireXP();
        Assert.Throws<NotSupportedException>(() => new MMDeviceEnumerator());
    }

    [Test]
    public void CanGetAudioClockClient()
    {
        OSUtils.RequireVista();
        var enumerator = new MMDeviceEnumerator();

        var captureClient = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console).CreateAudioClient();

        var REFTIMES_PER_MILLISEC = 10000;

        captureClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.None,
            REFTIMES_PER_MILLISEC * 100, 0, captureClient.MixFormat, Guid.Empty);

        // get AUDCLNT_E_NOT_INITIALIZED if not init    

        var clock = captureClient.AudioClockClient;
        Console.WriteLine("Clock Frequency: {0}", clock.Frequency);
        ulong p;
        ulong qpc;
        clock.GetPosition(out p, out qpc);
        Console.WriteLine("Clock Position: {0}:{1}", p, qpc);
        Console.WriteLine("Adjusted Position: {0}", clock.AdjustedPosition);
        Console.WriteLine("Can Adjust Position: {0}", clock.CanAdjustPosition);
        Console.WriteLine("Characteristics: {0}", clock.Characteristics);
        captureClient.Dispose();
    }
}
