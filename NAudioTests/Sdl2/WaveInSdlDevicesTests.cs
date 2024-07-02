using System;
using NAudio.Sdl2;
using NAudio.Sdl2.Structures;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Sdl2
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveInSdlDevicesTests
    {
        [Test]
        public void CanRequestNumberOfWaveInSdlDevices()
        {
            int deviceCount = WaveInSdl.DeviceCount;
            Assert.That(deviceCount > 0, "Expected at least one WaveInSdl device");
        }
        
        [Test]
        public void CanGetWaveInSdlDeviceCapabilities()
        {
            for (int n = 0; n < WaveInSdl.DeviceCount; n++)
            {
                WaveInSdlCapabilities capabilities = WaveInSdl.GetCapabilities(n);
                Assert.That(!String.IsNullOrEmpty(capabilities.DeviceName), "Needs a device name");
            }
        }

        [Test]
        public void CanGetWaveInSdlDeviceCapabilitiesList()
        {
            var capabilitiesList = WaveInSdl.GetCapabilitiesList();
            Assert.That(capabilitiesList.Count > 0, "Expected at least one WaveInSdlCapabilities");
        }

        [Test]
        public void CanGetWaveInSdlDeviceDefaultCapabilities()
        {
            Assert.DoesNotThrow(() => WaveInSdl.GetDefaultDeviceCapabilities());
        }
    }
}
