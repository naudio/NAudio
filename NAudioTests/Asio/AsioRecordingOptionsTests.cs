using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioRecordingOptionsTests
    {
        [Test]
        public void SampleRate_DefaultsToNull()
        {
            var options = new AsioRecordingOptions();
            Assert.That(options.SampleRate, Is.Null, "SampleRate == null signals 'use driver's current rate'.");
        }

        [Test]
        public void BufferSize_DefaultsToNull()
        {
            var options = new AsioRecordingOptions();
            Assert.That(options.BufferSize, Is.Null, "BufferSize == null signals 'use driver's preferred size'.");
        }

        [Test]
        public void InputChannels_IsPreserved()
        {
            var channels = new[] { 0, 3, 5 };
            var options = new AsioRecordingOptions { InputChannels = channels, SampleRate = 48000 };

            Assert.That(options.InputChannels, Is.SameAs(channels));
            Assert.That(options.SampleRate, Is.EqualTo(48000));
        }
    }
}
