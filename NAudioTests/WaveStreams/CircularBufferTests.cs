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
        public void CircularBufferHasMaxLengthAndCount()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            Assert.AreEqual(1024, circularBuffer.MaxLength);
            Assert.AreEqual(0, circularBuffer.Count);
        }

        [Test]
        public void ReadFromEmptyBufferReturnsNothing()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[1024];
            int read = circularBuffer.Read(buffer, 0, 1024);
            Assert.AreEqual(0, read);
        }

        [Test]
        public void CanWriteToBuffer()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            Assert.AreEqual(100, circularBuffer.Count);
            circularBuffer.Write(buffer, 0, 50);
            Assert.AreEqual(150, circularBuffer.Count);
        }

        [Test]
        public void BufferReturnsAsMuchAsIsAvailable()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            byte[] buffer = new byte[100];
            circularBuffer.Write(buffer, 0, 100);
            Assert.AreEqual(100, circularBuffer.Count);
            byte[] readBuffer = new byte[1000];
            int read = circularBuffer.Read(readBuffer, 0, 1000);
            Assert.AreEqual(100, read);
        }

        [Test]
        public void RejectsTooMuchData()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
                
            int written = circularBuffer.Write(buffer, 0, 200);
            Assert.AreEqual(100, written, "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            int written = circularBuffer.Write(buffer, 0, 50);
            Assert.AreEqual(25, written, "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenExactlyFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 100);
            int written = circularBuffer.Write(buffer, 0, 50);
            Assert.AreEqual(0, written, "Wrote the wrong amount");
        }

        [Test]
        public void CanWritePastEnd()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            byte[] buffer = new byte[200];
            circularBuffer.Write(buffer, 0, 75);
            Assert.AreEqual(75, circularBuffer.Count, "Initial count");
            int read = circularBuffer.Read(buffer, 0, 75);
            Assert.AreEqual(0, circularBuffer.Count, "Count after read");
            Assert.AreEqual(75, read, "Bytes read");
            // write wraps round
            circularBuffer.Write(buffer, 0, 50);
            Assert.AreEqual(50, circularBuffer.Count, "Count after wrap round");
            // read wraps round
            read = circularBuffer.Read(buffer, 0, 75);
            Assert.AreEqual(50, read, "Bytes Read 2");
            Assert.AreEqual(0, circularBuffer.Count, "Final Count");
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
            Assert.AreEqual(200, read);
            CheckBuffer(readBuffer, 0, read);
            
            // now write past the end
            circularBuffer.Write(numbers, 0, 200);
            Array.Clear(readBuffer, 0, readBuffer.Length);
            // now read past the end
            read = circularBuffer.Read(readBuffer, 0, 200);
            Assert.AreEqual(200, read);
            CheckBuffer(readBuffer, 0, read);
            
        }

        public void CheckBuffer(byte[] buffer, int startNumber, int length)
        {
            for (int n = 0; n < length; n++)
            {
                Assert.AreEqual(startNumber + n, buffer[n], "Byte mismatch at offset {0}", n);
            }
        }
    }
}
