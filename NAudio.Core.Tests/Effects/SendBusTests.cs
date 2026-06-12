using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class SendBusTests
    {
        // a trivial effect that scales its buffer, so the bus mechanics are
        // testable without depending on a particular reverb/chorus
        private sealed class GainEffect : IAudioEffect
        {
            private readonly float gain;
            public GainEffect(float gain) => this.gain = gain;
            public void Configure(WaveFormat format) { }
            public void Process(Span<float> buffer)
            {
                for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
            }
            public void Reset() { }
            public int LatencySamples => 0;
        }

        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        [Test]
        public void ProcessReturnAddsWetSignalToDestination()
        {
            var bus = new SendBus(new GainEffect(2f));
            bus.Configure(Stereo, 4);

            var send = bus.PrepareSend(4);          // 4 frames * 2 channels = 8 samples
            for (int i = 0; i < send.Length; i++) send[i] = 1f;

            var destination = new float[8];
            for (int i = 0; i < destination.Length; i++) destination[i] = 0.5f;

            bus.ProcessReturn(destination, 4);

            // each destination sample = dry 0.5 + wet (1 * gain 2) = 2.5
            foreach (var s in destination) Assert.That(s, Is.EqualTo(2.5f).Within(1e-6f));
        }

        [Test]
        public void PrepareSendClearsTheBufferEachBlock()
        {
            var bus = new SendBus(new GainEffect(1f));
            bus.Configure(Stereo, 4);

            var first = bus.PrepareSend(4);
            for (int i = 0; i < first.Length; i++) first[i] = 1f;
            bus.ProcessReturn(new float[8], 4);

            // a fresh PrepareSend must hand back a cleared buffer, not last block's
            var second = bus.PrepareSend(4);
            foreach (var s in second) Assert.That(s, Is.EqualTo(0f));
        }
    }
}
