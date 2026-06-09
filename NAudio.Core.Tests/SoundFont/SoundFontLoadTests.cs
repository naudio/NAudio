using System;
using System.IO;
using System.Text;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Core.Tests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class SoundFontLoadTests
    {
        [Test]
        public void LoadsMinimalValidSoundFont()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf, Is.Not.Null);
            Assert.That(sf.Presets, Is.Not.Null);
            Assert.That(sf.Instruments, Is.Not.Null);
            Assert.That(sf.SampleHeaders, Is.Not.Null);
            Assert.That(sf.SampleData, Is.Not.Null);
            Assert.That(sf.FileInfo, Is.Not.Null);
        }

        [Test]
        public void LoadsRichSoundFontWithMultiplePresetsAndInstruments()
        {
            var sf2 = SoundFontTestHelper.BuildRichSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Presets.Length, Is.EqualTo(2));
            Assert.That(sf.Instruments.Length, Is.EqualTo(2));
            Assert.That(sf.SampleHeaders.Length, Is.EqualTo(2));
        }

        [Test]
        public void RejectsNonRiffFile()
        {
            var bytes = Encoding.ASCII.GetBytes("NOT_RIFF_DATA_HERE!!");

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(bytes)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Not a RIFF file"));
        }

        [Test]
        public void RejectsRiffFileWithWrongFormType()
        {
            // Build a RIFF file with "WAVE" instead of "sfbk"
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write((uint)4);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            var bytes = ms.ToArray();

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(bytes)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Not a SoundFont"));
        }

        [Test]
        public void RejectsWhenFirstChunkIsNotList()
        {
            // Build RIFF/sfbk but with a non-LIST chunk where INFO LIST should be
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            var content = SoundFontTestHelper.Concat(
                Encoding.ASCII.GetBytes("sfbk"),
                SoundFontTestHelper.Chunk("data", new byte[4]));
            bw.Write((uint)content.Length);
            bw.Write(content);
            var bytes = ms.ToArray();

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(bytes)),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void RejectsMissingIfilSubChunk()
        {
            // Build INFO list with only INAM, no ifil
            var infoList = SoundFontTestHelper.ListChunk("INFO",
                SoundFontTestHelper.StringInfoChunk("INAM", "TestBnk"));
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(infoList, sdta, pdta);

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Missing SoundFont version"));
        }

        [Test]
        public void RejectsMissingINAMSubChunk()
        {
            // Build INFO list with only ifil, no INAM
            var infoList = SoundFontTestHelper.ListChunk("INFO",
                SoundFontTestHelper.IfilChunk());
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(infoList, sdta, pdta);

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Missing SoundFont name"));
        }

        [Test]
        public void AcceptsMissingIsngSubChunk()
        {
            // Per issue #150, isng is not required in practice
            var infoList = SoundFontTestHelper.ListChunk("INFO",
                SoundFontTestHelper.IfilChunk(),
                SoundFontTestHelper.StringInfoChunk("INAM", "TestBnk"));
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(infoList, sdta, pdta);

            Assert.DoesNotThrow(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)));
        }

        [Test]
        public void RejectsInvalidSdtaChunk()
        {
            var info = SoundFontTestHelper.BuildInfoList();
            // Build sdta LIST but with wrong type identifier
            var sdta = SoundFontTestHelper.ListChunk("xxxx",
                SoundFontTestHelper.Chunk("smpl", new byte[8]));
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Not a sample data chunk"));
        }

        [Test]
        public void RejectsInvalidPdtaChunk()
        {
            var info = SoundFontTestHelper.BuildInfoList();
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            // Build pdta LIST but with wrong type identifier
            var pdta = SoundFontTestHelper.ListChunk("xxxx",
                SoundFontTestHelper.Chunk("phdr", new byte[76]));
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            Assert.That(() => new NAudio.SoundFont.SoundFont(new MemoryStream(sf2)),
                Throws.TypeOf<InvalidDataException>()
                    .With.Message.Contains("Not a presets data chunk"));
        }

        [Test]
        public void LoadsSoundFontWithOddSizedChunkRequiringRiffPadding()
        {
            // A real-world SF2 had an odd-length INFO chunk followed by a RIFF pad
            // byte (not counted in the chunk size). The reader must skip that pad
            // byte or every subsequent chunk misaligns ("Not a presets data chunk
            // (LIST)"). Place the odd chunk *before* INAM so a missed pad byte
            // corrupts the bank-name read too. Chunk() now emits the pad byte, so
            // "odd" (3 bytes) produces an odd-sized, word-aligned chunk.
            var infoList = SoundFontTestHelper.ListChunk("INFO",
                SoundFontTestHelper.IfilChunk(),
                SoundFontTestHelper.StringInfoChunk("isng", "EMU8000"),
                SoundFontTestHelper.Chunk("ICMT", Encoding.ASCII.GetBytes("odd")), // 3 bytes + pad
                SoundFontTestHelper.StringInfoChunk("INAM", "TestBnk"));
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[8]);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(infoList, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.FileInfo.BankName, Is.EqualTo("TestBnk"));
            Assert.That(sf.FileInfo.Comments, Is.EqualTo("odd"));
            Assert.That(sf.Presets.Length, Is.GreaterThan(0));
            Assert.That(sf.SampleHeaders.Length, Is.GreaterThan(0));
        }

        [Test]
        public void LoadsRomBasedSoundFontWithEmptySmpl()
        {
            // ROM-based SoundFonts (samples in an EMU8000/AWE32 wavetable ROM, not
            // embedded) carry an empty smpl chunk, so the sdta LIST is just
            // "sdta" + an 8-byte zero-length smpl header (size 12). The empty smpl
            // sits exactly at the list boundary; the reader must still consume it,
            // or the stream misaligns and pdta loading fails with
            // "Not a presets data chunk (LIST)".
            var info = SoundFontTestHelper.BuildInfoList();
            var sdta = SoundFontTestHelper.BuildSdtaList(new byte[0]); // empty smpl
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList();
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);

            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.SampleData, Is.Not.Null);
            Assert.That(sf.SampleData.Length, Is.EqualTo(0));
            Assert.That(sf.Presets.Length, Is.GreaterThan(0));
            Assert.That(sf.Instruments.Length, Is.GreaterThan(0));
            Assert.That(sf.SampleHeaders.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ToStringIncludesInfoAndPresets()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            var str = sf.ToString();

            Assert.That(str, Does.Contain("Info Chunk"));
            Assert.That(str, Does.Contain("Presets Chunk"));
        }
    }
}
