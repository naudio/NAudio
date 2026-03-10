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

    }
}
