using NAudio.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class ChunkIdentifierTests
    {
        [TestCase("WAVE", 0x45564157)]
        [TestCase("data", 0x61746164)]
        [TestCase("fmt ", 0x20746D66)]
        [TestCase("RF64", 0x34364652)]
        [TestCase("ds64", 0x34367364)]
        [TestCase("labl", 0x6C62616C)]
        [TestCase("cue ", 0x20657563)]
        public void CanConvertChunkIdentifierToInt(string chunkIdentifier, int expected)
        {
            Assert.That(ChunkIdentifier.ChunkIdentifierToInt32(chunkIdentifier), Is.EqualTo(expected));
        }
    }
}
