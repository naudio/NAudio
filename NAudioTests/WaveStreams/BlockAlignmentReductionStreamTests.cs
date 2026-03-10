using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudioTests.Utils;
using NAudio.Wave;

namespace NAudioTests.WaveStreams
{
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

        private void CheckReadBuffer(byte[] readBuffer, int count, int startPosition)
        {
            for (int n = 0; n < count; n++)
            {
                byte expected = (byte)((startPosition + n) % 256);
                Assert.That(readBuffer[n], Is.EqualTo(expected), $"Read buffer at position {startPosition + n}");
            }
        }
    }
}
