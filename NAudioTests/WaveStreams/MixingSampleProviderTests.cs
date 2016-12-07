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
            Assert.AreEqual(0, msp.Read(new float[1000], 0, 1000));
        }

        [Test]
        public void WithReadFullySetNoInputsReturnsSampleCountRequested()
        {
            var msp = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            msp.ReadFully = true;
            var buffer = new float[1000];
            Assert.AreEqual(buffer.Length, msp.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public void WithOneInputReadsToTheEnd()
        {
            var input1 = new TestSampleProvider(44100, 2, 2000);
            var msp = new MixingSampleProvider(new [] { input1});
            var buffer = new float[1000];
            Assert.AreEqual(buffer.Length, msp.Read(buffer, 0, buffer.Length));
            // randomly check one value
            Assert.AreEqual(567, buffer[567]);
        }

        [Test] 
        public void WithOneInputReturnsSamplesReadIfNotEnoughToFullyRead()
        {
            var input1 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider(new[] { input1 });
            var buffer = new float[1000];
            Assert.AreEqual(800, msp.Read(buffer, 0, buffer.Length));
            // randomly check one value
            Assert.AreEqual(567, buffer[567]);
        }

        [Test]
        public void FullyReadCausesPartialBufferToBeZeroedOut()
        {
            var input1 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider(new[] { input1 });
            msp.ReadFully = true;
            // buffer of 1000 floats of value 9999
            var buffer = Enumerable.Range(1,1000).Select(n => 9999f).ToArray();

            Assert.AreEqual(buffer.Length, msp.Read(buffer, 0, buffer.Length));
            // check we get 800 samples, followed by zeroed out data
            Assert.AreEqual(567f, buffer[567]);
            Assert.AreEqual(799f, buffer[799]);
            Assert.AreEqual(0, buffer[800]);
            Assert.AreEqual(0, buffer[999]);
        }

        [Test]
        public void MixerInputEndedInvoked()
        {
            var input1 = new TestSampleProvider(44100, 2, 8000);
            var input2 = new TestSampleProvider(44100, 2, 800);
            var msp = new MixingSampleProvider(new[] { input1, input2 });
            ISampleProvider endedInput = null;
            msp.MixerInputEnded += (s, a) =>
            {
                Assert.IsNull(endedInput);
                endedInput = a.SampleProvider;
            };
            // buffer of 1000 floats of value 9999
            var buffer = Enumerable.Range(1, 1000).Select(n => 9999f).ToArray();

            Assert.AreEqual(buffer.Length, msp.Read(buffer, 0, buffer.Length));
            Assert.AreSame(input2, endedInput);
            Assert.AreEqual(1,msp.MixerInputs.Count());
        }

    }
}
