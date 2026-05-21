using System;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class ConcatenatingSampleProviderTests
    {
        [Test]
        public void CanPassASingleProvider()
        {
            // arrange
            const int expectedLength = 5000;
            var input = new TestSampleProvider(44100, 2, expectedLength);
            var concatenator = new ConcatenatingSampleProvider([input]);
            var buffer = new float[2000];
            var totalRead = 0;

            // act
            while (true)
            {
                var read = concatenator.Read(buffer.AsSpan());
                if (read == 0) break;
                totalRead += read;
                Assert.That(totalRead <= expectedLength);
            }
            Assert.That(totalRead == expectedLength);
        }

        [Test]
        public void CanPassTwoProviders()
        {
            // arrange
            var expectedLength = 100;
            var input1 = new TestSampleProvider(44100, 2, 50);
            var input2 = new TestSampleProvider(44100, 2, 50);
            var concatenator = new ConcatenatingSampleProvider([input1, input2]);
            var buffer = new float[2000];

            var read = concatenator.Read(buffer.AsSpan());
            Assert.That(read, Is.EqualTo(expectedLength), "read == expectedLength");
            Assert.That(buffer[49], Is.EqualTo(49));
            Assert.That(buffer[50], Is.EqualTo(0));
            Assert.That(buffer[99], Is.EqualTo(49));
        }
    }
}
