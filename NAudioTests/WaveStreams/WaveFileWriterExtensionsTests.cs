using System;
using System.IO;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileWriterExtensionsTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);
        private static byte[] Audio => new byte[] { 1, 0, 2, 0 };

        private static WaveFileReader WriteThenRead(Action<WaveFileWriter> configure)
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                configure(w);
            }
            ms.Position = 0;
            return new WaveFileReader(ms);
        }

        // --- WriteCueList ----------------------------------------------------------

        [Test]
        public void WriteCueListRoundTrips()
        {
            var source = new CueList();
            source.Add(new Cue(100, "A"));
            source.Add(new Cue(500, "B"));

            using var reader = WriteThenRead(w =>
            {
                w.WriteCueList(source);
                w.Write(Audio, 0, Audio.Length);
            });
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Position, Is.EqualTo(100));
            Assert.That(cues[0].Label, Is.EqualTo("A"));
            Assert.That(cues[1].Position, Is.EqualTo(500));
            Assert.That(cues[1].Label, Is.EqualTo("B"));
        }

        [Test]
        public void WriteCueListWithEmptyListIsNoOp()
        {
            using var reader = WriteThenRead(w =>
            {
                w.WriteCueList(new CueList());
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(reader.Chunks.Contains("cue "), Is.False);
        }

        [Test]
        public void WriteCueListWithNullIsNoOp()
        {
            using var reader = WriteThenRead(w =>
            {
                w.WriteCueList(null);
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(reader.Chunks.Contains("cue "), Is.False);
        }

        [Test]
        public void WriteCueListThrowsOnNullWriter()
        {
            Assert.That(() => WaveFileWriterExtensions.WriteCueList(null, new CueList()),
                Throws.ArgumentNullException);
        }

        // --- WriteBroadcastExtension -----------------------------------------------

        [Test]
        public void WriteBroadcastExtensionRoundTripsAllV1Fields()
        {
            var source = new BroadcastExtension
            {
                Description = "Desc",
                Originator = "Orig",
                OriginatorReference = "Ref",
                OriginationDate = "2026-04-23",
                OriginationTime = "11:22:33",
                TimeReference = 42L,
                Version = 1,
                UniqueMaterialIdentifier = "UMID",
                CodingHistory = "A=PCM"
            };
            using var reader = WriteThenRead(w =>
            {
                w.WriteBroadcastExtension(source);
                w.Write(Audio, 0, Audio.Length);
            });
            var bext = reader.Chunks.ReadBroadcastExtension();
            Assert.That(bext, Is.Not.Null);
            Assert.That(bext.Description, Is.EqualTo("Desc"));
            Assert.That(bext.Originator, Is.EqualTo("Orig"));
            Assert.That(bext.OriginatorReference, Is.EqualTo("Ref"));
            Assert.That(bext.OriginationDate, Is.EqualTo("2026-04-23"));
            Assert.That(bext.OriginationTime, Is.EqualTo("11:22:33"));
            Assert.That(bext.TimeReference, Is.EqualTo(42L));
            Assert.That(bext.Version, Is.EqualTo(1));
            Assert.That(bext.UniqueMaterialIdentifier, Is.EqualTo("UMID"));
            Assert.That(bext.CodingHistory, Is.EqualTo("A=PCM"));
        }

        [Test]
        public void WriteBroadcastExtensionRoundTripsV2Loudness()
        {
            var source = new BroadcastExtension
            {
                Version = 2,
                LoudnessValue = -2400,
                LoudnessRange = 600,
                MaxTruePeakLevel = -50,
                MaxMomentaryLoudness = -1800,
                MaxShortTermLoudness = -2000
            };
            using var reader = WriteThenRead(w =>
            {
                w.WriteBroadcastExtension(source);
                w.Write(Audio, 0, Audio.Length);
            });
            var bext = reader.Chunks.ReadBroadcastExtension();
            Assert.That(bext.Version, Is.EqualTo(2));
            Assert.That(bext.LoudnessValue, Is.EqualTo(-2400));
            Assert.That(bext.LoudnessRange, Is.EqualTo(600));
            Assert.That(bext.MaxTruePeakLevel, Is.EqualTo(-50));
            Assert.That(bext.MaxMomentaryLoudness, Is.EqualTo(-1800));
            Assert.That(bext.MaxShortTermLoudness, Is.EqualTo(-2000));
        }

        [Test]
        public void WriteBroadcastExtensionPlacesBextBeforeData()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.WriteBroadcastExtension(new BroadcastExtension { Description = "D" });
                w.Write(Audio, 0, Audio.Length);
            }
            var text = Encoding.ASCII.GetString(ms.ToArray());
            int bextIx = text.IndexOf("bext", StringComparison.Ordinal);
            int dataIx = text.IndexOf("data", StringComparison.Ordinal);
            Assert.That(bextIx, Is.GreaterThan(0));
            Assert.That(bextIx, Is.LessThan(dataIx));
        }

        [Test]
        public void WriteBroadcastExtensionThrowsOnNullWriter()
        {
            Assert.That(() => WaveFileWriterExtensions.WriteBroadcastExtension(null,
                new BroadcastExtension()), Throws.ArgumentNullException);
        }

        [Test]
        public void WriteBroadcastExtensionThrowsOnNullBext()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            Assert.That(() => w.WriteBroadcastExtension(null), Throws.ArgumentNullException);
        }

        [Test]
        public void WriteBroadcastExtensionDateTimeHelpersFormatCorrectly()
        {
            var dt = new DateTime(2026, 4, 23, 14, 5, 6);
            Assert.That(BroadcastExtension.FormatOriginationDate(dt), Is.EqualTo("2026-04-23"));
            Assert.That(BroadcastExtension.FormatOriginationTime(dt), Is.EqualTo("14:05:06"));
        }

        // --- WriteInfoMetadata -----------------------------------------------------

        [Test]
        public void WriteInfoMetadataRoundTripsAllNamedFields()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Title");
            source.Set("IART", "Artist");
            source.Set("ICMT", "Comment");
            using var reader = WriteThenRead(w =>
            {
                w.WriteInfoMetadata(source);
                w.Write(Audio, 0, Audio.Length);
            });
            var info = reader.Chunks.ReadInfoMetadata();
            Assert.That(info, Is.Not.Null);
            Assert.That(info.Title, Is.EqualTo("Title"));
            Assert.That(info.Artist, Is.EqualTo("Artist"));
            Assert.That(info.Comments, Is.EqualTo("Comment"));
        }

        [Test]
        public void WriteInfoMetadataRoundTripsCustomId()
        {
            var source = new InfoMetadata();
            source.Set("XXXX", "custom value");
            using var reader = WriteThenRead(w =>
            {
                w.WriteInfoMetadata(source);
                w.Write(Audio, 0, Audio.Length);
            });
            var info = reader.Chunks.ReadInfoMetadata();
            Assert.That(info["XXXX"], Is.EqualTo("custom value"));
        }

        [Test]
        public void WriteInfoMetadataEmptyIsNoOp()
        {
            using var reader = WriteThenRead(w =>
            {
                w.WriteInfoMetadata(new InfoMetadata());
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(reader.Chunks.ReadInfoMetadata(), Is.Null);
        }

        [Test]
        public void WriteInfoMetadataNullIsNoOp()
        {
            using var reader = WriteThenRead(w =>
            {
                w.WriteInfoMetadata(null);
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(reader.Chunks.ReadInfoMetadata(), Is.Null);
        }

        [Test]
        public void WriteInfoMetadataThrowsOnNullWriter()
        {
            Assert.That(() => WaveFileWriterExtensions.WriteInfoMetadata(null, new InfoMetadata()),
                Throws.ArgumentNullException);
        }

        // --- InfoMetadata.Set validation -------------------------------------------

        [Test]
        public void InfoMetadataSetThrowsOnNullId()
        {
            var info = new InfoMetadata();
            Assert.That(() => info.Set(null, "value"), Throws.ArgumentNullException);
        }

        [Test]
        public void InfoMetadataSetThrowsOnInvalidIdLength()
        {
            var info = new InfoMetadata();
            Assert.That(() => info.Set("abc", "value"), Throws.ArgumentException);
            Assert.That(() => info.Set("toolong", "value"), Throws.ArgumentException);
        }

        [Test]
        public void InfoMetadataSetIsCaseInsensitive()
        {
            var info = new InfoMetadata();
            info.Set("inam", "Title");
            Assert.That(info["INAM"], Is.EqualTo("Title"));
            // Overwriting via a different-cased id should replace, not duplicate.
            info.Set("INAM", "Updated");
            Assert.That(info.Count, Is.EqualTo(1));
            Assert.That(info.Title, Is.EqualTo("Updated"));
        }

        [Test]
        public void InfoMetadataSetWithNullOrEmptyRemovesEntry()
        {
            var info = new InfoMetadata();
            info.Set("INAM", "Title");
            info.Set("INAM", null);
            Assert.That(info.Contains("INAM"), Is.False);

            info.Set("INAM", "Title");
            info.Set("INAM", string.Empty);
            Assert.That(info.Contains("INAM"), Is.False);
        }
    }
}
