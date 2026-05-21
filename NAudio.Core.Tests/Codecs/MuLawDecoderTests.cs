using System;
using NAudio.Codecs;
using NUnit.Framework;

namespace NAudioTests.Codecs
{
    [TestFixture]
    [Category("UnitTest")]
    public class MuLawDecoderTests
    {
        [Test]
        public void BatchDecodeMatchesSingleSampleDecode()
        {
            var source = new byte[256];
            for (int i = 0; i < 256; i++) source[i] = (byte)((i * 47 + 17) & 0xFF);

            var batch = new short[source.Length];
            MuLawDecoder.Decode(source, batch);

            var expected = new short[source.Length];
            for (int i = 0; i < source.Length; i++)
                expected[i] = MuLawDecoder.MuLawToLinearSample(source[i]);

            Assert.That(batch, Is.EqualTo(expected),
                "batch Decode must produce byte-identical output to the single-sample form");
        }

        [Test]
        public void EncodeDecodeRoundTripsWithinCodecRange()
        {
            var source = new byte[256];
            for (int i = 0; i < 256; i++) source[i] = (byte)i;

            var decoded = new short[256];
            MuLawDecoder.Decode(source, decoded);

            for (int i = 0; i < 256; i++)
            {
                byte reEncoded = MuLawEncoder.LinearToMuLawSample(decoded[i]);
                Assert.That(reEncoded, Is.EqualTo(source[i]),
                    $"byte {i:x2} did not round-trip through Decode+Encode");
            }
        }

        [Test]
        public void DestinationShorterThanSourceThrows()
        {
            var source = new byte[100];
            var destination = new short[50];
            Assert.Throws<ArgumentException>(() => MuLawDecoder.Decode(source, destination));
        }

        [Test]
        public void LargerDestinationIsAllowed()
        {
            var source = new byte[] { 0xAA, 0x55, 0xFF };
            var destination = new short[5];
            destination[3] = 123;
            destination[4] = 456;

            MuLawDecoder.Decode(source, destination);

            Assert.That(destination[0], Is.EqualTo(MuLawDecoder.MuLawToLinearSample(0xAA)));
            Assert.That(destination[1], Is.EqualTo(MuLawDecoder.MuLawToLinearSample(0x55)));
            Assert.That(destination[2], Is.EqualTo(MuLawDecoder.MuLawToLinearSample(0xFF)));
            Assert.That(destination[3], Is.EqualTo((short)123), "must not write past source.Length");
            Assert.That(destination[4], Is.EqualTo((short)456), "must not write past source.Length");
        }

        [Test]
        public void EmptySourceIsNoOp()
        {
            var destination = new short[] { 1, 2, 3 };
            MuLawDecoder.Decode(ReadOnlySpan<byte>.Empty, destination);
            Assert.That(destination, Is.EqualTo(new short[] { 1, 2, 3 }));
        }
    }
}
