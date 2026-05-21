using System;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Utils;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileWriterChunkTests
    {
        private static WaveFormat Format => new WaveFormat(8000, 16, 1);
        private static byte[] Audio => new byte[] { 1, 0, 2, 0, 3, 0, 4, 0 };

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

        // --- AddChunk(string, byte[], ChunkPosition) --------------------------------

        [Test]
        public void AddChunkAfterDataSurfacesInReader()
        {
            using var reader = WriteThenRead(w =>
            {
                w.Write(Audio, 0, Audio.Length);
                w.AddChunk("mark", new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, ChunkPosition.AfterData);
            });
            var mark = reader.Chunks.Find("mark");
            Assert.That(mark, Is.Not.Null);
            Assert.That(reader.Chunks.GetData(mark), Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
        }

        [Test]
        public void AddChunkBeforeDataSurfacesInReader()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk("bfor", new byte[] { 1, 2, 3, 4 }, ChunkPosition.BeforeData);
                w.Write(Audio, 0, Audio.Length);
            });
            var chunk = reader.Chunks.Find("bfor");
            Assert.That(chunk, Is.Not.Null);
            Assert.That(reader.Chunks.GetData(chunk), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void AddChunkBeforeDataIsWrittenBeforeDataChunk()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddChunk("bfor", new byte[] { 1, 2, 3, 4 }, ChunkPosition.BeforeData);
                w.Write(Audio, 0, Audio.Length);
            }
            var bytes = ms.ToArray();
            var fileText = Encoding.ASCII.GetString(bytes);
            int bforIndex = fileText.IndexOf("bfor", StringComparison.Ordinal);
            int dataIndex = fileText.IndexOf("data", StringComparison.Ordinal);
            Assert.That(bforIndex, Is.GreaterThan(0));
            Assert.That(dataIndex, Is.GreaterThan(0));
            Assert.That(bforIndex, Is.LessThan(dataIndex));
        }

        [Test]
        public void AddChunkAfterDataIsWrittenAfterDataChunk()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.Write(Audio, 0, Audio.Length);
                w.AddChunk("aftr", new byte[] { 1, 2, 3, 4 }, ChunkPosition.AfterData);
            }
            var bytes = ms.ToArray();
            var fileText = Encoding.ASCII.GetString(bytes);
            int aftrIndex = fileText.IndexOf("aftr", StringComparison.Ordinal);
            int dataIndex = fileText.IndexOf("data", StringComparison.Ordinal);
            Assert.That(aftrIndex, Is.GreaterThan(dataIndex));
        }

        [Test]
        public void AddChunkBeforeDataAfterWriteThrows()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            w.Write(Audio, 0, Audio.Length);
            Assert.That(() => w.AddChunk("late", new byte[] { 1 }, ChunkPosition.BeforeData),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void AddChunkAfterDataAfterWriteWorks()
        {
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.Write(Audio, 0, Audio.Length);
                // Still allowed — after-data chunks are buffered until close.
                w.AddChunk("okok", new byte[] { 9, 9 }, ChunkPosition.AfterData);
            }
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            Assert.That(reader.Chunks.Contains("okok"), Is.True);
        }

        [Test]
        public void AddChunkWithOddLengthDataIsWordAligned()
        {
            using var reader = WriteThenRead(w =>
            {
                // 3-byte data + 2-byte data: after the 3-byte chunk, a pad byte must be inserted
                // so the 5-byte chunk's header is word-aligned.
                w.AddChunk("odd1", new byte[] { 1, 2, 3 }, ChunkPosition.AfterData);
                w.AddChunk("even", new byte[] { 4, 5 }, ChunkPosition.AfterData);
                w.Write(Audio, 0, Audio.Length);
            });
            var odd1 = reader.Chunks.Find("odd1");
            var even = reader.Chunks.Find("even");
            Assert.That(reader.Chunks.GetData(odd1), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(reader.Chunks.GetData(even), Is.EqualTo(new byte[] { 4, 5 }));
        }

        [Test]
        public void AddChunkPreservesOrderWithinPosition()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk("one ", new byte[] { 1 }, ChunkPosition.AfterData);
                w.AddChunk("two ", new byte[] { 2 }, ChunkPosition.AfterData);
                w.AddChunk("thre", new byte[] { 3 }, ChunkPosition.AfterData);
                w.Write(Audio, 0, Audio.Length);
            });
            var ids = reader.Chunks.Select(c => c.IdentifierAsString).ToList();
            int oneIx = ids.IndexOf("one ");
            int twoIx = ids.IndexOf("two ");
            int threIx = ids.IndexOf("thre");
            Assert.That(oneIx, Is.LessThan(twoIx));
            Assert.That(twoIx, Is.LessThan(threIx));
        }

        [Test]
        public void AddChunkRoundTripsArbitraryBytes()
        {
            var payload = new byte[] { 0x00, 0x7F, 0xFF, 0x55, 0xAA, 0x01 };
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk("blob", payload, ChunkPosition.AfterData);
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(reader.Chunks.GetData(reader.Chunks.Find("blob")), Is.EqualTo(payload));
        }

        [Test]
        public void AddChunkThrowsOnNullId()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            Assert.That(() => w.AddChunk(null, new byte[] { 1 }, ChunkPosition.AfterData),
                Throws.ArgumentNullException);
        }

        [Test]
        public void AddChunkThrowsOnInvalidIdLength()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            Assert.That(() => w.AddChunk("abc", new byte[] { 1 }, ChunkPosition.AfterData),
                Throws.ArgumentException);
            Assert.That(() => w.AddChunk("toolong", new byte[] { 1 }, ChunkPosition.AfterData),
                Throws.ArgumentException);
        }

        [Test]
        public void AddChunkThrowsOnNullData()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            Assert.That(() => w.AddChunk("test", null, ChunkPosition.AfterData),
                Throws.ArgumentNullException);
        }

        [Test]
        public void AddChunkAllowsEmptyData()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk("empt", Array.Empty<byte>(), ChunkPosition.AfterData);
                w.Write(Audio, 0, Audio.Length);
            });
            var empt = reader.Chunks.Find("empt");
            Assert.That(empt, Is.Not.Null);
            Assert.That(empt.Length, Is.EqualTo(0));
        }

        [Test]
        public void AddChunkThrowsAfterDispose()
        {
            var ms = new MemoryStream();
            var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            w.Dispose();
            Assert.That(() => w.AddChunk("test", new byte[] { 1 }, ChunkPosition.AfterData),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        // --- AddChunk(IWaveChunkWriter) --------------------------------------------

        [Test]
        public void AddChunkInterfaceCallsWriteData()
        {
            var writer = new RecordingChunkWriter("test", ChunkPosition.AfterData,
                new byte[] { 7, 8, 9 });
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk(writer);
                w.Write(Audio, 0, Audio.Length);
            });
            Assert.That(writer.Called, Is.True);
            var chunk = reader.Chunks.Find("test");
            Assert.That(reader.Chunks.GetData(chunk), Is.EqualTo(new byte[] { 7, 8, 9 }));
        }

        [Test]
        public void AddChunkInterfaceRespectsBeforeDataPosition()
        {
            var cw = new RecordingChunkWriter("bfor", ChunkPosition.BeforeData,
                new byte[] { 1, 2 });
            var ms = new MemoryStream();
            using (var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format))
            {
                w.AddChunk(cw);
                w.Write(Audio, 0, Audio.Length);
            }
            var fileText = Encoding.ASCII.GetString(ms.ToArray());
            int bforIndex = fileText.IndexOf("bfor", StringComparison.Ordinal);
            int dataIndex = fileText.IndexOf("data", StringComparison.Ordinal);
            Assert.That(bforIndex, Is.LessThan(dataIndex));
        }

        [Test]
        public void AddChunkInterfaceThrowsOnNull()
        {
            var ms = new MemoryStream();
            using var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            Assert.That(() => w.AddChunk((IWaveChunkWriter)null), Throws.ArgumentNullException);
        }

        // --- AddCue ----------------------------------------------------------------

        [Test]
        public void AddCueRoundTripsViaReadCueList()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddCue(100, "Intro");
                w.AddCue(500, "Hook");
                w.Write(Audio, 0, Audio.Length);
            });
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues.Count, Is.EqualTo(2));
            Assert.That(cues[0].Position, Is.EqualTo(100));
            Assert.That(cues[0].Label, Is.EqualTo("Intro"));
            Assert.That(cues[1].Position, Is.EqualTo(500));
            Assert.That(cues[1].Label, Is.EqualTo("Hook"));
        }

        [Test]
        public void AddCueWithNoCallsProducesNoCueChunk()
        {
            using var reader = WriteThenRead(w => w.Write(Audio, 0, Audio.Length));
            Assert.That(reader.Chunks.Contains("cue "), Is.False);
            Assert.That(reader.Chunks.ReadCueList(), Is.Null);
        }

        [Test]
        public void AddCueWithUnicodeLabelsRoundTrips()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddCue(1, "Björk");
                w.AddCue(2, "音楽");
                w.Write(Audio, 0, Audio.Length);
            });
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues[0].Label, Is.EqualTo("Björk"));
            Assert.That(cues[1].Label, Is.EqualTo("音楽"));
        }

        [Test]
        public void AddCueThrowsAfterDispose()
        {
            var ms = new MemoryStream();
            var w = new WaveFileWriter(new IgnoreDisposeStream(ms), Format);
            w.Dispose();
            Assert.That(() => w.AddCue(1, "late"), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void AddCueWorksWithoutAnyAudioWritten()
        {
            using var reader = WriteThenRead(w => w.AddCue(0, "Start"));
            var cues = reader.Chunks.ReadCueList();
            Assert.That(cues, Is.Not.Null);
            Assert.That(cues[0].Label, Is.EqualTo("Start"));
        }

        // --- Mixed usage -----------------------------------------------------------

        [Test]
        public void CanMixBeforeDataAndAfterDataAndAddCue()
        {
            using var reader = WriteThenRead(w =>
            {
                w.AddChunk("pre ", new byte[] { 1 }, ChunkPosition.BeforeData);
                w.AddCue(42, "Marker");
                w.Write(Audio, 0, Audio.Length);
                w.AddChunk("post", new byte[] { 2 }, ChunkPosition.AfterData);
            });
            Assert.That(reader.Chunks.Contains("pre "), Is.True);
            Assert.That(reader.Chunks.Contains("post"), Is.True);
            Assert.That(reader.Chunks.Contains("cue "), Is.True);
            Assert.That(reader.Chunks.ReadCueList().Count, Is.EqualTo(1));
        }

        private sealed class RecordingChunkWriter : IWaveChunkWriter
        {
            private readonly byte[] payload;
            public RecordingChunkWriter(string id, ChunkPosition pos, byte[] payload)
            {
                ChunkId = id;
                Position = pos;
                this.payload = payload;
            }
            public string ChunkId { get; }
            public ChunkPosition Position { get; }
            public bool Called { get; private set; }
            public void WriteData(BinaryWriter writer)
            {
                Called = true;
                writer.Write(payload);
            }
        }
    }
}
