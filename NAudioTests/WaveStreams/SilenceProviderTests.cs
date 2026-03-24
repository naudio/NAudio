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
            var read = sp.Read(b.AsSpan());
            Assert.That(read, Is.EqualTo(length));
            Assert.That(b, Is.EqualTo(new byte[length]));
        }

        [Test]
        public void ClearsEntireSpan()
        {
            var sp = new SilenceProvider(new WaveFormat(44100, 2));
            var length = 4;
            var b = Enumerable.Range(1, length).Select(n => (byte)1).ToArray();
            var read = sp.Read(b.AsSpan());
            Assert.That(read, Is.EqualTo(4));
            Assert.That(b, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        }
    }
}
