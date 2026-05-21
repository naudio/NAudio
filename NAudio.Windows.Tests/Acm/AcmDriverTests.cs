using NAudio.Wave.Compression;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            Assert.That(drivers, Is.Not.Null);
            foreach (AcmDriver driver in drivers)
            {
                Assert.That(driver.DriverId, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(driver.ShortName, Is.Not.Null.And.Not.Empty);
                Debug.WriteLine(driver.LongName);
            }
        }

        [Test]
        public void DoesntFindNonexistentCodec()
        {
            Assert.That(AcmDriver.IsCodecInstalled("ASJHASDHJSAK"), Is.False);
        }

        [Test]
        public void FindsStandardCodec()
        {
            Assert.That(AcmDriver.IsCodecInstalled("MS-ADPCM"), Is.True);
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
            Assert.That(drivers, Is.Not.Null);
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
                Assert.That(formatTags, Is.Not.Null, "FormatTags");
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
                Assert.That(formatTags, Is.Not.Null, "FormatTags");
                foreach (AcmFormatTag formatTag in formatTags)
                {                                        
                    IEnumerable<AcmFormat> formats = driver.GetFormats(formatTag);
                    Assert.That(formats, Is.Not.Null);
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
