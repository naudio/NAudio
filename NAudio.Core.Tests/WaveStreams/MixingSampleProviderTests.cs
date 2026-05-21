using System;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    [TestFixture]
    public class MixingSampleProviderTests
    {
        [Test]
        public void WithNoInputsFirstReadReturnsNoSamples()
        {
            var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            var buffer = new float[1000];
            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(0));
        }

        [Test]
        public void WithReadFullySetNoInputsReturnsSampleCountRequested()
        {
            var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            msp.ReadFully = true;
            var buffer = new float[1000];
            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
        }

        [Test]
        public void WithOneInputReadsToTheEnd()
        {
            var input1 = new TestSampleProvider(44100, 2, 2000);
            var msp = new MixingSampleProvider([input1]);
            var buffer = new float[1000];
            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
            // randomly check one value
            Assert.That(buffer[567], Is.EqualTo(567));
        }

        [Test]
        public void WithOneInputReturnsSamplesReadIfNotEnoughToFullyRead()
        {
            var input1 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider([input1]);
            var buffer = new float[1000];
            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(800));
            // randomly check one value
            Assert.That(buffer[567], Is.EqualTo(567));
        }

        [Test]
        public void FullyReadCausesPartialBufferToBeZeroedOut()
        {
            var input1 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider([input1]);
            msp.ReadFully = true;
            // buffer of 1000 floats of value 9999
            var buffer = Enumerable.Range(1,1000).Select(n => 9999f).ToArray();

            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
            // check we get 800 samples, followed by zeroed out data
            Assert.That(buffer[567], Is.EqualTo(567f));
            Assert.That(buffer[799], Is.EqualTo(799f));
            Assert.That(buffer[800], Is.EqualTo(0));
            Assert.That(buffer[999], Is.EqualTo(0));
        }

        [Test]
        public void MixerInputEndedInvoked()
        {
            var input1 = new TestSampleProvider(44100, 2, 8000);
            var input2 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider([input1, input2]);
            ISampleProvider endedInput = null;
            msp.MixerInputEnded += (s, a) =>
            {
                Assert.That(endedInput, Is.Null);
                endedInput = a.SampleProvider;
            };
            // buffer of 1000 floats of value 9999
            var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

            Assert.That(msp.Read(buffer.AsSpan()), Is.EqualTo(buffer.Length));
            Assert.That(endedInput, Is.SameAs(input2));
            Assert.That(msp.MixerInputs.Count(), Is.EqualTo(1));
        }

    }
}
