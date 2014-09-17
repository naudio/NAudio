using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using NAudio.Wave;

namespace NAudioTests.Mp3
{
    [TestFixture]
    [Category("UnitTest")]
    public class Mp3FrameTests
    {
        private const int crcNotPresent = 1;
        private const int bitRateIndex = 1;

        private readonly byte[] validMp3FrameHeader = new byte[] { 0xff, 
            (byte)(0xe0 + ((int)MpegVersion.Version2 << 3) + ((int)MpegLayer.Layer3 << 1) + crcNotPresent), 
            (byte)(bitRateIndex << 4), 0x00
        };

        private byte[] constructValidMp3Frame()
        {
            byte[] frame = new byte[52];
            Array.Copy(validMp3FrameHeader, frame, validMp3FrameHeader.Length);
            return frame;
        }

        [Test]
        public void CanParseValidMp3Frame()
        {
            MemoryStream ms = new MemoryStream(constructValidMp3Frame());
            Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
            Assert.IsNotNull(frame);
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(12)]        
        public void FailsToParseInvalidFrame(int length)
        {
            MemoryStream ms = new MemoryStream(new byte[length]);
            Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
            Assert.IsNull(frame);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CanParseMp3FrameOffsetByN(int offset)
        {
            byte[] validMp3Frame = constructValidMp3Frame();
            byte[] offsetBuffer = new byte[offset + validMp3Frame.Length];
            Array.Copy(validMp3Frame, 0, offsetBuffer, offset, validMp3Frame.Length);
            MemoryStream ms = new MemoryStream(offsetBuffer);
            Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
            Assert.IsNotNull(frame);
        }
    }
}
