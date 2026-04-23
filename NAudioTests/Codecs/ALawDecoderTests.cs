using System;
using NAudio.Codecs;
using NUnit.Framework;

namespace NAudioTests.Codecs
{
    [TestFixture]
    [Category("UnitTest")]
    public class ALawDecoderTests
    {
        [Test]
        public void BatchDecodeMatchesSingleSampleDecode()
        {
            // Every one of the 256 a-law byte values, in a mixed order, to exercise the whole table.
            var source = new byte[256];
            for (int i = 0; i < 256; i++) source[i] = (byte)((i * 53 + 11) & 0xFF);

            var batch = new short[source.Length];
            ALawDecoder.Decode(source, batch);

            var expected = new short[source.Length];
            for (int i = 0; i < source.Length; i++)
                expected[i] = ALawDecoder.ALawToLinearSample(source[i]);

            Assert.That(batch, Is.EqualTo(expected),
                "batch Decode must produce byte-identical output to the single-sample form");
        }

        [Test]
        public void EncodeDecodeRoundTripsWithinCodecRange()
        {
            // a-law is lossy, so exact equality doesn't hold — but re-encoding the decoded sample
            // must yield the same a-law byte we started with.
            var source = new byte[256];
            for (int i = 0; i < 256; i++) source[i] = (byte)i;

            var decoded = new short[256];
            ALawDecoder.Decode(source, decoded);

            for (int i = 0; i < 256; i++)
            {
                byte reEncoded = ALawEncoder.LinearToALawSample(decoded[i]);
                Assert.That(reEncoded, Is.EqualTo(source[i]),
                    $"byte {i:x2} did not round-trip through Decode+Encode");
            }
        }

        [Test]
        public void DestinationShorterThanSourceThrows()
        {
            var source = new byte[100];
            var destination = new short[50];
            Assert.Throws<ArgumentException>(() => ALawDecoder.Decode(source, destination));
        }

        [Test]
        public void LargerDestinationIsAllowed()
        {
            // Only source.Length samples should be written — trailing slots remain untouched.
            var source = new byte[] { 0xAA, 0x55, 0xFF };
            var destination = new short[5];
            destination[3] = 123;
            destination[4] = 456;

            ALawDecoder.Decode(source, destination);

            Assert.That(destination[0], Is.EqualTo(ALawDecoder.ALawToLinearSample(0xAA)));
            Assert.That(destination[1], Is.EqualTo(ALawDecoder.ALawToLinearSample(0x55)));
            Assert.That(destination[2], Is.EqualTo(ALawDecoder.ALawToLinearSample(0xFF)));
            Assert.That(destination[3], Is.EqualTo((short)123), "must not write past source.Length");
            Assert.That(destination[4], Is.EqualTo((short)456), "must not write past source.Length");
        }

        [Test]
        public void EmptySourceIsNoOp()
        {
            var destination = new short[] { 1, 2, 3 };
            ALawDecoder.Decode(ReadOnlySpan<byte>.Empty, destination);
            Assert.That(destination, Is.EqualTo(new short[] { 1, 2, 3 }));
        }
    }
}
