using System;
using System.Linq;
using NAudioDemo.NetworkChatDemo;
using NUnit.Framework;

namespace NAudioTests.Codecs
{
    [TestFixture]
    // Internal because the codec types under test are linked-in internals of NAudioDemo.
    internal class OpusChatCodecTests
    {
        private const double ToneFrequencyHz = 440.0;
        private const double ToneDurationSeconds = 2.0;
        private const int ChunkMilliseconds = 50;
        // Opus has ~6.5ms encoder lookahead at 48kHz; skip a generous warmup before measuring quality.
        private const int WarmupSamples = 2000;

        [TestCaseSource(nameof(CodecCases))]
        public void EncodeDecode_PreservesSampleCountWithinTolerance(Func<OpusChatCodec> factory)
        {
            using var codec = factory();
            int sampleRate = codec.RecordFormat.SampleRate;
            byte[] inputPcm = GenerateSineWavePcm(sampleRate, ToneFrequencyHz, ToneDurationSeconds);

            byte[] decoded = RoundTrip(codec, inputPcm);

            int inputSamples = inputPcm.Length / 2;
            int decodedSamples = decoded.Length / 2;
            // Final partial chunk that didn't fill a 20ms frame stays buffered, so allow a small shortfall.
            Assert.That(decodedSamples, Is.GreaterThan(inputSamples * 0.95),
                "Decoded sample count should be close to input sample count");
            Assert.That(decodedSamples, Is.LessThanOrEqualTo(inputSamples + sampleRate / 50),
                "Decoded sample count should not exceed input by more than one frame");
        }

        [TestCaseSource(nameof(CodecCases))]
        public void EncodeDecode_ProducesNonSilentAudio(Func<OpusChatCodec> factory)
        {
            using var codec = factory();
            int sampleRate = codec.RecordFormat.SampleRate;
            byte[] inputPcm = GenerateSineWavePcm(sampleRate, ToneFrequencyHz, ToneDurationSeconds);

            byte[] decoded = RoundTrip(codec, inputPcm);

            double inputRms = ComputeRms(inputPcm, 0);
            double decodedRms = ComputeRms(decoded, WarmupSamples * 2);
            Assert.That(decodedRms, Is.GreaterThan(inputRms * 0.5),
                $"Decoded RMS ({decodedRms:F0}) should be at least half of input RMS ({inputRms:F0})");
            Assert.That(decodedRms, Is.LessThan(inputRms * 2.0),
                $"Decoded RMS ({decodedRms:F0}) should not be wildly larger than input RMS ({inputRms:F0})");
        }

        [TestCaseSource(nameof(CodecCases))]
        public void Encode_AchievesCompressionVersusRawPcm(Func<OpusChatCodec> factory)
        {
            using var codec = factory();
            int sampleRate = codec.RecordFormat.SampleRate;
            byte[] inputPcm = GenerateSineWavePcm(sampleRate, ToneFrequencyHz, ToneDurationSeconds);

            int totalEncodedBytes = 0;
            int chunkBytes = sampleRate * ChunkMilliseconds / 1000 * 2;
            for (int offset = 0; offset + chunkBytes <= inputPcm.Length; offset += chunkBytes)
            {
                totalEncodedBytes += codec.Encode(inputPcm, offset, chunkBytes).Length;
            }

            // Raw PCM is sampleRate * 2 bytes/sec; Opus VOIP at 16-48 kbps is well below that.
            // Allow 4x compression as a conservative floor (real Opus typically achieves much more).
            int rawBytes = inputPcm.Length;
            Assert.That(totalEncodedBytes, Is.LessThan(rawBytes / 4),
                $"Encoded {totalEncodedBytes} bytes vs raw {rawBytes} bytes — compression ratio too low");
        }

        [Test]
        public void Encode_HandlesPartialChunksByBufferingLeftovers()
        {
            using var codec = new WideBandOpusCodec();
            int sampleRate = codec.RecordFormat.SampleRate;
            // 13ms of audio — less than one 20ms Opus frame; should buffer entirely.
            int partialChunkBytes = sampleRate * 13 / 1000 * 2;
            byte[] partial = GenerateSineWavePcm(sampleRate, ToneFrequencyHz, 0.013);

            byte[] encoded = codec.Encode(partial, 0, partialChunkBytes);

            Assert.That(encoded.Length, Is.Zero, "Sub-frame input should yield no encoded packets yet");
        }

        [Test]
        public void EncodeDecode_PreservesFundamentalFrequency()
        {
            using var codec = new WideBandOpusCodec();
            int sampleRate = codec.RecordFormat.SampleRate;
            byte[] inputPcm = GenerateSineWavePcm(sampleRate, ToneFrequencyHz, ToneDurationSeconds);

            byte[] decoded = RoundTrip(codec, inputPcm);

            // Count zero-crossings after warmup; a 440Hz tone should give ~880 crossings/sec.
            short[] decodedSamples = ToShorts(decoded);
            int crossings = CountZeroCrossings(decodedSamples, WarmupSamples, decodedSamples.Length - WarmupSamples);
            double measuredHz = crossings / 2.0 / ((double)(decodedSamples.Length - WarmupSamples) / sampleRate);
            Assert.That(measuredHz, Is.EqualTo(ToneFrequencyHz).Within(20.0),
                $"Recovered fundamental {measuredHz:F1}Hz should be near input {ToneFrequencyHz}Hz");
        }

        private static byte[] RoundTrip(OpusChatCodec codec, byte[] inputPcm)
        {
            int sampleRate = codec.RecordFormat.SampleRate;
            int chunkBytes = sampleRate * ChunkMilliseconds / 1000 * 2;
            using var encodedStream = new System.IO.MemoryStream();
            for (int offset = 0; offset + chunkBytes <= inputPcm.Length; offset += chunkBytes)
            {
                byte[] encoded = codec.Encode(inputPcm, offset, chunkBytes);
                encodedStream.Write(encoded, 0, encoded.Length);
            }
            byte[] encodedBytes = encodedStream.ToArray();
            return codec.Decode(encodedBytes, 0, encodedBytes.Length);
        }

        private static byte[] GenerateSineWavePcm(int sampleRate, double frequency, double durationSeconds)
        {
            int sampleCount = (int)(sampleRate * durationSeconds);
            var samples = new short[sampleCount];
            double angularStep = 2.0 * Math.PI * frequency / sampleRate;
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (short)(Math.Sin(angularStep * i) * 16000);
            }
            var bytes = new byte[sampleCount * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static double ComputeRms(byte[] pcmBytes, int byteOffset)
        {
            int sampleCount = (pcmBytes.Length - byteOffset) / 2;
            if (sampleCount <= 0) return 0;
            double sumSquares = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(pcmBytes[byteOffset + i * 2] | (pcmBytes[byteOffset + i * 2 + 1] << 8));
                sumSquares += sample * (double)sample;
            }
            return Math.Sqrt(sumSquares / sampleCount);
        }

        private static short[] ToShorts(byte[] pcmBytes)
        {
            var shorts = new short[pcmBytes.Length / 2];
            Buffer.BlockCopy(pcmBytes, 0, shorts, 0, pcmBytes.Length);
            return shorts;
        }

        private static int CountZeroCrossings(short[] samples, int offset, int length)
        {
            int crossings = 0;
            for (int i = offset + 1; i < offset + length; i++)
            {
                if ((samples[i - 1] >= 0) != (samples[i] >= 0)) crossings++;
            }
            return crossings;
        }

        private static System.Collections.Generic.IEnumerable<TestCaseData> CodecCases()
        {
            yield return new TestCaseData((Func<OpusChatCodec>)(() => new NarrowBandOpusCodec())).SetName("NarrowBand 8kHz");
            yield return new TestCaseData((Func<OpusChatCodec>)(() => new WideBandOpusCodec())).SetName("WideBand 16kHz");
            yield return new TestCaseData((Func<OpusChatCodec>)(() => new FullBandOpusCodec())).SetName("FullBand 48kHz");
        }
    }
}
