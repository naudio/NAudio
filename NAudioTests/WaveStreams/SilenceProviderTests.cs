using System;
using System.Linq;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class SilenceProviderTests
    {
        [Test]
        public void CanReadSilence()
        {
            var sp = new SilenceProvider(new WaveFormat(44100, 2));
            var length = 1000;
            var b = Enumerable.Range(1, length).Select(n => (byte) 1).ToArray();
            var read = sp.Read(b, 0, length);
            Assert.That(read, Is.EqualTo(length));
            Assert.That(b, Is.EqualTo(new byte[length]));
        }

        [Test]
        public void RespectsOffsetAndCount()
        {
            var sp = new SilenceProvider(new WaveFormat(44100, 2));
            var length = 10;
            var b = Enumerable.Range(1, length).Select(n => (byte)1).ToArray();
            var read = sp.Read(b, 2, 4);
            Assert.That(read, Is.EqualTo(4));
            Assert.That(b, Is.EqualTo(new byte[] { 1, 1, 0, 0, 0, 0, 1, 1, 1, 1}));
        }
    }
}
