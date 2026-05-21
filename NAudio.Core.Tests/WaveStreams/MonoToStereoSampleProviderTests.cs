using System;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [Category("UnitTest")]
    public class MonoToStereoSampleProviderTests
    {
        [Test]
        public void LeftChannelOnly()
        {
            var stereoStream = new TestSampleProvider(44100,1).ToStereo(1.0f, 0.0f);
            var buffer = new float[2000];
            var read = stereoStream.Read(buffer.AsSpan(0, 2000));
            Assert.That(read, Is.EqualTo(2000));
            for (int n = 0; n < read; n+=2)
            {
                Assert.That(buffer[n], Is.EqualTo(n/2), String.Format("left sample[{0}]",n));
                Assert.That(buffer[n+1], Is.EqualTo(0), String.Format("right sample[{0}]",n+1));
            }
        }
    }
}