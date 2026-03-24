using System;
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
            Assert.That(circularBuffer.MaxLength, Is.EqualTo(1024));
            Assert.That(circularBuffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReadFromEmptyBufferReturnsNothing()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            Span<byte> buffer = stackalloc byte[1024];
            int read = circularBuffer.Read(buffer);
            Assert.That(read, Is.EqualTo(0));
        }

        [Test]
        public void CanWriteToBuffer()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            ReadOnlySpan<byte> buffer = new byte[100];
            circularBuffer.Write(buffer);
            Assert.That(circularBuffer.Count, Is.EqualTo(100));
            circularBuffer.Write(buffer.Slice(0, 50));
            Assert.That(circularBuffer.Count, Is.EqualTo(150));
        }

        [Test]
        public void BufferReturnsAsMuchAsIsAvailable()
        {
            CircularBuffer circularBuffer = new CircularBuffer(1024);
            circularBuffer.Write(new byte[100]);
            Assert.That(circularBuffer.Count, Is.EqualTo(100));
            Span<byte> readBuffer = new byte[1000];
            int read = circularBuffer.Read(readBuffer);
            Assert.That(read, Is.EqualTo(100));
        }

        [Test]
        public void RejectsTooMuchData()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);

            int written = circularBuffer.Write(new byte[200]);
            Assert.That(written, Is.EqualTo(100), "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            circularBuffer.Write(new byte[75]);
            int written = circularBuffer.Write(new byte[50]);
            Assert.That(written, Is.EqualTo(25), "Wrote the wrong amount");
        }

        [Test]
        public void RejectsWhenExactlyFull()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            circularBuffer.Write(new byte[100]);
            int written = circularBuffer.Write(new byte[50]);
            Assert.That(written, Is.EqualTo(0), "Wrote the wrong amount");
        }

        [Test]
        public void CanWritePastEnd()
        {
            CircularBuffer circularBuffer = new CircularBuffer(100);
            circularBuffer.Write(new byte[75]);
            Assert.That(circularBuffer.Count, Is.EqualTo(75), "Initial count");
            Span<byte> readBuffer = new byte[75];
            int read = circularBuffer.Read(readBuffer);
            Assert.That(circularBuffer.Count, Is.EqualTo(0), "Count after read");
            Assert.That(read, Is.EqualTo(75), "Bytes read");
            // write wraps round
            circularBuffer.Write(new byte[50]);
            Assert.That(circularBuffer.Count, Is.EqualTo(50), "Count after wrap round");
            // read wraps round
            read = circularBuffer.Read(readBuffer);
            Assert.That(read, Is.EqualTo(50), "Bytes Read 2");
            Assert.That(circularBuffer.Count, Is.EqualTo(0), "Final Count");
        }

        [Test]
        public void DataIntegrityTest()
        {
            byte[] numbers = new byte[256];
            for (int n = 0; n < 256; n++)
            {
                numbers[n] = (byte)n;
            }

            CircularBuffer circularBuffer = new CircularBuffer(300);
            circularBuffer.Write(numbers.AsSpan(0, 200));
            Span<byte> readBuffer = new byte[256];
            int read = circularBuffer.Read(readBuffer.Slice(0, 200));
            Assert.That(read, Is.EqualTo(200));
            CheckBuffer(readBuffer, 0, read);

            // now write past the end
            circularBuffer.Write(numbers.AsSpan(0, 200));
            readBuffer.Clear();
            // now read past the end
            read = circularBuffer.Read(readBuffer.Slice(0, 200));
            Assert.That(read, Is.EqualTo(200));
            CheckBuffer(readBuffer, 0, read);
        }

        public void CheckBuffer(Span<byte> buffer, int startNumber, int length)
        {
            for (int n = 0; n < length; n++)
            {
                Assert.That(buffer[n], Is.EqualTo(startNumber + n), $"Byte mismatch at offset {n}");
            }
        }
    }
}
