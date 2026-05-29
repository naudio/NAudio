using System;
using NUnit.Framework;
using NAudio.Dsp;

namespace NAudio.Core.Tests
{
    [TestFixture]
    public class LfoTests
    {
        [Test]
        public void NoDelayProducesValueImmediately()
        {
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Square, FrequencyHz = 10 };
            // a square wave starts at +1 for the first half cycle
            Assert.That(lfo.Process(), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void DelayHoldsZeroThenOscillates()
        {
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Square, FrequencyHz = 10, DelaySeconds = 0.01f };
            lfo.Reset();
            // 10 samples of delay -> bipolar neutral (0)
            for (int i = 0; i < 10; i++)
                Assert.That(lfo.Process(), Is.EqualTo(0f), $"delay sample {i}");
            // after the delay the square wave produces its non-zero output
            Assert.That(Math.Abs(lfo.Process()), Is.GreaterThan(0.5f));
        }

        [Test]
        public void ResetReArmsTheDelay()
        {
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Square, FrequencyHz = 10, DelaySeconds = 0.003f };
            lfo.Reset();
            for (int i = 0; i < 3; i++) lfo.Process(); // consume the delay
            lfo.Process();                             // first oscillating sample
            lfo.Reset();                               // re-arm
            Assert.That(lfo.Process(), Is.EqualTo(0f));
        }
    }
}
