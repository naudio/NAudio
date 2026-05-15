using System;
using System.IO;
using System.Text;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
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
    }
}
