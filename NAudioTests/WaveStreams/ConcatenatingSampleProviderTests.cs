using System;
using System.Linq;
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
            var concatenator = new ConcatenatingSampleProvider(new[] {input});
            var buffer = new float[2000];
            var totalRead = 0;

            // act
            while (true)
            {
                var read = concatenator.Read(buffer, 0, buffer.Length);
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
            var concatenator = new ConcatenatingSampleProvider(new[] { input1, input2 });
            var buffer = new float[2000];
            
            var read = concatenator.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(expectedLength, read, "read == expectedLength");
            Assert.AreEqual(49, buffer[49]);
            Assert.AreEqual(0, buffer[50]);
            Assert.AreEqual(49, buffer[99]);
        }
    }
}
