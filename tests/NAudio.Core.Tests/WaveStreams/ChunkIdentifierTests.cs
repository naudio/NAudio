using System;
using NAudio.Utils;
using NUnit.Framework;

namespace NAudio.Core.Tests.WaveStreams;

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
    [Test]
    public void SpanOverloadMatchesStringOverload()
    {
        Assert.That(ChunkIdentifier.ChunkIdentifierToInt32("WAVE"u8), Is.EqualTo(ChunkIdentifier.ChunkIdentifierToInt32("WAVE")));
    }

    [TestCase("abc")]
    [TestCase("abcde")]
    [TestCase("abc\u00e9")] // four characters, but encodes to five UTF-8 bytes
    public void StringOverloadRejectsIdentifiersThatAreNotFourBytes(string identifier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChunkIdentifier.ChunkIdentifierToInt32(identifier));
    }

    [Test]
    public void StringOverloadRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ChunkIdentifier.ChunkIdentifierToInt32((string)null));
    }

    [Test]
    public void SpanOverloadRejectsIdentifiersThatAreNotFourBytes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ChunkIdentifier.ChunkIdentifierToInt32("abc"u8));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChunkIdentifier.ChunkIdentifierToInt32("abcde"u8));
    }
}
