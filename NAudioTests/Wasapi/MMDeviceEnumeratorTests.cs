using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.CoreAudioApi;
using System.Diagnostics;
using NAudioTests.Utils;

namespace NAudioTests.Wasapi
{
    [TestFixture]
    [Category("IntegrationTest")]
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
            foreach (MMDevice devices in enumerator.EnumerateAudioEndPoints(DataFlow.All,DeviceState.All))
            {
                Debug.WriteLine(devices);
            }
        }

        [Test]
        public void CanEnumerateCaptureDevices()
        {
            OSUtils.RequireVista();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
            {
                Debug.WriteLine(String.Format("{0}, {1}", device.FriendlyName, device.State));
            }
        }

        [Test]
        public void CanGetDefaultAudioEndpoint()
        {
            OSUtils.RequireVista();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Assert.IsNotNull(defaultAudioEndpoint);
        }

        [Test]
        public void CanActivateDefaultAudioEndpoint()
        {
            OSUtils.RequireVista();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            AudioClient audioClient = defaultAudioEndpoint.AudioClient;
            Assert.IsNotNull(audioClient);
        }

        [Test]
        public void ThrowsNotSupportedExceptionInXP()
        {
            OSUtils.RequireXP();
            Assert.Throws<NotSupportedException>(() => new MMDeviceEnumerator());
        }
    }
}
