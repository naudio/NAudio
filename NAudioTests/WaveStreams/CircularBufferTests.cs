using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NAudio.Utils;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class CircularBufferTests
    {

        [Test]
        public void PeekingReadsCorrectlyAndDoesntAffectReadPosition()
        {
            CircularBuffer circularBuffer = new CircularBuffer(128);
            for (int i = 0; i < 128; i++)
            {
                circularBuffer.Write(new byte[1] { (byte)i }, 0, 1);
            }

            byte[] peekData = new byte[1];
            circularBuffer.Peek(100, peekData, 0, 1);

            byte[] regularRead = new byte[1];
            circularBuffer.Read(regularRead, 0, 1);

            // Confirm data that was peeked is correct.
            Assert.AreEqual((byte)100, peekData[0]);

            // Confirm data read is from the start as the read position shouldn't have moved.
            Assert.AreEqual((byte)0, regularRead[0]);
        }

        [Test]
        public void CircularBufferHasMaxLengthAndCount()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            Assert.That(circularBuffer.MaxLength, Is.EqualTo(1024));
            Assert.That(circularBuffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReadFromEmptyBufferReturnsNothing()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[1024];
            int read = circularBuffer.Read(buffer, 0, 1024);
            Assert.That(read, Is.EqualTo(0));
        }

        [Test]
        public void CanWriteToBuffer()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            Assert.That(circularBuffer.Count, Is.EqualTo(100));
            circularBuffer.Write(buffer, 0, 50);
            Assert.That(circularBuffer.Count, Is.EqualTo(150));
        }

        [Test]
        public void BufferReturnsAsMuchAsIsAvailable()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            Assert.That(circularBuffer.Count, Is.EqualTo(100));
            byte[] readBuffer = new byte[1000];
            int read = circularBuffer.Read(readBuffer, 0, 1000);
            Assert.That(read, Is.EqualTo(100));
        }

        [Test]
        public void RejectsTooMuchData()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
                
            int written = circularBuffer.Write(buffer, 0, 200);
            Assert.That(written, Is.EqualTo(100), "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            int written = circularBuffer.Write(buffer, 0, 50);
            Assert.That(written, Is.EqualTo(25), "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenExactlyFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 100);
            int written = circularBuffer.Write(buffer, 0, 50);
            Assert.That(written, Is.EqualTo(0), "Wrote the wrong amount");
        }

        [Test]
        public void CanWritePastEnd()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            Assert.That(circularBuffer.Count, Is.EqualTo(75), "Initial count");
            int read = circularBuffer.Read(buffer, 0, 75);
            Assert.That(circularBuffer.Count, Is.EqualTo(0), "Count after read");
            Assert.That(read, Is.EqualTo(75), "Bytes read");
            // write wraps round
            circularBuffer.Write(buffer, 0, 50);
            Assert.That(circularBuffer.Count, Is.EqualTo(50), "Count after wrap round");
            // read wraps round
            read = circularBuffer.Read(buffer, 0, 75);
            Assert.That(read, Is.EqualTo(50), "Bytes Read 2");
            Assert.That(circularBuffer.Count, Is.EqualTo(0), "Final Count");
        }

        [Test]
        public void DataIntegrityTest()
        {
            byte[] numbers = new byte[256];
            byte[] readBuffer = new byte[256];
            for (int n = 0; n < 256; n++)
            {
                numbers[n] = (byte)n;
            }

            CircularBuffer circularBuffer = new CircularBuffer(300);
            circularBuffer.Write(numbers, 0, 200);
            Array.Clear(readBuffer, 0, readBuffer.Length);
            int read = circularBuffer.Read(readBuffer, 0, 200);
            Assert.That(read, Is.EqualTo(200));
            CheckBuffer(readBuffer, 0, read);
            
            // now write past the end
            circularBuffer.Write(numbers, 0, 200);
            Array.Clear(readBuffer, 0, readBuffer.Length);
            // now read past the end
            read = circularBuffer.Read(readBuffer, 0, 200);
            Assert.That(read, Is.EqualTo(200));
            CheckBuffer(readBuffer, 0, read);
            
        }

        public void CheckBuffer(byte[] buffer, int startNumber, int length)
        {
            for (int n = 0; n < length; n++)
            {
                Assert.That(buffer[n], Is.EqualTo(startNumber + n), $"Byte mismatch at offset {n}");
            }
        }
    }
}
