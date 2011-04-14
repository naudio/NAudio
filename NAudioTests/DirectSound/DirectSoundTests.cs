using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests.DirectSound
{
    [TestFixture]
    public class DirectSoundTests
    {
        [Test]
        public void CanEnumerateDevices()
        {
            foreach(var device in DirectSoundOut.Devices)
            {
                Console.WriteLine("{0} {1} {2}", device.Description, device.ModuleName, device.Guid);
            }
        }
    }
}
