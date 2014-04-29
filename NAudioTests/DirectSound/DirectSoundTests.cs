using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudioTests.DirectSound
{
    [TestFixture]
    public class DirectSoundTests
    {
        [Test]
        [Category("IntegrationTest")]
        public void CanEnumerateDevices()
        {
            foreach(var device in DirectSoundOut.Devices)
            {
                Debug.WriteLine(String.Format("{0} {1} {2}", device.Description, device.ModuleName, device.Guid));
            }
        }
    }
}
