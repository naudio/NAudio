using System.IO;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudioTests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class InfoChunkTests
    {
        [Test]
        public void ParsesSoundFontVersion()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(vMajor: 2, vMinor: 4);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.SoundFontVersion.Major, Is.EqualTo(2));
            Assert.That(sf.FileInfo.SoundFontVersion.Minor, Is.EqualTo(4));
        }

        [Test]
        public void ParsesBankName()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(bankName: "MyBank");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.BankName, Is.EqualTo("MyBank"));
        }

        [Test]
        public void ParsesWaveTableSoundEngine()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.WaveTableSoundEngine, Is.EqualTo("EMU8000"));
        }

        [Test]
        public void ParsesAllOptionalInfoSubChunks()
        {
            var sf2 = SoundFontTestHelper.BuildRichSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.DataROM, Is.EqualTo("TestROM"));
            Assert.That(sf.FileInfo.ROMVersion, Is.Not.Null);
            Assert.That(sf.FileInfo.ROMVersion.Major, Is.EqualTo(1));
            Assert.That(sf.FileInfo.ROMVersion.Minor, Is.EqualTo(0));
            Assert.That(sf.FileInfo.CreationDate, Is.EqualTo("2024-01-01"));
            Assert.That(sf.FileInfo.Author, Is.EqualTo("Test Author"));
            Assert.That(sf.FileInfo.TargetProduct, Is.EqualTo("Test Product"));
            Assert.That(sf.FileInfo.Copyright, Is.EqualTo("Copyright Test"));
            Assert.That(sf.FileInfo.Comments, Is.EqualTo("Test Comments"));
            Assert.That(sf.FileInfo.Tools, Is.EqualTo("TestTool"));
        }

        [Test]
        public void OptionalFieldsAreNullWhenNotPresent()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.DataROM, Is.Null);
            Assert.That(sf.FileInfo.ROMVersion, Is.Null);
            Assert.That(sf.FileInfo.CreationDate, Is.Null);
            Assert.That(sf.FileInfo.Author, Is.Null);
            Assert.That(sf.FileInfo.TargetProduct, Is.Null);
            Assert.That(sf.FileInfo.Copyright, Is.Null);
            Assert.That(sf.FileInfo.Comments, Is.Null);
            Assert.That(sf.FileInfo.Tools, Is.Null);
        }

        [Test]
        public void ParsesCreationDate()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("ICRD", "March 18 2026")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.CreationDate, Is.EqualTo("March 18 2026"));
        }

        [Test]
        public void ParsesAuthor()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("IENG", "John Smith")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.Author, Is.EqualTo("John Smith"));
        }

        [Test]
        public void ParsesCopyright()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("ICOP", "Copyright 2026")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.Copyright, Is.EqualTo("Copyright 2026"));
        }

        [Test]
        public void ParsesComments()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("ICMT", "Some comments")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.Comments, Is.EqualTo("Some comments"));
        }

        [Test]
        public void ParsesTools()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("ISFT", "NAudio 2.0")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.Tools, Is.EqualTo("NAudio 2.0"));
        }

        [Test]
        public void ParsesTargetProduct()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("IPRD", "SBAWE32")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.TargetProduct, Is.EqualTo("SBAWE32"));
        }

        [Test]
        public void ToStringOutputsFieldValues()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(bankName: "MyTestBank");
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            var str = sf.FileInfo.ToString();

            Assert.That(str, Does.Contain("MyTestBank"));
            Assert.That(str, Does.Contain("EMU8000"));
        }

        [Test]
        [Description("BUG: InfoChunk.ToString() has a hardcoded TODO instead of the actual Comments value")]
        public void ToStringShouldIncludeActualComments()
        {
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("ICMT", "My comments here")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // Verify the property is set correctly
            Assert.That(sf.FileInfo.Comments, Is.EqualTo("My comments here"));

            // BUG: ToString() has "TODO-fix comments" hardcoded instead of actual Comments value
            var str = sf.FileInfo.ToString();
            Assert.That(str, Does.Contain("My comments here"),
                "ToString() should include actual Comments value, not 'TODO-fix comments'");
        }

        #region Spec violation tests (MEDIUM severity)

        [Test]
        [Category("SpecViolation")]
        [Description("SF2 spec: Unknown INFO sub-chunks should be ignored for forward compatibility. " +
                     "Current code throws InvalidDataException on unknown sub-chunks.")]
        public void ShouldIgnoreUnknownInfoSubChunks()
        {
            // Build an SF2 with an unknown INFO sub-chunk "IKEY"
            // The SF2 spec recommends ignoring unknown sub-chunks for forward compatibility
            var info = SoundFontTestHelper.BuildInfoList(extraSubChunks: new[]
            {
                SoundFontTestHelper.StringInfoChunk("IKEY", "somedata")
            });
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            // FAILS: InfoChunk throws InvalidDataException("Unknown chunk type IKEY")
            // Should silently ignore the unknown sub-chunk per spec
            Assert.DoesNotThrow(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)),
                "Unknown INFO sub-chunks should be ignored for forward compatibility (SF2 spec section 5)");
        }

        #endregion
    }
}
