using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioPlaybackOptionsTests
    {
        [Test]
        public void From_SampleProvider_WrapsAsWaveProvider()
        {
            var sampleProvider = new SignalGenerator(48000, 2);

            var options = AsioPlaybackOptions.From(sampleProvider);

            Assert.That(options.Source, Is.Not.Null);
            Assert.That(options.Source.WaveFormat.SampleRate, Is.EqualTo(48000));
            Assert.That(options.Source.WaveFormat.Channels, Is.EqualTo(2));
        }

        [Test]
        public void From_NullSampleProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AsioPlaybackOptions.From(null));
        }

        [Test]
        public void From_PassesThroughOutputChannelsAndBufferSize()
        {
            var sampleProvider = new SignalGenerator(44100, 2);
            var channels = new[] { 2, 5 };

            var options = AsioPlaybackOptions.From(sampleProvider, channels, bufferSize: 512);

            Assert.That(options.OutputChannels, Is.SameAs(channels));
            Assert.That(options.BufferSize, Is.EqualTo(512));
        }

        [Test]
        public void AutoStopOnEndOfStream_DefaultsToTrue()
        {
            var options = new AsioPlaybackOptions();
            Assert.That(options.AutoStopOnEndOfStream, Is.True);
        }
    }
}
