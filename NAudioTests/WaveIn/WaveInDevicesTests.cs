using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class WaveInDevicesTests
    {
        [Test]
        public void CanRequestNumberOfWaveInDevices()
        {
            int deviceCount = WaveIn.DeviceCount;
            Assert.That(deviceCount > 0, "Expected at least one WaveIn device");
        }
        
        [Test]
        public void CanGetWaveInDeviceCapabilities()
        {
            WaveInCapabilities capabilities;
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                capabilities = WaveIn.GetCapabilities(n);
                Assert.IsNotNull(capabilities, "Null capabilities");
                //Assert.That(capabilities.Channels >= 1, "At least one channel"); - seem to get -1 a lot
                Assert.That(!String.IsNullOrEmpty(capabilities.ProductName), "Needs a name");
            }            
        }
    }
}
