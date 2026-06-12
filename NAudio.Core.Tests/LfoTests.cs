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

        private static void AssertBitEqual(float expected, float actual, string context)
        {
            if (BitConverter.SingleToInt32Bits(expected) != BitConverter.SingleToInt32Bits(actual))
                Assert.Fail($"{context}: expected {expected:R} but was {actual:R}");
        }

        [Test]
        public void AdvanceMatchesRepeatedProcessForRandomizedConfigs()
        {
            // seeded sweep: every waveform x random frequency/StartPhase/delay,
            // with n values that land inside the delay, span the delay boundary
            // and span many phase wraps (including sample-and-hold re-rolls).
            // Bit-exactness is required: the sampler voice substitutes Advance
            // for per-sample Process loops and must not change a rendered sample.
            var rng = new Random(0x5EED);
            int[] counts = { 0, 1, 2, 7, 64, 1000 };
            foreach (LfoWaveform waveform in Enum.GetValues<LfoWaveform>())
            {
                for (int cfg = 0; cfg < 6; cfg++)
                {
                    float frequency = 0.2f + (float)rng.NextDouble() * 1500f;
                    float startPhase = (float)rng.NextDouble();
                    float delay = rng.Next(3) == 0 ? 0f : (float)rng.NextDouble() * 0.002f; // 0..~88 samples
                    Lfo Create() => new Lfo(44100)
                    {
                        Waveform = waveform,
                        FrequencyHz = frequency,
                        StartPhase = startPhase,
                        DelaySeconds = delay
                    };
                    foreach (int n in counts)
                    {
                        var reference = Create();
                        var fast = Create();
                        string context = $"{waveform} f={frequency} sp={startPhase} d={delay} n={n}";
                        float expected = 0f;
                        for (int i = 0; i < n; i++) expected = reference.Process();
                        float actual = fast.Advance(n);
                        if (n > 0) AssertBitEqual(expected, actual, context);
                        // the post-advance state (phase, delay countdown, S&H
                        // generator) must also be identical: the next samples,
                        // crossing further wraps, must match bit for bit
                        for (int i = 0; i < 16; i++)
                            AssertBitEqual(reference.Process(), fast.Process(), $"{context} follow-up {i}");
                    }
                }
            }
        }

        [Test]
        public void AdvanceZeroReportsNextValueWithoutChangingState()
        {
            var reference = new Lfo(1000) { Waveform = LfoWaveform.Sine, FrequencyHz = 35, StartPhase = 0.4f };
            var fast = new Lfo(1000) { Waveform = LfoWaveform.Sine, FrequencyHz = 35, StartPhase = 0.4f };
            float reported = fast.Advance(0);
            for (int i = 0; i < 8; i++)
            {
                float expected = reference.Process();
                if (i == 0) AssertBitEqual(expected, reported, "Advance(0) reports the next value");
                AssertBitEqual(expected, fast.Process(), $"sample {i} after Advance(0)");
            }
        }

        [Test]
        public void AdvanceWithinDelayReturnsZeroAndConsumesIt()
        {
            var lfo = new Lfo(1000) { Waveform = LfoWaveform.Square, FrequencyHz = 10, DelaySeconds = 0.01f };
            Assert.That(lfo.Advance(4), Is.EqualTo(0f), "still inside the 10-sample delay");
            Assert.That(lfo.Advance(6), Is.EqualTo(0f), "the call that exhausts the delay still returns 0");
            Assert.That(lfo.Process(), Is.EqualTo(1f).Within(1e-5f), "the next sample oscillates");
        }

        [Test]
        public void AdvanceRejectsNegativeCounts()
        {
            var lfo = new Lfo(1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => lfo.Advance(-1));
        }
    }
}
