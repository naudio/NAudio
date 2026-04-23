using System;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class BextInterpreterTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);
        private static byte[] Audio => new byte[16];

        private static WaveFileReader OpenWithBext(BroadcastExtension bext)
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.WriteBroadcastExtension(bext);
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            return new WaveFileReader(ms);
        }

        [Test]
        public void InstanceIsSingleton()
        {
            Assert.That(BextInterpreter.Instance, Is.Not.Null);
            Assert.That(BextInterpreter.Instance, Is.SameAs(BextInterpreter.Instance));
        }

        [Test]
        public void ReturnsNullWhenBextChunkMissing()
        {
            using var reader = new WaveFileReader(new MemoryStream(WaveFileBuilder.Build(Format, Audio)));
            Assert.That(reader.Chunks.Read(BextInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ReturnsNullWhenBextChunkTruncated()
        {
            // 602 bytes is the v1 minimum; 100 is invalid. Use WaveFileBuilder for the malformed
            // payload since the real writer won't produce a too-short bext chunk.
            var bytes = WaveFileBuilder.Build(Format, Audio,
                new WaveFileBuilder.Chunk("bext", new byte[100]));
            using var reader = new WaveFileReader(new MemoryStream(bytes));
            Assert.That(reader.Chunks.Read(BextInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ParsesVersion1Fields()
        {
            var source = new BroadcastExtension
            {
                Description = "Test description",
                Originator = "NAudio",
                OriginatorReference = "REF-001",
                OriginationDate = "2026-04-22",
                OriginationTime = "12:34:56",
                TimeReference = 1234567890L,
                Version = 1,
                UniqueMaterialIdentifier = "UMID-ABC",
                CodingHistory = "A=PCM,F=48000,W=16,M=mono"
            };

            using var reader = OpenWithBext(source);
            var bext = reader.Chunks.Read(BextInterpreter.Instance);
            Assert.That(bext, Is.Not.Null);
            Assert.That(bext.Description, Is.EqualTo("Test description"));
            Assert.That(bext.Originator, Is.EqualTo("NAudio"));
            Assert.That(bext.OriginatorReference, Is.EqualTo("REF-001"));
            Assert.That(bext.OriginationDate, Is.EqualTo("2026-04-22"));
            Assert.That(bext.OriginationTime, Is.EqualTo("12:34:56"));
            Assert.That(bext.TimeReference, Is.EqualTo(1234567890L));
            Assert.That(bext.Version, Is.EqualTo(1));
            Assert.That(bext.UniqueMaterialIdentifier, Is.EqualTo("UMID-ABC"));
            Assert.That(bext.CodingHistory, Is.EqualTo("A=PCM,F=48000,W=16,M=mono"));
        }

        [Test]
        public void Version1DoesNotPopulateLoudnessFields()
        {
            using var reader = OpenWithBext(new BroadcastExtension { Version = 1 });
            var bext = reader.Chunks.Read(BextInterpreter.Instance);
            Assert.That(bext.LoudnessValue, Is.Null);
            Assert.That(bext.LoudnessRange, Is.Null);
            Assert.That(bext.MaxTruePeakLevel, Is.Null);
            Assert.That(bext.MaxMomentaryLoudness, Is.Null);
            Assert.That(bext.MaxShortTermLoudness, Is.Null);
        }

        [Test]
        public void Version2PopulatesLoudnessFields()
        {
            var source = new BroadcastExtension
            {
                Version = 2,
                LoudnessValue = -2300,      // -23.00 LUFS × 100
                LoudnessRange = 500,
                MaxTruePeakLevel = -100,
                MaxMomentaryLoudness = -1500,
                MaxShortTermLoudness = -1800
            };
            using var reader = OpenWithBext(source);
            var bext = reader.Chunks.Read(BextInterpreter.Instance);
            Assert.That(bext.Version, Is.EqualTo(2));
            Assert.That(bext.LoudnessValue, Is.EqualTo(-2300));
            Assert.That(bext.LoudnessRange, Is.EqualTo(500));
            Assert.That(bext.MaxTruePeakLevel, Is.EqualTo(-100));
            Assert.That(bext.MaxMomentaryLoudness, Is.EqualTo(-1500));
            Assert.That(bext.MaxShortTermLoudness, Is.EqualTo(-1800));
        }

        [Test]
        public void NullTerminatedFixedFieldsAreTrimmed()
        {
            // "short" is 5 bytes; the 256-byte Description field is zero-padded after it and
            // the parser should trim.
            using var reader = OpenWithBext(new BroadcastExtension { Description = "short" });
            var bext = reader.Chunks.Read(BextInterpreter.Instance);
            Assert.That(bext.Description, Is.EqualTo("short"));
            Assert.That(bext.Description.Length, Is.EqualTo(5));
        }

        [Test]
        public void EmptyCodingHistoryYieldsEmptyString()
        {
            using var reader = OpenWithBext(new BroadcastExtension { Version = 1 });
            var bext = reader.Chunks.Read(BextInterpreter.Instance);
            Assert.That(bext.CodingHistory, Is.EqualTo(string.Empty));
        }

        [Test]
        public void RoundTripsBextViaWriteBroadcastExtension()
        {
            var path = Path.Combine(Path.GetTempPath(), "bwf_rt_" + Guid.NewGuid() + ".wav");
            try
            {
                var source = new BroadcastExtension
                {
                    Description = "Integration Test",
                    Originator = "NAudio",
                    OriginatorReference = "TEST_REF",
                    OriginationDate = BroadcastExtension.FormatOriginationDate(new DateTime(2026, 4, 22)),
                    OriginationTime = BroadcastExtension.FormatOriginationTime(new DateTime(1, 1, 1, 10, 20, 30)),
                    TimeReference = 987654321L,
                    Version = 1,
                    UniqueMaterialIdentifier = "UMID-XYZ",
                    CodingHistory = "A=PCM,F=44100,W=16,M=stereo"
                };
                using (var w = new WaveFileWriter(path, new WaveFormat(44100, 16, 2), new WaveFileWriterOptions { EnableRf64 = true }))
                {
                    w.WriteBroadcastExtension(source);
                    w.Write(new byte[64], 0, 64);
                }

                using var reader = new WaveFileReader(path);
                var bext = reader.Chunks.Read(BextInterpreter.Instance);
                Assert.That(bext, Is.Not.Null);
                Assert.That(bext.Description, Is.EqualTo("Integration Test"));
                Assert.That(bext.Originator, Is.EqualTo("NAudio"));
                Assert.That(bext.OriginatorReference, Is.EqualTo("TEST_REF"));
                Assert.That(bext.OriginationDate, Is.EqualTo("2026-04-22"));
                Assert.That(bext.OriginationTime, Is.EqualTo("10:20:30"));
                Assert.That(bext.TimeReference, Is.EqualTo(987654321L));
                Assert.That(bext.Version, Is.EqualTo(1));
                Assert.That(bext.UniqueMaterialIdentifier, Is.EqualTo("UMID-XYZ"));
                Assert.That(bext.CodingHistory, Is.EqualTo("A=PCM,F=44100,W=16,M=stereo"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
