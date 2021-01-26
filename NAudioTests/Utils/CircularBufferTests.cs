using System;
using System.Linq;
using NAudio.Utils;
using NUnit.Framework;

namespace NAudioTests.Utils
{
    [TestFixture]
    public class CircularBufferTests
    {
        [Test]
        public void CanReadBufferAfterOneCycle()
        {
            CircularBuffer circularBuffer = new CircularBuffer(3);
            var readBuffer = new byte[1];

            for (byte i = 0; i < circularBuffer.MaxLength; i++)
            {
                var b = new byte[] { i };
                circularBuffer.Write(b, 0, 1);
                circularBuffer.Read(b, 0, 1);
            }

            circularBuffer.Write(new byte[] {9}, 0, 1);
            circularBuffer.Read(readBuffer, 0, 1);

            CollectionAssert.AreEqual(new byte[] { 9 }, readBuffer);
        }


        [Test]
        public void CanBackwardReaderIndex()
        {
            CircularBuffer circularBuffer = new CircularBuffer(3);
            var readBuffer = new byte[circularBuffer.MaxLength];

            for (byte i = 0; i < 5; i++) 
            {
                var b = new byte[] {i};
                circularBuffer.Write(b, 0, 1);
                circularBuffer.Read(b, 0, 1);
            }

            circularBuffer.Advance(-3);
            circularBuffer.Read(readBuffer, 0, readBuffer.Length);

            CollectionAssert.AreEqual(new byte[] {2, 3, 4}, readBuffer);
        }
    }
}
