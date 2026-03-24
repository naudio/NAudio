using System;
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
            var read = mono.Read(buffer.AsSpan(0, buffer.Length));
            Assert.That(read, Is.EqualTo(buffer.Length), "samples read");
            for (int sample = 0; sample < samples; sample++)
            {
                Assert.That(buffer[sample], Is.EqualTo(1 + 2*sample), "sample #" + sample);
            }
        }

        [Test]
        public void CorrectOutputFormat()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2);
            var mono = stereoSampleProvider.ToMono(0f, 1f);
            Assert.That(mono.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.IeeeFloat));
            Assert.That(mono.WaveFormat.Channels, Is.EqualTo(1));
            Assert.That(mono.WaveFormat.SampleRate, Is.EqualTo(44100));
        }

        [Test]
        public void CorrectOffset()
        {
            var stereoSampleProvider = new TestSampleProvider(44100, 2)
            {
                UseConstValue = true,
                ConstValue = 1
            };
            var mono = stereoSampleProvider.ToMono();

            var bufferLength = 30;
            var offset = 10;
            var samples = 10;

            // [10,20) in buffer will be filled with 1
            var buffer = new float[bufferLength];
            var read = mono.Read(buffer.AsSpan(offset, samples));
            Assert.That(read, Is.EqualTo(samples), "samples read");

            for (int i = 0; i < bufferLength; i++)
            {
                var sample = buffer[i];

                if (i < offset || i >= offset + samples)
                {
                    Assert.That(sample, Is.EqualTo(0), "not in Read range");
                }
                else
                {
                    Assert.That(sample, Is.Not.EqualTo(0), "in Read range");
                }
            }
        }
    }
}