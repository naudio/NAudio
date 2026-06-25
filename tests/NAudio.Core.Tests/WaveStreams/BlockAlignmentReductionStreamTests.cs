using System;
using NUnit.Framework;
using NAudio.Core.Tests.Utils;
using NAudio.Wave;

namespace NAudio.Core.Tests.WaveStreams;

[TestFixture]
[Category("UnitTest")]
public class BlockAlignmentReductionStreamTests
{
    [Test]
    public void CanCreateBlockAlignmentReductionStream()
    {
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);
        Assert.That(inputStream.BlockAlign, Is.EqualTo(726));
        Assert.That(blockStream.BlockAlign, Is.EqualTo(2));
    }

    [Test]
    public void CanReadNonBlockAlignedLengths()
    {
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);

        byte[] inputBuffer = new byte[1024];
        int read = blockStream.Read(inputBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 1");
        Assert.That(blockStream.Position, Is.EqualTo(1024));
        CheckReadBuffer(inputBuffer, 1024, 0);

        read = blockStream.Read(inputBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 2");
        Assert.That(blockStream.Position, Is.EqualTo(2048), "position 2");
        CheckReadBuffer(inputBuffer, 1024, 1024);
    }

    [Test]
    public void CanRepositionToNonBlockAlignedPositions()
    {
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);

        byte[] inputBuffer = new byte[1024];
        int read = blockStream.Read(inputBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 1");
        Assert.That(blockStream.Position, Is.EqualTo(1024));
        CheckReadBuffer(inputBuffer, 1024, 0);

        read = blockStream.Read(inputBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 2");
        Assert.That(blockStream.Position, Is.EqualTo(2048), "position 2");
        CheckReadBuffer(inputBuffer, 1024, 1024);

        // can reposition correctly
        blockStream.Position = 1000;
        read = blockStream.Read(inputBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 3");
        Assert.That(blockStream.Position, Is.EqualTo(2024), "position 3");
        CheckReadBuffer(inputBuffer, 1024, 1000);
    }

    [Test]
    public void CanRepositionAfterNonBlockAlignedRead()
    {
        // Reproduces #368: an arbitrary-length read (the whole point of
        // this helper) leaves the internal position non-block-aligned.
        // A subsequent valid, block-aligned reposition must not throw
        // "Position must be block aligned" - the setter must validate
        // the incoming value, not the stale current position.
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);

        byte[] inputBuffer = new byte[1023]; // odd -> position becomes non-block-aligned
        int read = blockStream.Read(inputBuffer, 0, 1023);
        Assert.That(read, Is.EqualTo(1023), "bytes read 1");
        Assert.That(blockStream.Position, Is.EqualTo(1023), "position 1");
        CheckReadBuffer(inputBuffer, 1023, 0);

        // 2048 is a multiple of BlockAlign (2) so this must succeed even
        // though the current position (1023) is not block aligned.
        Assert.DoesNotThrow(() => blockStream.Position = 2048);
        Assert.That(blockStream.Position, Is.EqualTo(2048), "position 2");

        byte[] readBuffer = new byte[1024];
        read = blockStream.Read(readBuffer, 0, 1024);
        Assert.That(read, Is.EqualTo(1024), "bytes read 2");
        Assert.That(blockStream.Position, Is.EqualTo(3072), "position 3");
        CheckReadBuffer(readBuffer, 1024, 2048);
    }

    [Test]
    public void ReadLargerThanBufferReturnsAllData()
    {
        // Reproduces #1022: a single read (or CopyTo chunk) larger than the
        // internal circular buffer - sized at WaveFormat.AverageBytesPerSecond * 4 -
        // used to truncate the stream. The fill loop asked the source for the whole
        // request at once; CircularBuffer.Write silently dropped everything past its
        // capacity and the loop then drained and discarded the rest of the source.
        // The whole stream must come back regardless of read size.
        const int streamLength = 80000;
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, streamLength);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);

        // 8000 Hz * 2 bytes = 16000 AverageBytesPerSecond -> 64000 byte buffer.
        // Read more than that in one call to exercise the overflow path.
        Assert.That(blockStream.WaveFormat.AverageBytesPerSecond * 4, Is.LessThan(streamLength),
            "test must request more than one buffer's worth in a single read");

        byte[] readBuffer = new byte[streamLength];
        int total = 0;
        int read;
        // Read may legitimately return a partial buffer (capacity-limited); keep
        // reading until the stream is exhausted, exactly as Stream.CopyTo does.
        while (total < streamLength && (read = blockStream.Read(readBuffer, total, streamLength - total)) > 0)
        {
            total += read;
        }

        Assert.That(total, Is.EqualTo(streamLength), "total bytes read");
        Assert.That(blockStream.Position, Is.EqualTo(streamLength), "final position");
        CheckReadBuffer(readBuffer, streamLength, 0);
    }

    [Test]
    public void RepositionToNonBlockAlignedPositionThrows()
    {
        // The setter must reject a non-block-aligned target value.
        // BlockAlign is 2 (16-bit mono) so an odd position is invalid.
        BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
        BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);

        Assert.Throws<ArgumentException>(() => blockStream.Position = 1001);
    }

    private void CheckReadBuffer(byte[] readBuffer, int count, int startPosition)
    {
        for (int n = 0; n < count; n++)
        {
            byte expected = (byte)((startPosition + n) % 256);
            Assert.That(readBuffer[n], Is.EqualTo(expected), $"Read buffer at position {startPosition + n}");
        }
    }
}
