using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using NUnit.Framework;
using System.Diagnostics;
using NAudio.Wave.Compression;

namespace NAudioTests.Acm
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class AcmDriverTests
    {
        [Test]
        public void CanEnumerateDrivers()
        {
            IEnumerable<AcmDriver> drivers = AcmDriver.EnumerateAcmDrivers();
            Assert.IsNotNull(drivers);
            foreach (AcmDriver driver in drivers)
            {
                Assert.GreaterOrEqual((int)driver.DriverId, 0);
                Assert.IsTrue(!String.IsNullOrEmpty(driver.ShortName));
                Debug.WriteLine(driver.LongName);
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

        [Test]
        public void CanOpenAndCloseDriver()
        {
            IEnumerable<AcmDriver> drivers = AcmDriver.EnumerateAcmDrivers();
            Assert.IsNotNull(drivers);
            foreach (AcmDriver driver in drivers)
            {
                driver.Open();
                driver.Close();
            }
        }

        [Test]
        public void CanEnumerateFormatTags()
        {
            foreach(AcmDriver driver in AcmDriver.EnumerateAcmDrivers())
            {
                Debug.WriteLine("Enumerating Format Tags for " + driver.LongName);
                driver.Open();
                IEnumerable<AcmFormatTag> formatTags = driver.FormatTags;
                Assert.IsNotNull(formatTags, "FormatTags");
                foreach(AcmFormatTag formatTag in formatTags)
                {
                    Debug.WriteLine(String.Format("{0} {1} {2} Standard formats: {3} Support Flags: {4} Format Size: {5}",
                        formatTag.FormatTagIndex, 
                        formatTag.FormatTag,
                        formatTag.FormatDescription,
                        formatTag.StandardFormatsCount,
                        formatTag.SupportFlags,
                        formatTag.FormatSize));
                }
                driver.Close();
            }
        }

        [Test]
        public void CanEnumerateFormats()
        {
            using (AcmDriver driver = AcmDriver.FindByShortName("MS-ADPCM"))
            {
                driver.Open();
                IEnumerable<AcmFormatTag> formatTags = driver.FormatTags;
                Assert.IsNotNull(formatTags, "FormatTags");
                foreach (AcmFormatTag formatTag in formatTags)
                {                                        
                    IEnumerable<AcmFormat> formats = driver.GetFormats(formatTag);
                    Assert.IsNotNull(formats);
                    foreach (AcmFormat format in formats)
                    {
                        Debug.WriteLine(String.Format("{0} {1} {2} {3} {4}",
                            format.FormatIndex,
                            format.FormatTag,
                            format.FormatDescription,
                            format.WaveFormat,
                            format.SupportFlags));
                    }
                }
            }
        }
    }
}
