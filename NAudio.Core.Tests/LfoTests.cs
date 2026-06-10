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

        [Test]
        public void DefaultStartPhaseKeepsHistoricBehaviour()
        {
            // StartPhase is opt-in: at the default of 0 the triangle still
            // starts at its +1 peak, as it always has
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Triangle, FrequencyHz = 10 };
            Assert.That(lfo.StartPhase, Is.EqualTo(0f));
            Assert.That(lfo.Process(), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void StartPhaseIsAppliedImmediatelyAndOnReset()
        {
            // SF2.04 §8.1.2 (gens 21/23): a SoundFont LFO "begins its upward ramp
            // from zero" — for the triangle that is phase 0.75 (zero, rising)
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Triangle, FrequencyHz = 10, StartPhase = 0.75f };
            float first = lfo.Process();
            float second = lfo.Process();
            float third = lfo.Process();
            Assert.That(first, Is.EqualTo(0f).Within(1e-6f), "the triangle starts at zero");
            Assert.That(second, Is.GreaterThan(first), "and ramps upward");
            Assert.That(third, Is.GreaterThan(second));

            lfo.Reset();
            Assert.That(lfo.Process(), Is.EqualTo(0f).Within(1e-6f), "Reset re-applies the start phase");
        }

        [Test]
        public void StartPhaseWrapsIntoOneCycle()
        {
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Triangle, FrequencyHz = 10, StartPhase = 1.75f };
            Assert.That(lfo.StartPhase, Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(lfo.Process(), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void FirstSampleAfterDelayIsAtTheStartPhase()
        {
            // the phase must not advance during the delay: the first oscillating
            // sample after the delay expires is taken exactly at StartPhase, so
            // the modulation steps from the delay's 0 to ~0, not to full depth
            var lfo = new Lfo(1000)
            {
                Waveform = LfoWaveform.Triangle,
                FrequencyHz = 10,
                StartPhase = 0.75f,
                DelaySeconds = 0.01f
            };
            lfo.Reset();
            for (int i = 0; i < 10; i++)
                Assert.That(lfo.Process(), Is.EqualTo(0f), $"delay sample {i}");
            Assert.That(lfo.Process(), Is.EqualTo(0f).Within(1e-6f), "first post-delay sample is at the start phase");
            float next = lfo.Process();
            Assert.That(next, Is.GreaterThan(0f), "then the upward ramp begins");
            Assert.That(lfo.Process(), Is.GreaterThan(next));
        }
    }
}
