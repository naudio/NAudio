using NAudio.Wave;
using NUnit.Framework;
using System;
using System.IO;

namespace NAudioTests.Mp3
{
    [TestFixture]
    [Category("UnitTest")]
    public class Mp3FrameTests
    {
        private const int CrcNotPresent = 1;
        private const int BitRateIndex = 1;
        private const int ValidFrameLength = 26;

        private readonly byte[] validMp3FrameHeader = { 0xff, 
            0xe0 + ((int)MpegVersion.Version2 << 3) + ((int)MpegLayer.Layer3 << 1) + CrcNotPresent, 
            BitRateIndex << 4, 0x00
        };

        private byte[] ConstructValidMp3Frame()
        {
            byte[] frame = new byte[ValidFrameLength];
            Array.Copy(validMp3FrameHeader, frame, validMp3FrameHeader.Length);
            return frame;
        }

        private byte[] ConstructConsecutiveValidMp3Frames(int count)
        {
            var frame = ConstructValidMp3Frame();
            var allFrames = new byte[frame.Length * count];
            for (int n = 0; n < count; n++)
            {
                Array.Copy(frame, 0, allFrames, n * frame.Length, frame.Length);
            }

            return allFrames;
        }

        private byte[] ConstructId3v2Header(int payloadLength)
        {
            return new[]
            {
                (byte)'I', (byte)'D', (byte)'3',
                (byte)0x04, (byte)0x00,
                (byte)0x00,
                (byte)((payloadLength >> 21) & 0x7F),
                (byte)((payloadLength >> 14) & 0x7F),
                (byte)((payloadLength >> 7) & 0x7F),
                (byte)(payloadLength & 0x7F)
            };
        }

        [Test]
        public void CanParseValidMp3Frame()
        {
            MemoryStream ms = new MemoryStream(ConstructConsecutiveValidMp3Frames(3));
            Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
            Assert.That(frame, Is.Not.Null);
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
            Assert.That(frame, Is.Null);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void CanParseMp3FrameOffsetByN(int offset)
        {
            byte[] validMp3Frames = ConstructConsecutiveValidMp3Frames(3);
            byte[] offsetBuffer = new byte[offset + validMp3Frames.Length];
            Array.Copy(validMp3Frames, 0, offsetBuffer, offset, validMp3Frames.Length);
            MemoryStream ms = new MemoryStream(offsetBuffer);
            Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.FileOffset, Is.EqualTo(offset));
        }

        [Test]
        public void SkipsId3v2TagAndFindsFirstAudioFrame()
        {
            byte[] fakeArtworkData = new byte[120];
            Array.Copy(validMp3FrameHeader, 0, fakeArtworkData, 10, validMp3FrameHeader.Length);
            byte[] id3Header = ConstructId3v2Header(fakeArtworkData.Length);
            byte[] audioFrames = ConstructConsecutiveValidMp3Frames(3);

            byte[] data = new byte[id3Header.Length + fakeArtworkData.Length + audioFrames.Length];
            Array.Copy(id3Header, 0, data, 0, id3Header.Length);
            Array.Copy(fakeArtworkData, 0, data, id3Header.Length, fakeArtworkData.Length);
            Array.Copy(audioFrames, 0, data, id3Header.Length + fakeArtworkData.Length, audioFrames.Length);

            using (var ms = new MemoryStream(data))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.FileOffset, Is.EqualTo(id3Header.Length + fakeArtworkData.Length));
            }
        }

        [Test]
        public void RejectsSingleFalsePositiveHeaderAndFindsRealFrameSequence()
        {
            byte[] falsePositive = new byte[ValidFrameLength];
            Array.Copy(validMp3FrameHeader, falsePositive, validMp3FrameHeader.Length);

            byte[] filler = new byte[60];
            byte[] realFrames = ConstructConsecutiveValidMp3Frames(3);
            byte[] data = new byte[falsePositive.Length + filler.Length + realFrames.Length];
            Array.Copy(falsePositive, 0, data, 0, falsePositive.Length);
            Array.Copy(filler, 0, data, falsePositive.Length, filler.Length);
            Array.Copy(realFrames, 0, data, falsePositive.Length + filler.Length, realFrames.Length);

            using (var ms = new MemoryStream(data))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.FileOffset, Is.EqualTo(falsePositive.Length + filler.Length));
            }
        }
    }
}
