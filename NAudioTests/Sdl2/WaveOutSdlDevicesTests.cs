using System;
using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NUnit.Framework;

namespace NAudioTests.Sdl2
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveOutSdlDevicesTests
    {
        [Test]
        public void CanRequestNumberOfWaveOutSdlDevices()
        {
            int deviceCount = WaveOutSdl.DeviceCount;
            Assert.That(deviceCount > 0, "Expected at least one WaveOutSdl device");
        }
        
        [Test]
        public void CanGetWaveOutSdlDeviceCapabilities()
        {
            for (int n = 0; n < WaveOutSdl.DeviceCount; n++)
            {
                WaveOutSdlCapabilities capabilities = WaveOutSdl.GetCapabilities(n);
                Assert.That(!String.IsNullOrEmpty(capabilities.DeviceName), "Needs a device name");
            }
        }

        [Test]
        public void CanGetWaveOutSdlDeviceCapabilitiesList()
        {
            var capabilitiesList = WaveOutSdl.GetCapabilitiesList();
            Assert.That(capabilitiesList.Count > 0, "Expected at least one WaveOutSdlCapabilities");
        }

        [Test]
        public void CanGetWaveOutSdlDeviceDefaultCapabilities()
        {
            Assert.DoesNotThrow(() => WaveOutSdl.GetDefaultDeviceCapabilities());
        }
    }
}
