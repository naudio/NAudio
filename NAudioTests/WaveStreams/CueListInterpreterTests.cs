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
    public class CueListInterpreterTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);

        // Writes a WAV with the given cue+label pairs via the real writer, then opens it for read.
        private static WaveFileReader OpenWithCues(params (int pos, string label)[] cues)
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                foreach (var (pos, label) in cues) w.AddCue(pos, label);
                w.WriteSamples(new short[] { 1, 2, 3, 4 }, 0, 4);
            }
            ms.Position = 0;
            return new WaveFileReader(ms);
        }

        [Test]
        public void InstanceIsSingleton()
        {
            Assert.That(CueListInterpreter.Instance, Is.Not.Null);
            Assert.That(CueListInterpreter.Instance, Is.SameAs(CueListInterpreter.Instance));
        }

        [Test]
        public void ReturnsNullWhenNoCueChunk()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.WriteSamples(new short[] { 1, 2 }, 0, 2);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Read(CueListInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ReturnsNullWhenCuePresentButNoLabels()
        {
            // Orphan "cue " chunk (no companion LIST/adtl) — use AddChunk at the byte level.
            var cueBytes = BuildOrphanCueChunkBody(cueId: 1, samplePosition: 100);
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
                w.WriteSamples(new short[] { 1, 2 }, 0, 2);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Read(CueListInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ReturnsNullWhenOnlyInfoListIsPresent()
        {
            // cue chunk + LIST/INFO (no adtl) — the interpreter must not mistake INFO for adtl.
            var cueBytes = BuildOrphanCueChunkBody(cueId: 1, samplePosition: 100);
            var info = new InfoMetadata();
            info.Set("INAM", "Title");

            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddChunk("cue ", cueBytes, ChunkPosition.AfterData);
                w.WriteInfoMetadata(info);
                w.WriteSamples(new short[] { 1, 2 }, 0, 2);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Read(CueListInterpreter.Instance), Is.Null);
        }

        [Test]
        public void ReadsCueListFromCueAndAdtlChunks()
        {
            using var reader = OpenWithCues((100, "Intro"), (200, "Verse"));
            var cues = reader.Chunks.Read(CueListInterpreter.Instance);
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Position, Is.EqualTo(100));
            Assert.That(cues[0].Label, Is.EqualTo("Intro"));
            Assert.That(cues[1].Position, Is.EqualTo(200));
            Assert.That(cues[1].Label, Is.EqualTo("Verse"));
        }

        [Test]
        public void PicksAdtlListWhenInfoAndAdtlCoexist()
        {
            // When a file has both INFO metadata and adtl cue labels the interpreter must still
            // find the adtl list.
            var info = new InfoMetadata();
            info.Set("INAM", "Title");

            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddCue(1000, "TheLabel");
                w.WriteInfoMetadata(info);
                w.WriteSamples(new short[] { 1, 2 }, 0, 2);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            var cues = reader.Chunks.Read(CueListInterpreter.Instance);
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(1));
            Assert.That(cues[0].Label, Is.EqualTo("TheLabel"));
        }

        [Test]
        public void AcceptsLowercaseListChunkId()
        {
            // RIFF treats LIST/list as the same chunk; our writer emits "LIST" but readers
            // must accept "list" too. Hand-built to exercise that.
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddCue(42, "lower");
                w.WriteSamples(new short[] { 1, 2 }, 0, 2);
            }
            // rewrite the LIST id in place to lowercase
            var bytes = ms.ToArray();
            FourCcReplace(bytes, "LIST", "list");
            using var reader = new WaveFileReader(new MemoryStream(bytes));
            var cues = reader.Chunks.Read(CueListInterpreter.Instance);
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues[0].Label, Is.EqualTo("lower"));
        }

        [Test]
        public void RoundTripsLabelsWithNonAsciiCharacters()
        {
            using var reader = OpenWithCues(
                (100, "Björk"),
                (200, "Ω-section"),
                (300, "音楽"));
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues[0].Label, Is.EqualTo("Björk"));
            Assert.That(cues[1].Label, Is.EqualTo("Ω-section"));
            Assert.That(cues[2].Label, Is.EqualTo("音楽"));
        }

        [Test]
        public void RoundTripsViaWaveFileWriter()
        {
            using var reader = OpenWithCues((1000, "Alpha"), (2000, "Beta"));
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Position, Is.EqualTo(1000));
            Assert.That(cues[0].Label, Is.EqualTo("Alpha"));
            Assert.That(cues[1].Position, Is.EqualTo(2000));
            Assert.That(cues[1].Label, Is.EqualTo("Beta"));
        }

        // ---- helpers ------------------------------------------------------------

        /// <summary>
        /// Minimal "cue " chunk body: 1 cue point, no label. Used to exercise the "no adtl"
        /// branch of the interpreter (the real writer always emits matching adtl).
        /// </summary>
        private static byte[] BuildOrphanCueChunkBody(int cueId, int samplePosition)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(1);                                 // point count
            w.Write(cueId);                             // dwIdentifier
            w.Write(samplePosition);                    // dwPosition
            w.Write(ChunkIdentifier.ChunkIdentifierToInt32("data")); // fccChunk
            w.Write(0);                                 // dwChunkStart
            w.Write(0);                                 // dwBlockStart
            w.Write(samplePosition);                    // dwSampleOffset
            return ms.ToArray();
        }

        private static void FourCcReplace(byte[] bytes, string from, string to)
        {
            if (from.Length != 4 || to.Length != 4) throw new ArgumentException("FourCC must be 4 chars");
            for (int i = 0; i <= bytes.Length - 4; i++)
            {
                if (bytes[i] == from[0] && bytes[i + 1] == from[1] && bytes[i + 2] == from[2] && bytes[i + 3] == from[3])
                {
                    bytes[i] = (byte)to[0];
                    bytes[i + 1] = (byte)to[1];
                    bytes[i + 2] = (byte)to[2];
                    bytes[i + 3] = (byte)to[3];
                    return;
                }
            }
            throw new InvalidOperationException($"Marker '{from}' not found");
        }
    }
}
