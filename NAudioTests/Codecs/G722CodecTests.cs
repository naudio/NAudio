using System;
using System.Linq;
using NAudio.Codecs;
using NUnit.Framework;

namespace NAudioTests.Codecs
{
    [TestFixture]
    public class G722CodecTests
    {
        [TestCase(48000, 6)]
        [TestCase(56000, 7)]
        [TestCase(64000, 8)]
        public void Constructor_SetsExpectedBitsPerSample(int rate, int expectedBitsPerSample)
        {
            var state = new G722CodecState(rate, G722Flags.None);

            Assert.That(state.BitsPerSample, Is.EqualTo(expectedBitsPerSample));
            Assert.That(state.Band[0].det, Is.EqualTo(32));
            Assert.That(state.Band[1].det, Is.EqualTo(8));
        }

        [Test]
        public void Constructor_InvalidRate_Throws()
        {
            Assert.That(() => new G722CodecState(32000, G722Flags.None), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Constructor_OnlyEnablesPackedForNonEightBitMode()
        {
            var packed56000 = new G722CodecState(56000, G722Flags.Packed);
            var packed64000 = new G722CodecState(64000, G722Flags.Packed);

            Assert.That(packed56000.Packed, Is.True);
            Assert.That(packed64000.Packed, Is.False);
        }

        [Test]
        public void Encode_UnpackedWideband_ReturnsOneBytePerCode()
        {
            var codec = new G722Codec();
            var state = new G722CodecState(64000, G722Flags.None);
            var input = CreatePcm(320);
            var encoded = new byte[input.Length / 2];

            var bytesEncoded = codec.Encode(state, encoded, input, input.Length);

            Assert.That(bytesEncoded, Is.EqualTo(input.Length / 2));
        }

        [Test]
        public void Encode_UnpackedNarrowband_ReturnsOneBytePerInputSample()
        {
            var codec = new G722Codec();
            var state = new G722CodecState(64000, G722Flags.SampleRate8000);
            var input = CreatePcm(320);
            var encoded = new byte[input.Length];

            var bytesEncoded = codec.Encode(state, encoded, input, input.Length);

            Assert.That(bytesEncoded, Is.EqualTo(input.Length));
        }

        [TestCase(false, false, 120, 240)]
        [TestCase(true, false, 120, 120)]
        [TestCase(false, true, 120, 240)]
        public void Decode_ReturnsExpectedSampleCount(bool sampleRate8000, bool ituMode, int encodedBytes, int expectedSamples)
        {
            var codec = new G722Codec();
            var options = sampleRate8000 ? G722Flags.SampleRate8000 : G722Flags.None;
            var state = new G722CodecState(64000, options)
            {
                ItuTestMode = ituMode
            };
            var encoded = CreateEncodedData(encodedBytes);
            var decoded = new short[expectedSamples + 8];

            var samplesDecoded = codec.Decode(state, decoded, encoded, encoded.Length);

            Assert.That(samplesDecoded, Is.EqualTo(expectedSamples));
        }

        [Test]
        public void EncodeDecode_UnpackedWideband_PreservesSampleCount()
        {
            var codec = new G722Codec();
            var encoderState = new G722CodecState(64000, G722Flags.None);
            var decoderState = new G722CodecState(64000, G722Flags.None);
            var input = CreatePcm(320);
            var encoded = new byte[input.Length / 2];
            var decoded = new short[input.Length];

            var bytesEncoded = codec.Encode(encoderState, encoded, input, input.Length);
            var samplesDecoded = codec.Decode(decoderState, decoded, encoded, bytesEncoded);

            Assert.That(samplesDecoded, Is.EqualTo(input.Length));
            Assert.That(decoded.Any(s => s != 0), Is.True);
        }

        [Test]
        public void EncodeDecode_UnpackedNarrowband_PreservesSampleCount()
        {
            var codec = new G722Codec();
            var encoderState = new G722CodecState(64000, G722Flags.SampleRate8000);
            var decoderState = new G722CodecState(64000, G722Flags.SampleRate8000);
            var input = CreatePcm(320);
            var encoded = new byte[input.Length];
            var decoded = new short[input.Length];

            var bytesEncoded = codec.Encode(encoderState, encoded, input, input.Length);
            var samplesDecoded = codec.Decode(decoderState, decoded, encoded, bytesEncoded);

            Assert.That(samplesDecoded, Is.EqualTo(input.Length));
            Assert.That(decoded.Any(s => s != 0), Is.True);
        }

        [TestCase(48000, 32, 12)]
        [TestCase(56000, 32, 14)]
        public void Encode_Packed_ReturnsExpectedByteCount(int rate, int inputSamples, int expectedBytes)
        {
            var codec = new G722Codec();
            var state = new G722CodecState(rate, G722Flags.Packed);
            var input = CreatePcm(inputSamples);
            var encoded = new byte[input.Length];

            var bytesEncoded = codec.Encode(state, encoded, input, input.Length);

            Assert.That(bytesEncoded, Is.EqualTo(expectedBytes));
        }

        [Test]
        public void PackedEncode_PreservesBitBufferAcrossCalls()
        {
            var codec = new G722Codec();
            var state = new G722CodecState(56000, G722Flags.Packed);
            var input = CreatePcm(2);
            var firstOutput = new byte[1];
            var secondOutput = new byte[1];

            var firstBytes = codec.Encode(state, firstOutput, input, input.Length);
            var bitsAfterFirstCall = state.OutBits;
            var secondBytes = codec.Encode(state, secondOutput, input, input.Length);

            Assert.That(firstBytes, Is.EqualTo(0));
            Assert.That(bitsAfterFirstCall, Is.EqualTo(7));
            Assert.That(secondBytes, Is.EqualTo(1));
            Assert.That(state.OutBits, Is.EqualTo(6));
        }

        [Test]
        public void PackedDecode_PreservesBitBufferAcrossCalls()
        {
            var codec = new G722Codec();
            var state = new G722CodecState(56000, G722Flags.Packed);
            var output = new short[8];

            var firstSamples = codec.Decode(state, output, new byte[] { 0xAA }, 1);
            var bitsAfterFirstCall = state.InBits;
            var secondSamples = codec.Decode(state, output, new byte[] { 0x55 }, 1);

            Assert.That(firstSamples, Is.EqualTo(2));
            Assert.That(bitsAfterFirstCall, Is.EqualTo(1));
            Assert.That(secondSamples, Is.EqualTo(2));
            Assert.That(state.InBits, Is.EqualTo(2));
        }

        private static short[] CreatePcm(int sampleCount)
        {
            var data = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                data[i] = (short)((((i * 73) % 65536) - 32768) / 4);
            }
            return data;
        }

        private static byte[] CreateEncodedData(int length)
        {
            var data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = (byte)((i * 37 + 11) & 0xFF);
            }
            return data;
        }
    }
}
