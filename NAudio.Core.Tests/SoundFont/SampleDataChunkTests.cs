using System.IO;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Core.Tests.SoundFont
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

        [Test]
        public void SixteenBitSoundFontHasNo24BitData()
        {
            var sf2 = SoundFontTestHelper.BuildMinimalSoundFont();
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Has24BitSamples, Is.False);
            Assert.That(sf.SampleData24, Is.Null);
        }

        [Test]
        public void LoadsSm24LowByteDataWhenPresent()
        {
            // 4 samples => 8 bytes of smpl (high 16 bits) + 4 bytes of sm24 (low 8 bits)
            var smpl = new byte[] { 0x00, 0x10, 0x00, 0x20, 0x00, 0x30, 0x00, 0x40 };
            var sm24 = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

            var info = SoundFontTestHelper.BuildInfoList();
            var sdta = SoundFontTestHelper.BuildSdta24List(smpl, sm24);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList(sampleEnd: 3);
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Has24BitSamples, Is.True);
            Assert.That(sf.SampleData, Is.EqualTo(smpl));
            Assert.That(sf.SampleData24, Is.EqualTo(sm24));
        }

        [Test]
        public void IgnoresSm24ChunkThatDoesNotPairWithSmpl()
        {
            // sm24 too short to be one byte per 16-bit sample => ignored
            var smpl = new byte[] { 0x00, 0x10, 0x00, 0x20, 0x00, 0x30, 0x00, 0x40 };
            var sm24 = new byte[] { 0xAA }; // only 1 byte for 4 samples

            var info = SoundFontTestHelper.BuildInfoList();
            var sdta = SoundFontTestHelper.BuildSdta24List(smpl, sm24);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList(sampleEnd: 3);
            var sf2 = SoundFontTestHelper.BuildSoundFont(info, sdta, pdta);
            var sf = new NAudio.SoundFont.SoundFont(new MemoryStream(sf2));

            Assert.That(sf.Has24BitSamples, Is.False);
            Assert.That(sf.SampleData24, Is.Null);
        }

        // ---- ReadSampleDataFloat: the canonical 16/24-bit decode ----

        private static NAudio.SoundFont.SoundFont Load(byte[] smpl, byte[] sm24 = null)
        {
            var info = SoundFontTestHelper.BuildInfoList();
            var sdta = sm24 == null
                ? SoundFontTestHelper.BuildSdtaList(smpl)
                : SoundFontTestHelper.BuildSdta24List(smpl, sm24);
            var pdta = SoundFontTestHelper.BuildMinimalPdtaList(sampleEnd: (uint)(smpl.Length / 2 - 1));
            return new NAudio.SoundFont.SoundFont(
                new MemoryStream(SoundFontTestHelper.BuildSoundFont(info, sdta, pdta)));
        }

        [Test]
        public void ReadSampleDataFloatDecodes16BitSamples()
        {
            // 16384 (0x4000) and -16384 (0xC000), little-endian
            var smpl = new byte[] { 0x00, 0x40, 0x00, 0xC0 };
            var samples = Load(smpl).ReadSampleDataFloat();

            Assert.That(samples, Has.Length.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(0.5f));
            Assert.That(samples[1], Is.EqualTo(-0.5f));
        }

        [Test]
        public void ReadSampleDataFloatCombinesTheSm24LowBytes()
        {
            // per SF2.04: sample = (smpl16 << 8) | sm24, scaled by 2^23
            var smpl = new byte[]
            {
                0x01, 0x00, // 1      -> (1 << 8) | 0xFF = 511
                0xFF, 0xFF, // -1     -> (-1 << 8) | 0x00 = -256
                0x00, 0x80, // -32768 -> (-32768 << 8) | 0x00 = -8388608 (full scale)
            };
            var sm24 = new byte[] { 0xFF, 0x00, 0x00, 0x00 }; // padded to even length
            var samples = Load(smpl, sm24).ReadSampleDataFloat();

            const float scale = 1f / 8388608f;
            Assert.That(samples, Has.Length.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(511 * scale));
            Assert.That(samples[1], Is.EqualTo(-256 * scale));
            Assert.That(samples[2], Is.EqualTo(-1f));
        }

        [Test]
        public void ReadSampleDataFloatFallsBackTo16BitWhenSm24IsInvalid()
        {
            var smpl = new byte[] { 0x00, 0x40, 0x00, 0x40 };
            var sm24 = new byte[] { 0xAA }; // too short for 2 samples -> ignored
            var samples = Load(smpl, sm24).ReadSampleDataFloat();

            Assert.That(samples[0], Is.EqualTo(0.5f));
            Assert.That(samples[1], Is.EqualTo(0.5f));
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
