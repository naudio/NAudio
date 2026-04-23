using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveChunksExtensionsTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);
        private static byte[] Audio => new byte[16];

        // Produces a writer backed by a MemoryStream, runs the given action, then returns a
        // WaveFileReader over the resulting bytes.
        private static WaveFileReader WriteThenRead(System.Action<WaveFileWriter> write)
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                write(w);
                w.Write(Audio, 0, Audio.Length);
            }
            ms.Position = 0;
            return new WaveFileReader(ms);
        }

        [Test]
        public void ReadCueListReturnsCueListWhenPresent()
        {
            using var reader = WriteThenRead(w => w.AddCue(100, "X"));
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReadCueListReturnsNullWhenAbsent()
        {
            using var reader = WriteThenRead(_ => { });
            Assert.That(reader.Chunks.ReadCueList(), Is.Null);
        }

        [Test]
        public void ReadBroadcastExtensionReturnsObjectWhenPresent()
        {
            using var reader = WriteThenRead(w => w.WriteBroadcastExtension(new BroadcastExtension
            {
                Description = "D",
                Version = 1
            }));
            var bext = reader.Chunks.ReadBroadcastExtension();
            Assert.That(bext, Is.Not.Null);
            Assert.That(bext.Description, Is.EqualTo("D"));
        }

        [Test]
        public void ReadBroadcastExtensionReturnsNullWhenAbsent()
        {
            using var reader = WriteThenRead(_ => { });
            Assert.That(reader.Chunks.ReadBroadcastExtension(), Is.Null);
        }

        [Test]
        public void ReadInfoMetadataReturnsObjectWhenPresent()
        {
            var info = new InfoMetadata();
            info.Set("INAM", "Title");
            using var reader = WriteThenRead(w => w.WriteInfoMetadata(info));
            var parsed = reader.Chunks.ReadInfoMetadata();
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Title, Is.EqualTo("Title"));
        }

        [Test]
        public void ReadInfoMetadataReturnsNullWhenAbsent()
        {
            using var reader = WriteThenRead(_ => { });
            Assert.That(reader.Chunks.ReadInfoMetadata(), Is.Null);
        }

        [Test]
        public void ExtensionMethodsAreNullSafe()
        {
            // Extension methods against a null receiver should not throw — they short-circuit to null.
            WaveChunks chunks = null;
            Assert.That(chunks.ReadCueList(), Is.Null);
            Assert.That(chunks.ReadBroadcastExtension(), Is.Null);
            Assert.That(chunks.ReadInfoMetadata(), Is.Null);
        }
    }
}
