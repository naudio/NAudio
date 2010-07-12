using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class WaveFileReaderTests
    {
        [Test]
        public void TestEmptyFile()
        {
            // arrange
            var fileContents = new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // "RIFF"
                0x26, 0x00, 0x00, 0x00, // ChunkSize = 38
                0x57, 0x41, 0x56, 0x45, // "WAVE"
                0x66, 0x6d, 0x74, 0x20, // "fmt "
                0x12, 0x00, 0x00, 0x00, // Subchunk1Size = 18
                0x07, 0x00, 0x02, 0x00, // AudioFormat = 7, NumChannels = 2
                0x40, 0x1f, 0x00, 0x00, // SampleRate = 8000
                0x80, 0x3e, 0x00, 0x00, // ByteRate = 16000
                0x02, 0x00, 0x08, 0x00, // BlockAlign = 2, BitsPerSample = 8
                0x00, 0x00,             // ExtraParamSize = 0
                0x64, 0x61, 0x74, 0x61, // "data"
                0x00, 0x00, 0x00, 0x00, // Subchunk2Size = 0
            };
            using (var inputStream = new MemoryStream(fileContents))
            {
                // act
                WaveFormat wv;
                long dataChunkPosition;
                int dataChunkLength;
                var chunks = new List<RiffChunk>();
                WaveFileReader.ReadWaveHeader(inputStream, out wv, out dataChunkPosition, out dataChunkLength, chunks);

                // assert
                Assert.AreEqual(16000, wv.AverageBytesPerSecond);
                Assert.AreEqual(8, wv.BitsPerSample);
                Assert.AreEqual(2, wv.Channels);
                Assert.AreEqual(8000, wv.SampleRate);

                Assert.AreEqual(46, dataChunkPosition);
                Assert.AreEqual(0, dataChunkLength);
                Assert.AreEqual(0, chunks.Count);
            }
        }
    }
}
