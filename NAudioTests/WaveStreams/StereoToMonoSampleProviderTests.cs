using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    [Category("UnitTest")]
    public class StereoToMonoSampleProviderTests
    {
        [Test]
        public void RightChannelOnly()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2);
            var mono = stereoSampleProvider.ToMono(0f, 1f);
            var samples = 1000;
            var buffer = new float[samples];
            var read = mono.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, read, "samples read");
            for (int sample = 0; sample < samples; sample++)
            {
                Assert.AreEqual(1 + 2*sample, buffer[sample], "sample #" + sample);
            }
        }

        [Test]
        public void CorrectOutputFormat()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2);
            var mono = stereoSampleProvider.ToMono(0f, 1f);
            Assert.AreEqual(WaveFormatEncoding.IeeeFloat, mono.WaveFormat.Encoding);
            Assert.AreEqual(1, mono.WaveFormat.Channels);
            Assert.AreEqual(44100, mono.WaveFormat.SampleRate);
        }
    }
}