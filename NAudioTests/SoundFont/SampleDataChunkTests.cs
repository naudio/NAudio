using System.IO;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudioTests.SoundFont
{
    [TestFixture]
    [Category("UnitTest")]
    public class SampleDataChunkTests
    {
        [Test]
        public void SampleDataIsNotNull()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.SampleData, Is.Not.Null);
            Assert.That(sf.SampleData.Length, Is.GreaterThan(0));
        }

        #region Spec violation tests (HIGH severity)

        [Test]
        [Category("SpecViolation")]
        [Description("SF2 spec section 6.2: The sdta LIST chunk contains a smpl sub-chunk. " +
                     "SampleData should contain only the raw PCM sample bytes from the smpl sub-chunk, " +
                     "not the sdta/smpl chunk headers. Current implementation calls GetData() which " +
                     "reads from DataOffset, including the 'sdta' identifier and smpl chunk header.")]
        public void SampleDataShouldContainOnlyRawPcmBytes()
        {
            // Build SF2 with known sample data bytes
            var rawSamples = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44 };
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(sampleData: rawSamples);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // FAILS: SampleData[0] is 0x73 ('s' from "sdta") instead of 0xAA
            // The sdta LIST's GetData() reads from DataOffset which includes the "sdta" type identifier
            // followed by the "smpl" sub-chunk header (ID + size), before the actual sample data.
            // Expected: [0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44]
            // Actual:   [s, d, t, a, s, m, p, l, 0x08, 0x00, 0x00, 0x00, 0xAA, 0xBB, ...]
            Assert.That(sf.SampleData[0], Is.EqualTo(0xAA),
                "First byte of SampleData should be the first PCM sample byte, " +
                "not part of the sdta/smpl chunk headers");
        }

        [Test]
        [Category("SpecViolation")]
        [Description("SF2 spec section 6.2: SampleData length should match the smpl sub-chunk data size, " +
                     "not include the sdta type identifier (4 bytes) and smpl chunk header (8 bytes).")]
        public void SampleDataLengthShouldMatchRawSampleLength()
        {
            var rawSamples = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44 };
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(sampleData: rawSamples);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            // FAILS: SampleData.Length includes the "sdta" (4 bytes) + "smpl" header (4+4 bytes)
            // Expected: 8 (just the raw PCM bytes)
            // Actual:   20 (4 "sdta" + 4 "smpl" + 4 size + 8 raw data)
            Assert.That(sf.SampleData.Length, Is.EqualTo(rawSamples.Length),
                "SampleData length should equal the raw PCM data length, " +
                "not include sdta/smpl chunk overhead bytes");
        }

        [Test]
        [Category("SpecViolation")]
        [Description("SF2 spec: SampleHeader offsets reference positions within the smpl data. " +
                     "If SampleData includes chunk headers, these offsets will be wrong.")]
        public void SampleHeaderOffsetsShouldAlignWithSampleData()
        {
            var rawSamples = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33, 0x44 };
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont(sampleData: rawSamples);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            var header = sf.SampleHeaders[0];
            // Sample header Start=0, so the first sample point should be at byte offset 0 in SampleData
            // Each sample point is 2 bytes (16-bit PCM)
            int byteOffset = (int)(header.Start * 2);

            // FAILS: At byteOffset 0, SampleData contains "sdta" chunk header, not PCM data
            Assert.That(byteOffset, Is.LessThan(sf.SampleData.Length),
                "Sample byte offset should be within SampleData bounds");
            Assert.That(sf.SampleData[byteOffset], Is.EqualTo(0xAA),
                "Sample data at header.Start offset should contain actual PCM data, " +
                "not chunk header bytes");
        }

        #endregion
    }
}
