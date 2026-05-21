using System.IO;
using System.Linq;
using NAudio.Utils;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class InfoListInterpreterTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);
        private static byte[] Audio => new byte[16];

        private static WaveFileReader OpenWithInfo(InfoMetadata info)
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.WriteInfoMetadata(info);
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            return new WaveFileReader(ms);
        }

        [Test]
        public void InstanceIsSingleton()
        {
            Assert.That(InfoListInterpreter.Instance, Is.Not.Null);
            Assert.That(InfoListInterpreter.Instance, Is.SameAs(InfoListInterpreter.Instance));
        }

        [Test]
        public void ReturnsNullWhenNoListChunk()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Read(InfoListInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ReturnsNullWhenListIsAdtlNotInfo()
        {
            // File with cue labels (adtl) but no INFO list — the interpreter must distinguish.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddCue(1, "Label");
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Read(InfoListInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ParsesNamedFieldsFromInfoList()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Song Title");
            source.Set("IART", "Artist Name");
            source.Set("IPRD", "Album Name");
            source.Set("ICMT", "Comments here");
            source.Set("ICOP", "2026 NAudio");
            source.Set("ICRD", "2026-04-22");
            source.Set("IENG", "Engineer");
            source.Set("IGNR", "Electronic");
            source.Set("IKEY", "foo;bar;baz");
            source.Set("ISFT", "NAudio tests");
            source.Set("ISRC", "Studio");
            source.Set("ITCH", "Technician");
            source.Set("ISBJ", "Test subject");
            source.Set("ITRK", "7");

            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.Title, Is.EqualTo("Song Title"));
            Assert.That(info.Artist, Is.EqualTo("Artist Name"));
            Assert.That(info.Product, Is.EqualTo("Album Name"));
            Assert.That(info.Comments, Is.EqualTo("Comments here"));
            Assert.That(info.Copyright, Is.EqualTo("2026 NAudio"));
            Assert.That(info.CreationDate, Is.EqualTo("2026-04-22"));
            Assert.That(info.Engineer, Is.EqualTo("Engineer"));
            Assert.That(info.Genre, Is.EqualTo("Electronic"));
            Assert.That(info.Keywords, Is.EqualTo("foo;bar;baz"));
            Assert.That(info.Software, Is.EqualTo("NAudio tests"));
            Assert.That(info.Source, Is.EqualTo("Studio"));
            Assert.That(info.Technician, Is.EqualTo("Technician"));
            Assert.That(info.Subject, Is.EqualTo("Test subject"));
            Assert.That(info.TrackNumber, Is.EqualTo("7"));
        }

        [Test]
        public void IndexerReturnsValueForKnownId()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Hello");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info["INAM"], Is.EqualTo("Hello"));
        }

        [Test]
        public void IndexerReturnsNullForUnknownId()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Hello");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info["ZZZZ"], Is.Null);
        }

        [Test]
        public void IndexerIsCaseInsensitive()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Hello");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info["inam"], Is.EqualTo("Hello"));
            Assert.That(info["InAm"], Is.EqualTo("Hello"));
        }

        [Test]
        public void IndexerReturnsNullOnNullId()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Hello");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info[null], Is.Null);
        }

        [Test]
        public void ContainsReportsPresence()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "Hello");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info.Contains("INAM"), Is.True);
            Assert.That(info.Contains("inam"), Is.True);
            Assert.That(info.Contains("IART"), Is.False);
            Assert.That(info.Contains(null), Is.False);
        }

        [Test]
        public void CountReflectsNumberOfEntries()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "A");
            source.Set("IART", "B");
            source.Set("ICMT", "C");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info.Count, Is.EqualTo(3));
        }

        [Test]
        public void EnumerationYieldsAllEntries()
        {
            var source = new InfoMetadata();
            source.Set("INAM", "A");
            source.Set("IART", "B");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            var pairs = info.ToDictionary(kv => kv.Key, kv => kv.Value);
            Assert.That(pairs["INAM"], Is.EqualTo("A"));
            Assert.That(pairs["IART"], Is.EqualTo("B"));
        }

        [Test]
        public void HandlesOddLengthValuesWithWordAlignmentPadding()
        {
            // An odd-length payload forces a pad byte; the parser must skip it correctly.
            // Values of length 3, 4, 5 produce sizes (with null-terminator) of 4, 5, 6 —
            // so the middle one triggers word-alignment padding.
            var source = new InfoMetadata();
            source.Set("INAM", "abc");
            source.Set("IART", "abcd");
            source.Set("ICMT", "later");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info.Title, Is.EqualTo("abc"));
            Assert.That(info.Artist, Is.EqualTo("abcd"));
            Assert.That(info.Comments, Is.EqualTo("later"));
        }

        [Test]
        public void RoundTripsValuesWithNonAsciiCharacters()
        {
            // INFO values are written and read as UTF-8, so arbitrary Unicode must round-trip.
            var source = new InfoMetadata();
            source.Set("INAM", "Björk");
            source.Set("IART", "Ω-section");
            source.Set("ICMT", "音楽 — fin");
            using var reader = OpenWithInfo(source);
            var info = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(info.Title, Is.EqualTo("Björk"));
            Assert.That(info.Artist, Is.EqualTo("Ω-section"));
            Assert.That(info.Comments, Is.EqualTo("音楽 — fin"));
        }

        [Test]
        public void PicksInfoListWhenAdtlAndInfoCoexist()
        {
            var info = new InfoMetadata();
            info.Set("INAM", "Song");

            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddCue(1, "CueLabel");
                w.WriteInfoMetadata(info);
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            var parsed = reader.Chunks.Read(InfoListInterpreter.Instance);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Title, Is.EqualTo("Song"));
        }
    }
}
