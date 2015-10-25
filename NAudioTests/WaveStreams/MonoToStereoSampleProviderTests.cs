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
            var read = stereoStream.Read(buffer, 0, 2000);
            Assert.AreEqual(2000, read);
            for (int n = 0; n < read; n+=2)
            {
                Assert.AreEqual(n/2, buffer[n], String.Format("left sample[{0}]",n));
                Assert.AreEqual(0, buffer[n+1], String.Format("right sample[{0}]",n+1));
            }
        }
    }
}