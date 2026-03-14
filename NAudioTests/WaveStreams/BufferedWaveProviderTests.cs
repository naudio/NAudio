using System;
using System.Linq;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class BufferedWaveProviderTests
    {
        [Test]
        public void CanClearBeforeWritingSamples()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.ClearBuffer();
            Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
        }
        
        [Test]
        public void BufferedBytesAreReturned()
        {
            var bytesToBuffer = 1000;
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            var data = Enumerable.Range(1, bytesToBuffer).Select(n => (byte)(n % 256)).ToArray();
            bwp.AddSamples(data, 0, data.Length);
            Assert.That(bwp.BufferedBytes, Is.EqualTo(bytesToBuffer));
            var readBuffer = new byte[bytesToBuffer];
            var bytesRead = bwp.Read(readBuffer, 0, bytesToBuffer);
            Assert.That(bytesRead, Is.EqualTo(bytesToBuffer));
            Assert.That(readBuffer, Is.EqualTo(data));
            Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
        }

        [Test]
        public void EmptyBufferCanReturnZeroFromRead()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            bwp.ReadFully = false;
            var buffer = new byte[44100];
            var read = bwp.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(0));
        }

        [Test]
        public void PartialReadsPossibleWithReadFullyFalse()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            bwp.ReadFully = false;
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 2000);
            var read = bwp.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(2000));
            Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
        }

        [Test]
        public void FullReadsByDefault()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 2000);
            var read = bwp.Read(buffer, 0, buffer.Length);
            Assert.That(read, Is.EqualTo(buffer.Length));
            Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
        }

        [Test]
        public void WhenBufferHasMoreThanNeededReadFully()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            var buffer = new byte[44100];
            bwp.AddSamples(buffer, 0, 5000);
            var read = bwp.Read(buffer, 0, 2000);
            Assert.That(read, Is.EqualTo(2000));
            Assert.That(bwp.BufferedBytes, Is.EqualTo(3000));
        }

        // ── Constructor defaults ──────────────────────────────────────────────

        [Test]
        public void DefaultBufferLengthIsFiveSeconds()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bwp = new BufferedWaveProvider(format);
            Assert.That(bwp.BufferLength, Is.EqualTo(format.AverageBytesPerSecond * 5));
        }

        [Test]
        public void ReadFullyIsTrueByDefault()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat());
            Assert.That(bwp.ReadFully, Is.True);
        }

        [Test]
        public void WaveFormatPropertyReturnsConstructorArgument()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bwp = new BufferedWaveProvider(format);
            Assert.That(bwp.WaveFormat, Is.SameAs(format));
        }

        // ── BufferLength property ─────────────────────────────────────────────

        [Test]
        public void BufferLengthReflectsConstructorDuration()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bwp = new BufferedWaveProvider(format, TimeSpan.FromSeconds(2));
            Assert.That(bwp.BufferLength, Is.EqualTo((int)(2.0 * format.AverageBytesPerSecond)));
        }

        // ── BufferDuration property ───────────────────────────────────────────

        [Test]
        public void BufferDurationReflectsConstructorArgument()
        {
            var format = new WaveFormat(44100, 16, 2);
            var bwp = new BufferedWaveProvider(format, TimeSpan.FromSeconds(3));
            Assert.That(bwp.BufferDuration.TotalSeconds, Is.EqualTo(3.0).Within(0.001));
        }

        // ── DiscardOnBufferOverflow ───────────────────────────────────────────

        [Test]
        public void AddSamplesThrowsWhenBufferFullAndDiscardDisabled()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100));
            bwp.AddSamples(new byte[bwp.BufferLength], 0, bwp.BufferLength);
            Assert.Throws<InvalidOperationException>(() => bwp.AddSamples(new byte[1], 0, 1));
        }

        [Test]
        public void AddSamplesDiscardsWhenBufferFullAndDiscardEnabled()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2), TimeSpan.FromMilliseconds(100));
            bwp.DiscardOnBufferOverflow = true;
            bwp.AddSamples(new byte[bwp.BufferLength], 0, bwp.BufferLength);
            Assert.DoesNotThrow(() => bwp.AddSamples(new byte[100], 0, 100));
            Assert.That(bwp.BufferedBytes, Is.EqualTo(bwp.BufferLength));
        }

        // ── ClearBuffer ───────────────────────────────────────────────────────

        [Test]
        public void ClearBufferResetsBufferedBytesToZero()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.AddSamples(new byte[500], 0, 500);
            bwp.ClearBuffer();
            Assert.That(bwp.BufferedBytes, Is.EqualTo(0));
        }

        // ── Offset parameters ─────────────────────────────────────────────────

        [Test]
        public void AddSamplesRespectsSourceOffset()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.ReadFully = false;
            bwp.AddSamples(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 4, 4);
            var readBuffer = new byte[4];
            bwp.Read(readBuffer, 0, 4);
            Assert.That(readBuffer, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
        }

        [Test]
        public void ReadRespectsDestinationOffset()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.ReadFully = false;
            bwp.AddSamples(new byte[] { 1, 2, 3, 4 }, 0, 4);
            var readBuffer = new byte[8];
            var bytesRead = bwp.Read(readBuffer, 4, 4);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(readBuffer, Is.EqualTo(new byte[] { 0, 0, 0, 0, 1, 2, 3, 4 }));
        }

        // ── Miscellaneous ─────────────────────────────────────────────────────

        [Test]
        public void MultipleAddSamplesCallsAccumulate()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            bwp.AddSamples(new byte[200], 0, 200);
            bwp.AddSamples(new byte[300], 0, 300);
            Assert.That(bwp.BufferedBytes, Is.EqualTo(500));
        }

        [Test]
        public void ReadFullyZeroFillsBufferWhenNothingEverWritten()
        {
            var bwp = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
            var buffer = new byte[] { 1, 2, 3, 4 };
            var bytesRead = bwp.Read(buffer, 0, 4);
            Assert.That(bytesRead, Is.EqualTo(4));
            Assert.That(buffer, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        }
    }
}
