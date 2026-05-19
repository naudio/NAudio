using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    /// <summary>
    /// Covers the parameter validation and state-reset behaviour added in response
    /// to issue #190 — out-of-Nyquist or non-positive parameters must throw, and
    /// updating coefficients on an existing filter must not let prior state (e.g.
    /// a latched <see cref="float.NaN"/> in y1/y2) survive into the next run.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class BiQuadFilterValidationTests
    {
        private const float SampleRate = 48000f;
        private const float Nyquist = SampleRate / 2f;

        // -- frequency bounds ---------------------------------------------------

        [Test]
        public void SetPeakingEqRejectsFrequencyAtOrAboveNyquist()
        {
            var filter = BiQuadFilter.PeakingEQ(SampleRate, 1000f, 1f, 0f);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => filter.SetPeakingEq(SampleRate, Nyquist, 1f, 6f));
            Assert.That(ex.ParamName, Is.EqualTo("centreFrequency"));
        }

        [Test]
        public void SetPeakingEqRejectsZeroFrequency()
        {
            var filter = BiQuadFilter.PeakingEQ(SampleRate, 1000f, 1f, 0f);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => filter.SetPeakingEq(SampleRate, 0f, 1f, 6f));
            Assert.That(ex.ParamName, Is.EqualTo("centreFrequency"));
        }

        [Test]
        public void SetLowPassFilterRejectsFrequencyAboveNyquist()
        {
            var filter = BiQuadFilter.LowPassFilter(SampleRate, 1000f, 0.707f);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => filter.SetLowPassFilter(SampleRate, Nyquist + 1f, 0.707f));
            Assert.That(ex.ParamName, Is.EqualTo("cutoffFrequency"));
        }

        [Test]
        public void SetHighPassFilterRejectsNegativeFrequency()
        {
            var filter = BiQuadFilter.HighPassFilter(SampleRate, 1000f, 0.707f);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => filter.SetHighPassFilter(SampleRate, -1f, 0.707f));
            Assert.That(ex.ParamName, Is.EqualTo("cutoffFrequency"));
        }

        // -- the original #190 trigger ------------------------------------------

        [Test]
        public void PeakingEqFactoryRejectsFrequencyAtOrAboveNyquist()
        {
            // The exact scenario from issue #190: an EQ band at 9600 Hz against an
            // 8 kHz source. Pre-fix this silently produced unstable coefficients;
            // post-fix it must throw with a clear param name.
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => BiQuadFilter.PeakingEQ(8000f, 9600f, 1f, 0f));
            Assert.That(ex.ParamName, Is.EqualTo("centreFrequency"));
        }

        // -- q / shelfSlope -----------------------------------------------------

        [Test]
        public void SetPeakingEqRejectsZeroQ()
        {
            var filter = BiQuadFilter.PeakingEQ(SampleRate, 1000f, 1f, 0f);
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => filter.SetPeakingEq(SampleRate, 1000f, 0f, 6f));
            Assert.That(ex.ParamName, Is.EqualTo("q"));
        }

        [Test]
        public void LowShelfFactoryRejectsZeroSlope()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => BiQuadFilter.LowShelf(SampleRate, 1000f, 0f, 6f));
            Assert.That(ex.ParamName, Is.EqualTo("shelfSlope"));
        }

        [Test]
        public void HighShelfFactoryRejectsNegativeSlope()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => BiQuadFilter.HighShelf(SampleRate, 1000f, -1f, 6f));
            Assert.That(ex.ParamName, Is.EqualTo("shelfSlope"));
        }

        // -- sample rate --------------------------------------------------------

        [Test]
        public void NotchFilterFactoryRejectsZeroSampleRate()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => BiQuadFilter.NotchFilter(0f, 1000f, 1f));
            Assert.That(ex.ParamName, Is.EqualTo("sampleRate"));
        }

        // -- state reset on coefficient change (the core #190 fix) -------------

        [Test]
        public void SetPeakingEqResetsStateBetweenRuns()
        {
            // After running samples through filter A and then re-tuning it, its output
            // for a fresh signal must match a brand-new filter at the same coefficients.
            // Pre-fix, the latched x1/x2/y1/y2 would have made A diverge from B.
            var input = GenerateTestSignal(2048);

            var reused = BiQuadFilter.PeakingEQ(SampleRate, 1000f, 1f, 12f);
            for (int i = 0; i < input.Length; i++) reused.Transform(input[i]);

            // Re-tune to a different operating point. With the fix, x1/x2/y1/y2 reset to 0.
            reused.SetPeakingEq(SampleRate, 4000f, 0.7f, -3f);

            var fresh = BiQuadFilter.PeakingEQ(SampleRate, 4000f, 0.7f, -3f);

            var reusedOut = new float[input.Length];
            var freshOut = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                reusedOut[i] = reused.Transform(input[i]);
                freshOut[i] = fresh.Transform(input[i]);
            }

            Assert.That(reusedOut, Is.EqualTo(freshOut),
                "after SetPeakingEq, the filter must behave identically to a freshly constructed one");
        }

        [Test]
        public void StateResetClearsLatchedNonFiniteOutput()
        {
            // Drive a peaking filter with a high-gain DC ramp until its internal state grows
            // very large, then re-tune it with SetPeakingEq. A subsequent run on a small finite
            // input must produce strictly finite output — the fix's contract.
            var filter = BiQuadFilter.PeakingEQ(SampleRate, 1000f, 0.1f, 24f);
            for (int i = 0; i < 16384; i++) filter.Transform(1f);

            filter.SetPeakingEq(SampleRate, 2000f, 1f, 0f);

            var probe = GenerateTestSignal(256);
            foreach (var s in probe)
                Assert.That(float.IsFinite(filter.Transform(s)), Is.True,
                    "filter output must be finite after a coefficient change resets state");
        }

        private static float[] GenerateTestSignal(int length, int seed = 42)
        {
            var rng = new Random(seed);
            var signal = new float[length];
            for (int i = 0; i < length; i++)
                signal[i] = (float)(rng.NextDouble() * 2 - 1);
            return signal;
        }
    }
}
