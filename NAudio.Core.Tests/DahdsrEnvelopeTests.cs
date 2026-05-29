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
    }
}
