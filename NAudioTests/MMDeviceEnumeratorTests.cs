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
        public void CanGetDefaultAudioEndpoint()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                Assert.IsNotNull(defaultAudioEndpoint);
            }            
        }

        [Test]
        public void CanActivateDefaultAudioEndpoint()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                AudioClient audioClient = defaultAudioEndpoint.AudioClient;
                Assert.IsNotNull(audioClient);
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
