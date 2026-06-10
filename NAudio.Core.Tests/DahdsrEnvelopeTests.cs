using NUnit.Framework;
using NAudio.Dsp;

namespace NAudio.Core.Tests
{
    [TestFixture]
    public class DahdsrEnvelopeTests
    {
        [Test]
        public void StartsIdleWithZeroOutput()
        {
            var env = new DahdsrEnvelope(1000);
            Assert.That(env.Stage, Is.EqualTo(DahdsrEnvelope.EnvelopeStage.Idle));
            Assert.That(env.Process(), Is.EqualTo(0f));
        }

        [Test]
        public void DelayStageHoldsOutputAtZero()
        {
            var env = new DahdsrEnvelope(1000) { DelaySeconds = 0.01f, AttackSeconds = 0.05f };
            env.Gate(true);
            // first 10 samples (the delay) must be exactly zero
            for (int i = 0; i < 10; i++)
            {
                Assert.That(env.Process(), Is.EqualTo(0f), $"sample {i} during delay");
            }
            // after the delay, the attack should soon produce a rising output
            float a = env.Process();
            float b = env.Process();
            Assert.That(b, Is.GreaterThan(a));
        }

        [Test]
        public void AttackRisesTowardsOne()
        {
            var env = new DahdsrEnvelope(1000) { AttackSeconds = 0.02f };
            env.Gate(true);
            float prev = env.Process();
            bool reachedTop = false;
            for (int i = 0; i < 100; i++)
            {
                float v = env.Process();
                Assert.That(v, Is.GreaterThanOrEqualTo(prev - 1e-6f));
                prev = v;
                if (v >= 0.999f) { reachedTop = true; break; }
            }
            Assert.That(reachedTop, "attack should reach the top");
        }

        [Test]
        public void ZeroAttackJumpsStraightToFullLevel()
        {
            var env = new DahdsrEnvelope(1000) { AttackSeconds = 0f, DecaySeconds = 1f, SustainLevel = 0.5f };
            env.Gate(true);
            Assert.That(env.Process(), Is.EqualTo(1f));
        }

        [Test]
        public void DecayFallsToSustainLevel()
        {
            var env = new DahdsrEnvelope(1000)
            {
                AttackSeconds = 0.001f,
                DecaySeconds = 0.02f,
                SustainLevel = 0.4f
            };
            env.Gate(true);
            float last = 0;
            for (int i = 0; i < 500; i++) last = env.Process();
            Assert.That(env.Stage, Is.EqualTo(DahdsrEnvelope.EnvelopeStage.Sustain));
            Assert.That(last, Is.EqualTo(0.4f).Within(0.01f));
        }

        [Test]
        public void SustainHoldsUntilGateOff()
        {
            var env = new DahdsrEnvelope(1000)
            {
                AttackSeconds = 0.001f,
                DecaySeconds = 0.005f,
                SustainLevel = 0.6f
            };
            env.Gate(true);
            for (int i = 0; i < 500; i++) env.Process();
            float s1 = env.Process();
            for (int i = 0; i < 1000; i++) env.Process();
            float s2 = env.Process();
            Assert.That(s1, Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(s2, Is.EqualTo(s1).Within(1e-6f));
        }

        [Test]
        public void ReleaseFallsToZeroAndFinishes()
        {
            var env = new DahdsrEnvelope(1000)
            {
                AttackSeconds = 0.001f,
                DecaySeconds = 0.001f,
                SustainLevel = 0.8f,
                ReleaseSeconds = 0.02f
            };
            env.Gate(true);
            for (int i = 0; i < 100; i++) env.Process();
            env.Gate(false);
            Assert.That(env.Stage, Is.EqualTo(DahdsrEnvelope.EnvelopeStage.Release));
            for (int i = 0; i < 2000; i++) env.Process();
            Assert.That(env.IsFinished, Is.True);
            Assert.That(env.Output, Is.EqualTo(0f));
        }

        [Test]
        public void ResetReturnsToIdle()
        {
            var env = new DahdsrEnvelope(1000) { AttackSeconds = 0.01f };
            env.Gate(true);
            for (int i = 0; i < 5; i++) env.Process();
            env.Reset();
            Assert.That(env.Stage, Is.EqualTo(DahdsrEnvelope.EnvelopeStage.Idle));
            Assert.That(env.Output, Is.EqualTo(0f));
        }

        // Counts Process() calls from gate-on until the given stage is reached
        // (zero attack/hold so the decay starts immediately).
        private static int SamplesUntil(DahdsrEnvelope env, DahdsrEnvelope.EnvelopeStage stage, int limit)
        {
            int samples = 0;
            while (env.Stage != stage && samples < limit)
            {
                env.Process();
                samples++;
            }
            return samples;
        }

        [Test]
        public void DecayToHighSustainTruncatesProportionally()
        {
            // SF2.04 §8.1.2 gen 36: the decay time is the time for a 100% change
            // (full scale to -100 dB); the sustain level truncates the ramp. With
            // a 0.5 s decay and a -1 dB sustain the ramp must complete in about
            // 0.5 s x 1/100 = 5 ms (~221 samples), not creep toward sustain for
            // most of the 0.5 s.
            var env = new DahdsrEnvelope(44100)
            {
                AttackSeconds = 0f,
                DecaySeconds = 0.5f,
                SustainLevel = 0.891f // -1.0 dB
            };
            env.Gate(true);
            int samples = SamplesUntil(env, DahdsrEnvelope.EnvelopeStage.Sustain, 44100);
            Assert.That(samples, Is.LessThan(450), "a -1 dB sustain should truncate the decay after ~5 ms");
            Assert.That(samples, Is.GreaterThan(100), "the ramp still takes its proportional share of the decay time");
        }

        [Test]
        public void DecayToZeroSustainTakesTheFullDecayTime()
        {
            // with sustain at silence the decay runs its full 100 dB travel, so
            // it lasts the whole DecaySeconds (0.5 s = 22050 samples at 44.1 kHz)
            var env = new DahdsrEnvelope(44100)
            {
                AttackSeconds = 0f,
                DecaySeconds = 0.5f,
                SustainLevel = 0f
            };
            env.Gate(true);
            int samples = SamplesUntil(env, DahdsrEnvelope.EnvelopeStage.Sustain, 2 * 44100);
            Assert.That(samples, Is.EqualTo(22050).Within(700));
        }

        [Test]
        public void DecayIsLinearInDecibels()
        {
            // SF2.04 §8.1.2 gen 36: the volume-envelope decay is a constant-rate
            // dB ramp, i.e. a constant ratio between successive output samples —
            // not an exponential approach that flattens out near the sustain level
            var env = new DahdsrEnvelope(44100)
            {
                AttackSeconds = 0f,
                DecaySeconds = 0.1f,
                SustainLevel = 0.5f
            };
            env.Gate(true);
            env.Process(); // consume the instant attack (output = 1, decay next)
            float prev = env.Process();
            float firstRatio = env.Process() / prev;
            prev = env.Output;
            while (env.Stage == DahdsrEnvelope.EnvelopeStage.Decay && env.Output > 0.55f)
            {
                float current = env.Process();
                if (env.Stage != DahdsrEnvelope.EnvelopeStage.Decay) break;
                Assert.That(current / prev, Is.EqualTo(firstRatio).Within(5e-5f),
                    "the per-sample decay ratio must stay constant (linear in dB)");
                prev = current;
            }
            Assert.That(prev, Is.LessThan(0.6f), "the loop should have covered a useful stretch of the decay");
        }

        [Test]
        public void ReleaseFromLowSustainCompletesProportionallySooner()
        {
            // SF2.04 §8.1.2 gen 38: the release time is the time for a 100%
            // change (full scale to -100 dB), so releasing from a -20 dB sustain
            // (0.1) covers 80 dB and takes ~80% of the time a release from full
            // scale does.
            int FramesToFinish(float sustain)
            {
                var env = new DahdsrEnvelope(44100)
                {
                    AttackSeconds = 0f,
                    DecaySeconds = 0.001f,
                    SustainLevel = sustain,
                    ReleaseSeconds = 0.5f
                };
                env.Gate(true);
                SamplesUntil(env, DahdsrEnvelope.EnvelopeStage.Sustain, 44100);
                env.Gate(false);
                int samples = 0;
                while (!env.IsFinished && samples < 2 * 44100)
                {
                    env.Process();
                    samples++;
                }
                return samples;
            }

            int fromLowSustain = FramesToFinish(0.1f);
            int fromFullScale = FramesToFinish(1.0f);
            Assert.That(fromFullScale, Is.EqualTo(22050).Within(700), "release from full scale runs the whole 100 dB");
            Assert.That((double)fromLowSustain / fromFullScale, Is.EqualTo(0.8).Within(0.02),
                "release from -20 dB covers 80 of the 100 dB travel");
        }
    }
}
