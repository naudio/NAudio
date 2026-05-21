using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    [TestFixture]
    [Category("UnitTest")]
    public class AsioDuplexOptionsTests
    {
        [Test]
        public void Defaults_AreSentinelNulls()
        {
            var options = new AsioDuplexOptions();
            Assert.Multiple(() =>
            {
                Assert.That(options.InputChannels, Is.Null);
                Assert.That(options.OutputChannels, Is.Null);
                Assert.That(options.SampleRate, Is.Null, "SampleRate == null signals 'use driver's current rate'.");
                Assert.That(options.BufferSize, Is.Null, "BufferSize == null signals 'use driver's preferred size'.");
                Assert.That(options.Processor, Is.Null);
            });
        }

        [Test]
        public void Init_PreservesAllValues()
        {
            int[] inputs = [0, 2];
            int[] outputs = [4, 5];
            AsioProcessCallback processor = (in AsioProcessBuffers _) => { };

            var options = new AsioDuplexOptions
            {
                InputChannels = inputs,
                OutputChannels = outputs,
                SampleRate = 96000,
                BufferSize = 256,
                Processor = processor
            };

            Assert.That(options.InputChannels, Is.SameAs(inputs));
            Assert.That(options.OutputChannels, Is.SameAs(outputs));
            Assert.That(options.SampleRate, Is.EqualTo(96000));
            Assert.That(options.BufferSize, Is.EqualTo(256));
            Assert.That(options.Processor, Is.SameAs(processor));
        }
    }
}
