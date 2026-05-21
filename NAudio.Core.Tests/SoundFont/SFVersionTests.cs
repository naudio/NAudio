using System.IO;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudioTests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class SFVersionTests
    {
        [Test]
        public void ParsesVersion2Point04()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(vMajor: 2, vMinor: 4);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.SoundFontVersion.Major, Is.EqualTo(2));
            Assert.That(sf.FileInfo.SoundFontVersion.Minor, Is.EqualTo(4));
        }

        [Test]
        public void ParsesVersion2Point01()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(vMajor: 2, vMinor: 1);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.SoundFontVersion.Major, Is.EqualTo(2));
            Assert.That(sf.FileInfo.SoundFontVersion.Minor, Is.EqualTo(1));
        }

        [Test]
        public void ParsesVersion3Point0()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(vMajor: 3, vMinor: 0);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.SoundFontVersion.Major, Is.EqualTo(3));
            Assert.That(sf.FileInfo.SoundFontVersion.Minor, Is.EqualTo(0));
        }

        [Test]
        public void SFVersionMajorAndMinorAreReadWrite()
        {
            var v = new SFVersion();
            v.Major = 2;
            v.Minor = 4;
            Assert.That(v.Major, Is.EqualTo(2));
            Assert.That(v.Minor, Is.EqualTo(4));
        }

        #region Spec violation test (LOW severity)

        [Test]
        [Category("SpecViolation")]
        [Description("SF2 spec: sfVersionTag fields are WORD (unsigned 16-bit, range 0-65535). " +
                     "SFVersion uses signed short (range -32768 to 32767). Values above 32767 " +
                     "are misinterpreted as negative numbers.")]
        public void VersionFieldsShouldBeUnsignedPerSpec()
        {
            // Build an SF2 with a hypothetical minor version of 0x8000 (32768 unsigned)
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(vMinor: 0x8000);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // FAILS: SFVersion.Minor is declared as short (signed 16-bit)
            // Reading 0x8000 as Int16 gives -32768 instead of the correct 32768
            // The SF2 spec defines these fields as WORD (unsigned 16-bit)
            Assert.That(sf.FileInfo.SoundFontVersion.Minor, Is.GreaterThan(0),
                "Version minor 0x8000 should be positive (32768) per SF2 spec WORD type, " +
                "but SFVersion.Minor is short, reading it as -32768");
        }

        #endregion
    }
}
