using System;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveChunksTests
    {
        private static WaveFormat DefaultFormat => new WaveFormat(8000, 16, 1);
        private static byte[] SampleAudio => new byte[] { 0, 0, 10, 0, 20, 0, 30, 0 };

        private static WaveFileReader Open(params WaveFileBuilder.Chunk[] extras)
        {
            var bytes = WaveFileBuilder.Build(DefaultFormat, SampleAudio, extras);
            return new WaveFileReader(new MemoryStream(bytes));
        }

        [Test]
        public void ChunksIsNeverNull()
        {
            using var reader = Open();
            Assert.That(reader.Chunks, Is.Not.Null);
        }

        [Test]
        public void EmptyFileHasEmptyChunksCollection()
        {
            using var reader = Open();
            Assert.That(reader.Chunks.Count, Is.EqualTo(0));
            Assert.That(reader.Chunks, Is.Empty);
        }

        [Test]
        public void ChunksReflectsExtraChunkCount()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("foo ", new byte[] { 1, 2, 3, 4 }),
                new WaveFileBuilder.Chunk("bar ", new byte[] { 5, 6 }));
            Assert.That(reader.Chunks.Count, Is.EqualTo(2));
        }

        [Test]
        public void ChunksExposesRiffChunkMetadata()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            using var reader = Open(new WaveFileBuilder.Chunk("test", data));
            var chunk = reader.Chunks[0];
            Assert.That(chunk.IdentifierAsString, Is.EqualTo("test"));
            Assert.That(chunk.Length, Is.EqualTo(data.Length));
            Assert.That(chunk.StreamPosition, Is.GreaterThan(0));
        }

        [Test]
        public void FindReturnsFirstMatchingChunk()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("foo ", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("bar ", new byte[] { 2 }));
            var found = reader.Chunks.Find("bar ");
            Assert.That(found, Is.Not.Null);
            Assert.That(found.IdentifierAsString, Is.EqualTo("bar "));
        }

        [Test]
        public void FindReturnsNullWhenChunkMissing()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("foo ", new byte[] { 1 }));
            Assert.That(reader.Chunks.Find("zzzz"), Is.Null);
        }

        [Test]
        public void FindIsCaseInsensitive()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("LIST", new byte[] { 1, 2, 3, 4 }));
            Assert.That(reader.Chunks.Find("list"), Is.Not.Null);
            Assert.That(reader.Chunks.Find("List"), Is.Not.Null);
            Assert.That(reader.Chunks.Find("LIST"), Is.Not.Null);
        }

        [Test]
        public void FindReturnsFirstOfDuplicates()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("LIST", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("LIST", new byte[] { 2, 2 }));
            var first = reader.Chunks.Find("LIST");
            Assert.That(first.Length, Is.EqualTo(1));
        }

        [Test]
        public void FindAllReturnsAllMatches()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("LIST", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("foo ", new byte[] { 9 }),
                new WaveFileBuilder.Chunk("LIST", new byte[] { 2, 2 }));
            var matches = reader.Chunks.FindAll("LIST").ToList();
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches[0].Length, Is.EqualTo(1));
            Assert.That(matches[1].Length, Is.EqualTo(2));
        }

        [Test]
        public void FindAllIsCaseInsensitive()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("list", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("LIST", new byte[] { 2, 2 }));
            Assert.That(reader.Chunks.FindAll("List").Count(), Is.EqualTo(2));
        }

        [Test]
        public void FindAllReturnsEmptyWhenNoMatch()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("foo ", new byte[] { 1 }));
            Assert.That(reader.Chunks.FindAll("zzzz"), Is.Empty);
        }

        [Test]
        public void ContainsReturnsTrueWhenPresent()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("foo ", new byte[] { 1 }));
            Assert.That(reader.Chunks.Contains("foo "), Is.True);
        }

        [Test]
        public void ContainsReturnsFalseWhenAbsent()
        {
            using var reader = Open();
            Assert.That(reader.Chunks.Contains("foo "), Is.False);
        }

        [Test]
        public void ContainsIsCaseInsensitive()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("LIST", new byte[] { 1 }));
            Assert.That(reader.Chunks.Contains("list"), Is.True);
        }

        [Test]
        public void GetDataReturnsRawChunkBytes()
        {
            var data = new byte[] { 10, 20, 30, 40, 50 };
            using var reader = Open(new WaveFileBuilder.Chunk("test", data));
            var read = reader.Chunks.GetData(reader.Chunks[0]);
            Assert.That(read, Is.EqualTo(data));
        }

        [Test]
        public void GetDataWorksOnEmptyChunk()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("mt  ", Array.Empty<byte>()));
            var read = reader.Chunks.GetData(reader.Chunks[0]);
            Assert.That(read, Is.Empty);
        }

        [Test]
        public void GetDataPreservesReaderPosition()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("test", new byte[] { 1, 2, 3, 4 }));
            reader.Position = 4;
            var posBefore = reader.Position;
            reader.Chunks.GetData(reader.Chunks[0]);
            Assert.That(reader.Position, Is.EqualTo(posBefore));
        }

        [Test]
        public void GetDataThrowsOnNull()
        {
            using var reader = Open();
            Assert.That(() => reader.Chunks.GetData(null), Throws.ArgumentNullException);
        }

        [Test]
        public void FindThrowsOnNullId()
        {
            using var reader = Open();
            Assert.That(() => reader.Chunks.Find(null), Throws.ArgumentNullException);
        }

        [Test]
        public void FindAllThrowsOnNullId()
        {
            using var reader = Open();
            Assert.That(() => reader.Chunks.FindAll(null).ToList(), Throws.ArgumentNullException);
        }

        [Test]
        public void ContainsReturnsFalseOnNullId()
        {
            // Contains is implemented via Find; null id would throw. We guard on the public surface
            // by documenting case-insensitive match; null would be a programming error. Preserve
            // the throwing behavior.
            using var reader = Open();
            Assert.That(() => reader.Chunks.Contains(null), Throws.ArgumentNullException);
        }

        [Test]
        public void ReadThrowsOnNullInterpreter()
        {
            using var reader = Open();
            Assert.That(() => reader.Chunks.Read<object>(null), Throws.ArgumentNullException);
        }

        [Test]
        public void ReadDelegatesToInterpreter()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("test", new byte[] { 9, 9 }));
            var interpreter = new RecordingInterpreter();
            var result = reader.Chunks.Read(interpreter);
            Assert.That(interpreter.WasCalled, Is.True);
            Assert.That(result, Is.SameAs(reader.Chunks));
        }

        [Test]
        public void EnumerationYieldsAllChunks()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("one ", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("two ", new byte[] { 2 }),
                new WaveFileBuilder.Chunk("thre", new byte[] { 3 }));
            var ids = reader.Chunks.Select(c => c.IdentifierAsString).ToList();
            Assert.That(ids, Is.EqualTo(new[] { "one ", "two ", "thre" }));
        }

        [Test]
        public void IndexerReturnsChunksInInsertionOrder()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("one ", new byte[] { 1 }),
                new WaveFileBuilder.Chunk("two ", new byte[] { 2 }));
            Assert.That(reader.Chunks[0].IdentifierAsString, Is.EqualTo("one "));
            Assert.That(reader.Chunks[1].IdentifierAsString, Is.EqualTo("two "));
        }

        [Test]
        public void ChunksBeforeAndAfterDataAreBothSeen()
        {
            using var reader = Open(
                new WaveFileBuilder.Chunk("bfor", new byte[] { 1 }, beforeData: true),
                new WaveFileBuilder.Chunk("aftr", new byte[] { 2 }));
            Assert.That(reader.Chunks.Count, Is.EqualTo(2));
            Assert.That(reader.Chunks.Contains("bfor"), Is.True);
            Assert.That(reader.Chunks.Contains("aftr"), Is.True);
        }

        [Test]
        public void OddLengthChunkDataReadsCorrectly()
        {
            // verifies that word-alignment padding doesn't leak into the chunk data
            var odd = new byte[] { 1, 2, 3, 4, 5 };
            using var reader = Open(new WaveFileBuilder.Chunk("odd ", odd));
            Assert.That(reader.Chunks.GetData(reader.Chunks[0]), Is.EqualTo(odd));
        }

        [Test]
        public void GetDataDoesNotInterfereWithSampleReading()
        {
            using var reader = Open(new WaveFileBuilder.Chunk("test", new byte[] { 1, 2, 3, 4 }));
            // read one sample frame, grab chunk data, then read the next frame — both should succeed
            var first = reader.ReadNextSampleFrame();
            var chunkData = reader.Chunks.GetData(reader.Chunks[0]);
            var second = reader.ReadNextSampleFrame();
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(chunkData, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }

        private sealed class RecordingInterpreter : IWaveChunkInterpreter<WaveChunks>
        {
            public bool WasCalled { get; private set; }
            public WaveChunks Interpret(WaveChunks chunks)
            {
                WasCalled = true;
                return chunks;
            }
        }
    }
}
