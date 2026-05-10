using System;
using NAudio.Wave;
using NAudioTests.Utils;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class WaveStreamTests
    {
        [Test]
        public void CurrentTimeSetterAlignsToBlockBoundary_AlreadyAligned()
        {
            // 16-bit mono 8 kHz: BlockAlign = 2, AverageBytesPerSecond = 16000.
            // 100 ms -> 1600 bytes, already a multiple of BlockAlign.
            var stream = new NullWaveStream(new WaveFormat(8000, 16, 1), 1_000_000);

            stream.CurrentTime = TimeSpan.FromMilliseconds(100);

            Assert.That(stream.Position, Is.EqualTo(1600));
            Assert.That(stream.Position % stream.BlockAlign, Is.Zero);
        }

        [Test]
        public void CurrentTimeSetterAlignsToBlockBoundary_RoundsDownMidSample()
        {
            // 16-bit mono 8 kHz: AverageBytesPerSecond = 16000, BlockAlign = 2.
            // A duration that maps to an odd byte count must be rounded down so we
            // don't land in the middle of a sample (the bug from #106).
            // 9.9375 ms -> 0.0099375 * 16000 = 159 bytes -> should snap down to 158.
            var stream = new NullWaveStream(new WaveFormat(8000, 16, 1), 1_000_000);

            stream.CurrentTime = TimeSpan.FromTicks(99375); // 9.9375 ms

            Assert.That(stream.Position % stream.BlockAlign, Is.Zero);
            Assert.That(stream.Position, Is.EqualTo(158));
        }

        [Test]
        public void CurrentTimeSetterAlignsToBlockBoundary_NonPcmFormat()
        {
            // GSM 6.10: 8 kHz, AverageBytesPerSecond = 1625, BlockAlign = 65.
            // For non-PCM formats AverageBytesPerSecond != SampleRate * BlockAlign,
            // so the only correct rounding is bytePosition - (bytePosition % BlockAlign).
            // 1 second -> 1625 bytes -> snap down to 1625 - (1625 % 65) = 1625 - 0 = 1625? No:
            // 1625 % 65 = 0, so use 1.5s: 2437 bytes -> 2437 % 65 = 32 -> 2405.
            var gsm = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Gsm610,
                sampleRate: 8000,
                channels: 1,
                averageBytesPerSecond: 1625,
                blockAlign: 65,
                bitsPerSample: 0);
            var stream = new NullWaveStream(gsm, 1_000_000);

            stream.CurrentTime = TimeSpan.FromMilliseconds(1500);

            Assert.That(stream.Position % stream.BlockAlign, Is.Zero);
            Assert.That(stream.Position, Is.EqualTo(2405));
            // Sanity-check that the buggy "SampleRate * BlockAlign" formula from #754
            // would NOT match here (it would yield 8000 * 1.5 * 65 = 780000).
            Assert.That(stream.Position, Is.Not.EqualTo(780_000));
        }
    }
}
