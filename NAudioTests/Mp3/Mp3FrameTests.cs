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

        private readonly byte[] validMp3FrameHeader =
        {
            0xff,
            0xe0 + ((int)MpegVersion.Version2 << 3) + ((int)MpegLayer.Layer3 << 1) + CrcNotPresent,
            BitRateIndex << 4,
            0x00
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

        private static byte[] ConstructMp3Header(
            MpegVersion version,
            MpegLayer layer,
            int bitRateIndex,
            int sampleRateIndex,
            bool crcPresent,
            ChannelMode channelMode,
            int channelExtension,
            int emphasis)
        {
            return new[]
            {
                (byte)0xFF,
                (byte)(0xE0 | (((int)version & 0x03) << 3) | (((int)layer & 0x03) << 1) | (crcPresent ? 0 : 1)),
                (byte)(((bitRateIndex & 0x0F) << 4) | ((sampleRateIndex & 0x03) << 2)),
                (byte)((((int)channelMode & 0x03) << 6) | ((channelExtension & 0x03) << 4) | (emphasis & 0x03))
            };
        }

        private static byte[] ConstructId3v2Header(int payloadLength, byte flags = 0x00)
        {
            return new[]
            {
                (byte)'I', (byte)'D', (byte)'3',
                (byte)0x04, (byte)0x00,
                flags,
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

        [Test]
        public void ParsesExpectedPropertiesFromValidFrame()
        {
            using (var ms = new MemoryStream(ConstructConsecutiveValidMp3Frames(3)))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.MpegVersion, Is.EqualTo(MpegVersion.Version2));
                Assert.That(frame.MpegLayer, Is.EqualTo(MpegLayer.Layer3));
                Assert.That(frame.CrcPresent, Is.False);
                Assert.That(frame.BitRateIndex, Is.EqualTo(BitRateIndex));
                Assert.That(frame.BitRate, Is.EqualTo(8000));
                Assert.That(frame.SampleRate, Is.EqualTo(22050));
                Assert.That(frame.SampleCount, Is.EqualTo(576));
                Assert.That(frame.FrameLength, Is.EqualTo(ValidFrameLength));
                Assert.That(frame.ChannelMode, Is.EqualTo(ChannelMode.Stereo));
                Assert.That(frame.ChannelExtension, Is.EqualTo(0));
                Assert.That(frame.Copyright, Is.False);
                Assert.That(frame.FileOffset, Is.EqualTo(0));
                Assert.That(frame.RawData, Is.Not.Null);
                Assert.That(frame.RawData.Length, Is.EqualTo(ValidFrameLength));
                Assert.That(frame.RawData[0], Is.EqualTo(validMp3FrameHeader[0]));
                Assert.That(frame.RawData[1], Is.EqualTo(validMp3FrameHeader[1]));
                Assert.That(frame.RawData[2], Is.EqualTo(validMp3FrameHeader[2]));
                Assert.That(frame.RawData[3], Is.EqualTo(validMp3FrameHeader[3]));
            }
        }

        [Test]
        public void ReadDataFalseSkipsBodyAndLeavesRawDataNull()
        {
            using (var ms = new MemoryStream(ConstructConsecutiveValidMp3Frames(1)))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms, false);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.RawData, Is.Null);
                Assert.That(ms.Position, Is.EqualTo(frame.FileOffset + frame.FrameLength));
            }
        }

        [Test]
        public void ReadDataFalseOnNonSeekableStreamThrows()
        {
            using (var stream = new NonSeekableReadStream(ConstructConsecutiveValidMp3Frames(3)))
            {
                Assert.Throws<NotSupportedException>(() => Mp3Frame.LoadFromStream(stream, false));
            }
        }

        [Test]
        public void TruncatedFrameThrowsEndOfStreamException()
        {
            using (var ms = new MemoryStream(validMp3FrameHeader))
            {
                Assert.Throws<EndOfStreamException>(() => Mp3Frame.LoadFromStream(ms));
            }
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

        [Test]
        public void RejectsFrameWithReservedMpegVersion()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Reserved, MpegLayer.Layer3, 1, 0, false, ChannelMode.Stereo, 0, 0);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void RejectsFrameWithReservedLayer()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Reserved, 1, 0, false, ChannelMode.Stereo, 0, 0);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void RejectsFrameWithInvalidBitRateIndex()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Layer3, 15, 0, false, ChannelMode.Stereo, 0, 0);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void RejectsFrameWithReservedSampleRateIndex()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Layer3, 1, 3, false, ChannelMode.Stereo, 0, 0);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void RejectsFrameWithInvalidEmphasis()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Layer3, 1, 0, false, ChannelMode.Stereo, 0, 2);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void RejectsFrameWithChannelExtensionOutsideJointStereo()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Layer3, 1, 0, false, ChannelMode.Stereo, 1, 0);
            using (var ms = new MemoryStream(header))
            {
                Assert.That(Mp3Frame.LoadFromStream(ms), Is.Null);
            }
        }

        [Test]
        public void AcceptsFrameWithChannelExtensionInJointStereo()
        {
            byte[] header = ConstructMp3Header(MpegVersion.Version2, MpegLayer.Layer3, 1, 0, false, ChannelMode.JointStereo, 2, 0);
            using (var ms = new MemoryStream(header))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms, false);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.ChannelMode, Is.EqualTo(ChannelMode.JointStereo));
                Assert.That(frame.ChannelExtension, Is.EqualTo(2));
            }
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
        public void SkipsId3v2TagWithFooterAndFindsFirstAudioFrame()
        {
            byte[] fakeId3Payload = new byte[80];
            byte[] id3Header = ConstructId3v2Header(fakeId3Payload.Length, 0x10);
            byte[] id3Footer = new byte[10];
            byte[] audioFrames = ConstructConsecutiveValidMp3Frames(3);

            byte[] data = new byte[id3Header.Length + fakeId3Payload.Length + id3Footer.Length + audioFrames.Length];
            Array.Copy(id3Header, 0, data, 0, id3Header.Length);
            Array.Copy(fakeId3Payload, 0, data, id3Header.Length, fakeId3Payload.Length);
            Array.Copy(id3Footer, 0, data, id3Header.Length + fakeId3Payload.Length, id3Footer.Length);
            Array.Copy(audioFrames, 0, data, id3Header.Length + fakeId3Payload.Length + id3Footer.Length, audioFrames.Length);

            using (var ms = new MemoryStream(data))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.FileOffset, Is.EqualTo(id3Header.Length + fakeId3Payload.Length + id3Footer.Length));
            }
        }

        [Test]
        public void DoesNotSkipId3HeaderWithInvalidSynchsafeSize()
        {
            byte[] invalidId3Header = ConstructId3v2Header(0);
            invalidId3Header[6] = 0x80;
            byte[] audioFrames = ConstructConsecutiveValidMp3Frames(3);
            byte[] data = new byte[invalidId3Header.Length + audioFrames.Length];
            Array.Copy(invalidId3Header, 0, data, 0, invalidId3Header.Length);
            Array.Copy(audioFrames, 0, data, invalidId3Header.Length, audioFrames.Length);

            using (var ms = new MemoryStream(data))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(ms);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.FileOffset, Is.EqualTo(invalidId3Header.Length));
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

        [Test]
        public void NonSeekableStreamSkipsSequenceValidation()
        {
            byte[] falsePositive = new byte[ValidFrameLength];
            Array.Copy(validMp3FrameHeader, falsePositive, validMp3FrameHeader.Length);

            byte[] filler = new byte[60];
            byte[] realFrames = ConstructConsecutiveValidMp3Frames(3);
            byte[] data = new byte[falsePositive.Length + filler.Length + realFrames.Length];
            Array.Copy(falsePositive, 0, data, 0, falsePositive.Length);
            Array.Copy(filler, 0, data, falsePositive.Length, filler.Length);
            Array.Copy(realFrames, 0, data, falsePositive.Length + filler.Length, realFrames.Length);

            using (var stream = new NonSeekableReadStream(data))
            {
                Mp3Frame frame = Mp3Frame.LoadFromStream(stream);
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.FileOffset, Is.EqualTo(0));
            }
        }

        private sealed class NonSeekableReadStream : Stream
        {
            private readonly MemoryStream source;

            public NonSeekableReadStream(byte[] data)
            {
                source = new MemoryStream(data);
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => source.Length;

            public override long Position
            {
                get => source.Position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return source.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    source.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
