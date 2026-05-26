using System;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.FileFormats.Wav;
using NAudio.Wave;
using NAudio.Core.Tests.Utils;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams
{
    /// <summary>
    /// Tests for <see cref="NAudio.FileFormats.Wav.WaveFileChunkReader"/>, exercised
    /// through <see cref="WaveFileReader"/> against hand-built RIFF streams.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class WaveFileChunkReaderTests
    {
        private static WaveFormat DefaultFormat => new WaveFormat(8000, 16, 1);

        [Test]
        public void OddLengthChunkFollowedByNonUtf8ByteDoesNotThrow()
        {
            // An odd-length, non-word-aligned chunk whose following bytes begin a UTF-8
            // supplementary character (U+1F600, "F0 9F 98 80"). PeekChar() decodes this to
            // a surrogate pair and throws "The output char buffer is too small..." — the
            // exact failure reported in issue #959. The byte-wise read must not.
            var bytes = BuildMalformedWav(
                audioData: new byte[] { 0, 0, 10, 0, 20, 0, 30, 0 },
                oddChunkId: "Tst1",
                oddChunkData: new byte[] { 1, 2, 3 },
                writePadByte: false,
                trailingChunkId: new byte[] { 0xF0, 0x9F, 0x98, 0x80 });

            WaveFileReader reader = null;
            Assert.That(() => reader = new WaveFileReader(new MemoryStream(bytes)), Throws.Nothing);
            using (reader)
            {
                Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(8000));
                Assert.That(reader.Length, Is.EqualTo(8));
            }
        }

        [Test]
        public void OddLengthFinalChunkAtEndOfStreamDoesNotThrow()
        {
            // The data chunk is odd-length with no trailing pad byte and ends exactly at
            // end of stream. PeekChar() returned -1 here; a naive ReadByte() (PR #828)
            // would throw EndOfStreamException. The EOF guard must keep this working.
            var bytes = BuildMalformedWav(
                audioData: new byte[] { 11, 22, 33 },
                oddChunkId: null,
                oddChunkData: null,
                writePadByte: false,
                trailingChunkId: null);

            WaveFileReader reader = null;
            Assert.That(() => reader = new WaveFileReader(new MemoryStream(bytes)), Throws.Nothing);
            using (reader)
            {
                Assert.That(reader.Length, Is.EqualTo(3));
            }
        }

        [Test]
        public void WellFormedOddLengthChunkStillSkipsPadByte()
        {
            // Behaviour preservation: a word-aligned odd-length chunk (proper 0 pad byte)
            // must still have its pad byte consumed so the following chunk parses cleanly.
            var bytes = WaveFileBuilder.Build(
                DefaultFormat,
                new byte[] { 0, 0, 10, 0 },
                new WaveFileBuilder.Chunk("odd ", new byte[] { 1, 2, 3 }, beforeData: true),
                new WaveFileBuilder.Chunk("aftr", new byte[] { 9, 9, 9, 9 }));

            using var reader = new WaveFileReader(new MemoryStream(bytes));
            Assert.That(reader.Chunks.Contains("odd "), Is.True);
            Assert.That(reader.Chunks.Contains("aftr"), Is.True);
            Assert.That(reader.Chunks.GetData(reader.Chunks.Find("odd ")), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(reader.Chunks.GetData(reader.Chunks.Find("aftr")), Is.EqualTo(new byte[] { 9, 9, 9, 9 }));
        }

        /// <summary>
        /// Hand-rolls a RIFF/WAVE stream that NAudio's own writer won't produce: an
        /// optionally non-word-aligned odd-length chunk followed by arbitrary bytes.
        /// </summary>
        private static byte[] BuildMalformedWav(
            byte[] audioData,
            string oddChunkId,
            byte[] oddChunkData,
            bool writePadByte,
            byte[] trailingChunkId)
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(0); // RIFF size placeholder
                w.Write(Encoding.ASCII.GetBytes("WAVE"));

                w.Write(Encoding.ASCII.GetBytes("fmt "));
                DefaultFormat.Serialize(w);

                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(audioData.Length);
                w.Write(audioData);
                if ((audioData.Length & 1) == 1 && writePadByte) w.Write((byte)0);

                if (oddChunkId != null)
                {
                    w.Write(Encoding.ASCII.GetBytes(oddChunkId));
                    w.Write(oddChunkData.Length);
                    w.Write(oddChunkData);
                    if ((oddChunkData.Length & 1) == 1 && writePadByte) w.Write((byte)0);
                }

                if (trailingChunkId != null)
                {
                    w.Write(trailingChunkId); // 4-byte chunk id
                    w.Write(0);               // zero-length chunk
                }

                long fileLength = ms.Length;
                ms.Position = 4;
                w.Write((uint)(fileLength - 8));
            }

            return ms.ToArray();
        }

        // --- Header / structural validation ---------------------------------

        [Test]
        public void NotARiffOrRf64FileThrowsFormatException()
        {
            var bytes = Riff("JUNK", w => { Fourcc(w, "WAVE"); WriteFmt(w); WriteChunk(w, "data", new byte[] { 0, 0 }); });
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>().With.Message.Contain("RIFF"));
        }

        [Test]
        public void RiffWithoutWaveHeaderThrowsFormatException()
        {
            var bytes = Riff("RIFF", w => { Fourcc(w, "XXXX"); WriteFmt(w); WriteChunk(w, "data", new byte[] { 0, 0 }); });
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>().With.Message.Contain("WAVE"));
        }

        [Test]
        public void MissingFmtChunkThrowsFormatException()
        {
            var bytes = Riff("RIFF", w => { Fourcc(w, "WAVE"); WriteChunk(w, "data", new byte[] { 0, 0, 0, 0 }); });
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>().With.Message.Contain("fmt"));
        }

        [Test]
        public void MissingDataChunkThrowsFormatException()
        {
            var bytes = Riff("RIFF", w => { Fourcc(w, "WAVE"); WriteFmt(w); });
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>().With.Message.Contain("data"));
        }

        [Test]
        public void FmtChunkLengthExceedingInt32ThrowsInvalidDataException()
        {
            var bytes = Riff("RIFF", w =>
            {
                Fourcc(w, "WAVE");
                Fourcc(w, "fmt ");
                w.Write(0xFFFFFFFFu); // declared fmt length > Int32.MaxValue
                Fourcc(w, "data");
                w.Write(0u);
            });
            Assert.That(() => Read(bytes), Throws.TypeOf<InvalidDataException>());
        }

        // --- RF64 ----------------------------------------------------------

        [Test]
        public void Rf64WithoutDs64ChunkThrowsFormatException()
        {
            var bytes = Riff("RF64", w => { Fourcc(w, "WAVE"); WriteChunk(w, "junk", new byte[] { 0, 0, 0, 0 }); },
                riffSizeOverride: 0xFFFFFFFF);
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>().With.Message.Contain("ds64"));
        }

        [Test]
        public void Rf64TakesDataChunkLengthFromDs64NotDataHeader()
        {
            // The data chunk's own size field is the RF64 0xFFFFFFFF sentinel; the real
            // length must come from the ds64 chunk (exercises the `if (!isRf64)` branch).
            var audio = new byte[] { 1, 2, 3, 4, 5, 6 };
            var bytes = Riff("RF64", w =>
            {
                Fourcc(w, "WAVE");
                Fourcc(w, "ds64");
                w.Write(28);                 // ds64 chunk size
                w.Write(1_000_000L);         // riffSize (clamped against stream length)
                w.Write((long)audio.Length); // dataChunkLength
                w.Write(3L);                 // sampleCount
                w.Write(0);                  // table length (chunkSize - 24 == 4 bytes)
                WriteFmt(w);
                Fourcc(w, "data");
                w.Write(0xFFFFFFFFu);        // RF64 sentinel in the data header
                w.Write(audio);
            }, riffSizeOverride: 0xFFFFFFFF);

            var r = Read(bytes);
            Assert.That(r.DataChunkLength, Is.EqualTo(audio.Length));
            Assert.That(r.WaveFormat, Is.Not.Null);
        }

        // --- Corrupt / oversized trailing chunk ----------------------------

        [Test]
        public void OversizedTrailingChunkIsToleratedWhenFmtAndDataAlreadyFound()
        {
            var bytes = Riff("RIFF", w =>
            {
                Fourcc(w, "WAVE");
                WriteFmt(w);
                WriteChunk(w, "data", new byte[] { 1, 2, 3, 4 });
                Fourcc(w, "junk");
                w.Write(0x7FFFFFFEu);          // declared length far beyond what remains
                w.Write(new byte[] { 9, 9 });
            });

            var r = Read(bytes);
            Assert.That(r.WaveFormat, Is.Not.Null);
            Assert.That(r.DataChunkLength, Is.EqualTo(4));
        }

        [Test]
        public void OversizedChunkBeforeFmtAndDataStillThrows()
        {
            var bytes = Riff("RIFF", w =>
            {
                Fourcc(w, "WAVE");
                Fourcc(w, "junk");
                w.Write(0x7FFFFFFEu);          // breaks the loop before fmt/data are seen
                w.Write(new byte[] { 0, 0 });
            });
            Assert.That(() => Read(bytes), Throws.TypeOf<FormatException>());
        }

        // --- riffSize boundary handling ------------------------------------

        [Test]
        public void ExtraBytesBeyondRiffSizeAreIgnored()
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
            {
                Fourcc(w, "RIFF");
                w.Write(0u);
                Fourcc(w, "WAVE");
                WriteFmt(w);
                WriteChunk(w, "data", new byte[] { 1, 2, 3, 4 });
                long covered = ms.Length;                       // RIFF region ends here
                WriteChunk(w, "extr", new byte[] { 7, 7, 7, 7 }); // physically present, but beyond riffSize
                ms.Position = 4;
                w.Write((uint)(covered - 8));
            }

            var r = Read(ms.ToArray());
            Assert.That(r.WaveFormat, Is.Not.Null);
            Assert.That(r.RiffChunks.Any(c => c.IdentifierAsString == "extr"), Is.False);
        }

        [Test]
        public void RiffSizeLargerThanStreamDoesNotReadPastEnd()
        {
            var bytes = Riff("RIFF", w =>
            {
                Fourcc(w, "WAVE");
                WriteFmt(w);
                WriteChunk(w, "data", new byte[] { 1, 2, 3, 4 });
            }, riffSizeOverride: 0xFFFFFF00u);

            var r = Read(bytes);
            Assert.That(r.WaveFormat, Is.Not.Null);
            Assert.That(r.DataChunkLength, Is.EqualTo(4));
        }

        // --- Chunk ordering ------------------------------------------------

        [Test]
        public void FmtChunkAfterDataChunkStillParses()
        {
            var bytes = Riff("RIFF", w =>
            {
                Fourcc(w, "WAVE");
                WriteChunk(w, "data", new byte[] { 1, 2, 3, 4 });
                WriteFmt(w);
            });

            var r = Read(bytes);
            Assert.That(r.WaveFormat, Is.Not.Null);
            Assert.That(r.WaveFormat.SampleRate, Is.EqualTo(8000));
            Assert.That(r.DataChunkLength, Is.EqualTo(4));
            Assert.That(r.DataChunkPosition, Is.GreaterThan(0));
        }

        // --- Builders ------------------------------------------------------

        private static byte[] Riff(string headerFourCc, Action<BinaryWriter> body, uint? riffSizeOverride = null)
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
            {
                Fourcc(w, headerFourCc);
                w.Write(0u); // riff size placeholder at offset 4
                body(w);
                long len = ms.Length;
                ms.Position = 4;
                w.Write(riffSizeOverride ?? (uint)(len - 8));
            }
            return ms.ToArray();
        }

        private static void Fourcc(BinaryWriter w, string id) => w.Write(Encoding.ASCII.GetBytes(id));

        private static void WriteChunk(BinaryWriter w, string id, byte[] data)
        {
            Fourcc(w, id);
            w.Write(data.Length);
            w.Write(data);
        }

        private static void WriteFmt(BinaryWriter w)
        {
            Fourcc(w, "fmt ");
            DefaultFormat.Serialize(w);
        }

        private static WaveFileChunkReader Read(byte[] bytes)
        {
            var reader = new WaveFileChunkReader();
            reader.ReadWaveHeader(new MemoryStream(bytes));
            return reader;
        }
    }
}
