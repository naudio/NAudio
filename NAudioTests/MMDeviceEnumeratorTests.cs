using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.CoreAudioApi;

namespace NAudioTests
{
    [TestFixture]
    public class MMDeviceEnumeratorTests
    {
        [Test]
        public void CanCreateMMDeviceEnumeratorInVista()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            }
        }

        [Test]       
        public void CanEnumerateDevicesInVista()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                foreach (MMDevice devices in enumerator.EnumerateAudioEndPoints(DataFlow.All,DeviceState.All))
                {
                    Console.WriteLine(devices);
                }
            }
        }

        [Test]
        public void ThrowsNotSupportedExceptionInXP()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                try
                {
                    MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                    Assert.Fail("Should have thrown an exception");
                }
                catch (NotSupportedException)
                {
                    
                }
            }
        }

    }
}
