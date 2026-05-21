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
            Assert.That(deviceCount, Is.GreaterThan(0), "Expected at least one WaveIn device");
        }
        
        [Test]
        public void CanGetWaveInDeviceCapabilities()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                WaveInCapabilities capabilities = WaveIn.GetCapabilities(n);
                Assert.That(capabilities, Is.Not.Null, "Null capabilities");
                Assert.That(String.IsNullOrEmpty(capabilities.ProductName), Is.False, "Needs a name");
            }
        }

        [Test]
        public void CanGetWaveInCaps2NamesFromRegistry()
        {
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                WaveInCapabilities capabilities = WaveIn.GetCapabilities(n);
                Console.WriteLine("PName:        {0}", capabilities.ProductName);
                Console.WriteLine("Name:         {0} {1}", capabilities.NameGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.NameGuid));
                Console.WriteLine("Product:      {0} {1}", capabilities.ProductGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ProductGuid));
                Console.WriteLine("Manufacturer: {0} {1}", capabilities.ManufacturerGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ManufacturerGuid));
            }
        }


        [Test]
        public void CanGetWaveOutCaps2NamesFromRegistry()
        {
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var capabilities = WaveOut.GetCapabilities(n);
                Console.WriteLine("PName:        {0}", capabilities.ProductName);
                Console.WriteLine("Name:         {0} {1}", capabilities.NameGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.NameGuid));
                Console.WriteLine("Product:      {0} {1}", capabilities.ProductGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ProductGuid));
                Console.WriteLine("Manufacturer: {0} {1}", capabilities.ManufacturerGuid, WaveCapabilitiesHelpers.GetNameFromGuid(capabilities.ManufacturerGuid));
            }
        }
    }
}
