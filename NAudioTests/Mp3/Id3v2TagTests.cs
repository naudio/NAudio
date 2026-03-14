using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Mp3
{
    [TestFixture]
    [Category("UnitTest")]
    public class Id3v2TagTests
    {
        private static byte[] GetSynchsafeBytes(int value)
        {
            return new[]
            {
                (byte)((value >> 21) & 0x7F),
                (byte)((value >> 14) & 0x7F),
                (byte)((value >> 7) & 0x7F),
                (byte)(value & 0x7F)
            };
        }

        private static byte[] BuildId3v2Header(int payloadLength, byte flags = 0)
        {
            var sizeBytes = GetSynchsafeBytes(payloadLength);
            return new[]
            {
                (byte)'I', (byte)'D', (byte)'3',
                (byte)0x03, (byte)0x00,
                flags,
                sizeBytes[0], sizeBytes[1], sizeBytes[2], sizeBytes[3]
            };
        }

        private static byte[] BuildTag(byte[] payload, byte flags = 0, bool includeFooter = false)
        {
            var header = BuildId3v2Header(payload.Length, flags);
            int footerLength = includeFooter ? 10 : 0;
            var tag = new byte[header.Length + payload.Length + footerLength];
            Array.Copy(header, 0, tag, 0, header.Length);
            Array.Copy(payload, 0, tag, header.Length, payload.Length);
            if (includeFooter)
            {
                for (int n = header.Length + payload.Length; n < tag.Length; n++)
                {
                    tag[n] = 0x55;
                }
            }

            return tag;
        }

        [Test]
        public void ReadTagReturnsNullForNonId3DataAndRestoresPosition()
        {
            var bytes = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
            using (var stream = new MemoryStream(bytes))
            {
                stream.Position = 2;
                var start = stream.Position;

                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Null);
                Assert.That(stream.Position, Is.EqualTo(start));
            }
        }

        [Test]
        public void ReadTagParsesHeaderOnlyTag()
        {
            var data = BuildTag(new byte[0]);
            using (var stream = new MemoryStream(data))
            {
                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Not.Null);
                Assert.That(tag.RawData.Length, Is.EqualTo(10));
                Assert.That(tag.RawData, Is.EqualTo(data));
                Assert.That(stream.Position, Is.EqualTo(10));
            }
        }

        [Test]
        public void ReadTagReadsPayloadBytes()
        {
            byte[] payload = { 1, 2, 3, 4, 5, 6, 7 };
            var data = BuildTag(payload);
            using (var stream = new MemoryStream(data))
            {
                var start = stream.Position;
                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Not.Null);
                Assert.That(tag.RawData.Length, Is.EqualTo(10 + payload.Length));
                Assert.That(tag.RawData, Is.EqualTo(data));
                Assert.That(stream.Position, Is.EqualTo(start + data.Length));
            }
        }

        [Test]
        public void ReadTagRespectsStartOffset()
        {
            byte[] prefix = { 9, 8, 7, 6 };
            byte[] payload = { 1, 2, 3 };
            byte[] tag = BuildTag(payload);
            byte[] buffer = new byte[prefix.Length + tag.Length + 5];
            Array.Copy(prefix, 0, buffer, 0, prefix.Length);
            Array.Copy(tag, 0, buffer, prefix.Length, tag.Length);

            using (var stream = new MemoryStream(buffer))
            {
                stream.Position = prefix.Length;
                Id3v2Tag parsed = Id3v2Tag.ReadTag(stream);

                Assert.That(parsed, Is.Not.Null);
                Assert.That(parsed.RawData, Is.EqualTo(tag));
                Assert.That(stream.Position, Is.EqualTo(prefix.Length + tag.Length));
            }
        }

        [Test]
        public void ReadTagReadsFooterWhenFlagSet()
        {
            byte[] payload = { 1, 2, 3, 4 };
            const byte FooterFlag = 0x10;
            var data = BuildTag(payload, FooterFlag, includeFooter: true);

            using (var stream = new MemoryStream(data))
            {
                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Not.Null);
                Assert.That(tag.RawData.Length, Is.EqualTo(data.Length));
                Assert.That(tag.RawData, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadTagWithTruncatedPayloadReturnsAvailableBytes()
        {
            var header = BuildId3v2Header(20);
            var availablePayload = new byte[] { 1, 2, 3 };
            var data = new byte[header.Length + availablePayload.Length];
            Array.Copy(header, 0, data, 0, header.Length);
            Array.Copy(availablePayload, 0, data, header.Length, availablePayload.Length);

            using (var stream = new MemoryStream(data))
            {
                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Not.Null);
                Assert.That(tag.RawData.Length, Is.EqualTo(data.Length));
                Assert.That(tag.RawData, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadTagWithExtendedHeaderShouldNotConsumeBytesPastTagBoundary()
        {
            const byte ExtendedHeaderFlag = 0x40;
            byte[] payload = { 0, 0, 0, 6, 1, 2, 3, 4, 5, 6 };
            byte[] tag = BuildTag(payload, ExtendedHeaderFlag);
            byte[] trailing = { 0xAA, 0xBB, 0xCC, 0xDD };
            byte[] data = new byte[tag.Length + trailing.Length];
            Array.Copy(tag, 0, data, 0, tag.Length);
            Array.Copy(trailing, 0, data, tag.Length, trailing.Length);

            using (var stream = new MemoryStream(data))
            {
                Id3v2Tag parsed = Id3v2Tag.ReadTag(stream);

                Assert.That(parsed, Is.Not.Null);
                Assert.That(stream.Position, Is.EqualTo(tag.Length));
            }
        }

        [Test]
        public void ReadTagOnNonSeekableStreamThrowsNotSupportedException()
        {
            using (var stream = new NonSeekableReadStream(BuildTag(new byte[0])))
            {
                Assert.Throws<NotSupportedException>(() => Id3v2Tag.ReadTag(stream));
            }
        }

        [Test]
        public void CreateWithNoTagsCreatesHeaderOnlyTag()
        {
            var tags = new KeyValuePair<string, string>[0];

            Id3v2Tag tag = Id3v2Tag.Create(tags);

            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.RawData.Length, Is.EqualTo(10));
            Assert.That(Encoding.ASCII.GetString(tag.RawData, 0, 3), Is.EqualTo("ID3"));
        }

        [Test]
        public void CreateCreatesTagContainingRequestedFrames()
        {
            var tags = new[]
            {
                new KeyValuePair<string, string>("TIT2", "My Title"),
                new KeyValuePair<string, string>("TPE1", "My Artist")
            };

            Id3v2Tag tag = Id3v2Tag.Create(tags);
            string rawText = Encoding.ASCII.GetString(tag.RawData);

            Assert.That(tag, Is.Not.Null);
            Assert.That(rawText.Contains("TIT2"), Is.True);
            Assert.That(rawText.Contains("TPE1"), Is.True);
        }

        [Test]
        public void CreateCreatesCommentFrame()
        {
            var tags = new[]
            {
                new KeyValuePair<string, string>("COMM", "A comment")
            };

            Id3v2Tag tag = Id3v2Tag.Create(tags);
            string rawText = Encoding.ASCII.GetString(tag.RawData);

            Assert.That(tag, Is.Not.Null);
            Assert.That(rawText.Contains("COMM"), Is.True);
        }

        [Test]
        public void CreateThrowsForNullKey()
        {
            var tags = new[]
            {
                new KeyValuePair<string, string>(null, "value")
            };

            Assert.Throws<ArgumentNullException>(() => Id3v2Tag.Create(tags));
        }

        [Test]
        public void CreateThrowsForInvalidKeyLength()
        {
            var tags = new[]
            {
                new KeyValuePair<string, string>("ABC", "value")
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => Id3v2Tag.Create(tags));
        }

        [Test]
        public void CreateThrowsForEmptyValue()
        {
            var tags = new[]
            {
                new KeyValuePair<string, string>("TIT2", "")
            };

            Assert.Throws<ArgumentNullException>(() => Id3v2Tag.Create(tags));
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
                get => throw new NotSupportedException();
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

        [Test]
        public void ReadTagReturnsNullForInvalidSynchsafeSizeAndRestoresPosition()
        {
            byte[] invalidHeader = BuildId3v2Header(0);
            invalidHeader[6] = 0x80;
            using (var stream = new MemoryStream(invalidHeader))
            {
                var start = stream.Position;

                Id3v2Tag tag = Id3v2Tag.ReadTag(stream);

                Assert.That(tag, Is.Null);
                Assert.That(stream.Position, Is.EqualTo(start));
            }
        }

        [Test]
        public void TrySkipTagSkipsValidTagAndFooter()
        {
            byte[] payload = { 1, 2, 3, 4, 5 };
            byte[] tag = BuildTag(payload, 0x10, includeFooter: true);
            byte[] trailing = { 0xAA, 0xBB };
            byte[] data = new byte[tag.Length + trailing.Length];
            Array.Copy(tag, 0, data, 0, tag.Length);
            Array.Copy(trailing, 0, data, tag.Length, trailing.Length);

            using (var stream = new MemoryStream(data))
            {
                bool skipped = Id3v2Tag.TrySkipTag(stream);

                Assert.That(skipped, Is.True);
                Assert.That(stream.Position, Is.EqualTo(tag.Length));
            }
        }

        [Test]
        public void TrySkipTagReturnsFalseForNonTagAndRestoresPosition()
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78, 0x90 };
            using (var stream = new MemoryStream(data))
            {
                stream.Position = 1;
                long start = stream.Position;

                bool skipped = Id3v2Tag.TrySkipTag(stream);

                Assert.That(skipped, Is.False);
                Assert.That(stream.Position, Is.EqualTo(start));
            }
        }

        [Test]
        public void TrySkipTagReturnsFalseForInvalidSynchsafeSizeAndRestoresPosition()
        {
            byte[] invalidHeader = BuildId3v2Header(0);
            invalidHeader[9] = 0x80;
            using (var stream = new MemoryStream(invalidHeader))
            {
                long start = stream.Position;

                bool skipped = Id3v2Tag.TrySkipTag(stream);

                Assert.That(skipped, Is.False);
                Assert.That(stream.Position, Is.EqualTo(start));
            }
        }
    }
}
