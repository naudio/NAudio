using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Wave;

namespace NAudioTests
{
    [TestFixture]
    public class AcmDriverTests
    {
        [Test]
        public void CanEnumerateDrivers()
        {
            IEnumerable<AcmDriver> drivers = AcmDriver.EnumerateAcmDrivers();
            Assert.IsNotNull(drivers);
            foreach (AcmDriver driver in drivers)
            {
                Assert.GreaterOrEqual(driver.DriverId, 0);
                Assert.IsTrue(!String.IsNullOrEmpty(driver.ShortName));
                Console.WriteLine(driver.LongName);
            }
        }

        [Test]
        public void DoesntFindNonexistentCodec()
        {
            Assert.IsFalse(AcmDriver.IsCodecInstalled("ASJHASDHJSAK"));
        }

        [Test]
        public void FindsStandardCodec()
        {
            Assert.IsTrue(AcmDriver.IsCodecInstalled("MS-ADPCM"));
        }

        [Test]
        public void HasFindByShortNameMethod()
        {
            AcmDriver driver = AcmDriver.FindByShortName("WM-AUDIO");
        }
    }
}
