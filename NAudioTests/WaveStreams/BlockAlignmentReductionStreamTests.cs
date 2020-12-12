using NUnit.Framework;
using NAudioTests.Utils;
using NAudio.Wave;
using System;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class BlockAlignmentReductionStreamTests
    {
        [Test]
        public void CanCreateBlockAlignmentReductionStream()
        {
            var inputStream = new BlockAlignedWaveStream(726, 80000);
            var blockStream = new BlockAlignReductionStream(inputStream);
            Assert.AreEqual(726, inputStream.BlockAlign);
            Assert.AreEqual(2, blockStream.BlockAlign);
        }

        [Test]
        public void CanReadNonBlockAlignedLengths()
        {
            var inputStream = new BlockAlignedWaveStream(726, 80000);
            var blockStream = new BlockAlignReductionStream(inputStream);
                        
            byte[] inputBuffer = new byte[1024];
            int read = blockStream.Read(new Span<byte>(inputBuffer));
            Assert.AreEqual(1024, read, "bytes read 1");
            Assert.AreEqual(blockStream.Position, 1024);
            CheckReadBuffer(inputBuffer, 1024, 0);

            read = blockStream.Read(new Span<byte>(inputBuffer));
            Assert.AreEqual(1024, read, "bytes read 2");
            Assert.AreEqual(2048, blockStream.Position, "position 2");
            CheckReadBuffer(inputBuffer, 1024, 1024);
        }

        [Test]
        public void CanRepositionToNonBlockAlignedPositions()
        {
            BlockAlignedWaveStream inputStream = new BlockAlignedWaveStream(726, 80000);
            BlockAlignReductionStream blockStream = new BlockAlignReductionStream(inputStream);


            byte[] inputBuffer = new byte[1024];
            int read = blockStream.Read(new Span<byte>(inputBuffer));
            Assert.AreEqual(1024, read, "bytes read 1");
            Assert.AreEqual(blockStream.Position, 1024);
            CheckReadBuffer(inputBuffer, 1024, 0);

            read = blockStream.Read(new Span<byte>(inputBuffer));
            Assert.AreEqual(1024, read, "bytes read 2");
            Assert.AreEqual(2048, blockStream.Position, "position 2");
            CheckReadBuffer(inputBuffer, 1024, 1024);


            // can reposition correctly
            blockStream.Position = 1000;
            read = blockStream.Read(new Span<byte>(inputBuffer));
            Assert.AreEqual(1024, read, "bytes read 3");
            Assert.AreEqual(2024, blockStream.Position, "position 3");
            CheckReadBuffer(inputBuffer, 1024, 1000);
            
        }

        private void CheckReadBuffer(byte[] readBuffer, int count, int startPosition)
        {
            for (int n = 0; n < count; n++)
            {
                byte expected = (byte)((startPosition + n) % 256);
                Assert.AreEqual(expected, readBuffer[n],"Read buffer at position {0}",startPosition+ n);
            }
        }

    }
}
