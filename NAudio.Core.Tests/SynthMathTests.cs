using System;
using NUnit.Framework;
using NAudio.Dsp;

namespace NAudio.Core.Tests
{
    [TestFixture]
    public class SynthMathTests
    {
        [Test]
        public void MidiNoteToFrequencyA4Is440()
        {
            Assert.That(SynthMath.MidiNoteToFrequency(69), Is.EqualTo(440.0).Within(1e-9));
        }

        [Test]
        public void MidiNoteToFrequencyOctavesDoubleAndHalve()
        {
            Assert.That(SynthMath.MidiNoteToFrequency(81), Is.EqualTo(880.0).Within(1e-9));
            Assert.That(SynthMath.MidiNoteToFrequency(57), Is.EqualTo(220.0).Within(1e-9));
        }

        [Test]
        public void FrequencyToMidiNoteRoundTrips()
        {
            for (double note = 0; note <= 127; note += 0.5)
            {
                double freq = SynthMath.MidiNoteToFrequency(note);
                Assert.That(SynthMath.FrequencyToMidiNote(freq), Is.EqualTo(note).Within(1e-9));
            }
        }

        [Test]
        public void CentsToRatioOctaveIsTwo()
        {
            Assert.That(SynthMath.CentsToRatio(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SynthMath.CentsToRatio(1200), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(SynthMath.CentsToRatio(-1200), Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void RatioToCentsIsInverseOfCentsToRatio()
        {
            Assert.That(SynthMath.RatioToCents(2.0), Is.EqualTo(1200.0).Within(1e-9));
            Assert.That(SynthMath.RatioToCents(SynthMath.CentsToRatio(350)), Is.EqualTo(350.0).Within(1e-9));
        }

        [Test]
        public void AbsoluteCentsToHertz6900Is440()
        {
            Assert.That(SynthMath.AbsoluteCentsToHertz(6900), Is.EqualTo(440.0).Within(1e-6));
        }

        [Test]
        public void HertzToAbsoluteCentsIsInverse()
        {
            Assert.That(SynthMath.HertzToAbsoluteCents(SynthMath.AbsoluteCentsToHertz(4500)),
                Is.EqualTo(4500.0).Within(1e-6));
        }

        [Test]
        public void TimecentsToSecondsZeroIsOneSecond()
        {
            Assert.That(SynthMath.TimecentsToSeconds(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SynthMath.TimecentsToSeconds(1200), Is.EqualTo(2.0).Within(1e-9));
            Assert.That(SynthMath.TimecentsToSeconds(-1200), Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void SecondsToTimecentsIsInverse()
        {
            Assert.That(SynthMath.SecondsToTimecents(1.0), Is.EqualTo(0.0).Within(1e-9));
            Assert.That(SynthMath.SecondsToTimecents(SynthMath.TimecentsToSeconds(-2400)),
                Is.EqualTo(-2400.0).Within(1e-6));
        }

        [Test]
        public void AttenuationCentibelsToGain()
        {
            Assert.That(SynthMath.AttenuationCentibelsToGain(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SynthMath.AttenuationCentibelsToGain(200), Is.EqualTo(0.1).Within(1e-9));
            Assert.That(SynthMath.AttenuationCentibelsToGain(60), Is.EqualTo(SynthMath.DecibelsToGain(-6)).Within(1e-9));
        }

        [Test]
        public void CentibelsToGainPositiveIsLouder()
        {
            Assert.That(SynthMath.CentibelsToGain(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SynthMath.CentibelsToGain(200), Is.EqualTo(10.0).Within(1e-9));
        }

        [Test]
        public void DecibelsAndGainRoundTrip()
        {
            Assert.That(SynthMath.DecibelsToGain(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SynthMath.DecibelsToGain(20), Is.EqualTo(10.0).Within(1e-9));
            Assert.That(SynthMath.GainToDecibels(10.0), Is.EqualTo(20.0).Within(1e-9));
        }

        [Test]
        public void ResonanceCentibelsToQ()
        {
            // 0 cB (SF2.04 §8.1.2 gen 8: "the filter is not resonant") must map
            // to a flat Butterworth response, so the mapping subtracts 3.01 dB
            // before converting (the convention FluidSynth documents):
            // Q = 10^((cB/10 - 3.01)/20) -> ~0.707 at 0 cB, not the old 1.0
            Assert.That(SynthMath.ResonanceCentibelsToQ(0), Is.EqualTo(Math.Pow(10, -3.01 / 20.0)).Within(1e-12));
            Assert.That(SynthMath.ResonanceCentibelsToQ(0), Is.EqualTo(0.7071).Within(1e-3));
            // 200 cB = 20 dB resonance -> Q = 10^((20 - 3.01)/20) ~= 7.07
            Assert.That(SynthMath.ResonanceCentibelsToQ(200), Is.EqualTo(Math.Pow(10, (20.0 - 3.01) / 20.0)).Within(1e-9));
        }

        [Test]
        public void ResonanceCentibelsToQClampsToTheSpecRange()
        {
            // SF2.04 §8.1.2 gen 8: initialFilterQ ranges 0..960 cB; malformed
            // values clamp rather than mapping toward float infinity (which
            // would give a biquad with alpha = 0 that rings forever)
            Assert.That(SynthMath.ResonanceCentibelsToQ(20000),
                Is.EqualTo(SynthMath.ResonanceCentibelsToQ(960)).Within(1e-9));
            Assert.That(SynthMath.ResonanceCentibelsToQ(-50),
                Is.EqualTo(SynthMath.ResonanceCentibelsToQ(0)).Within(1e-12));
            Assert.That(double.IsFinite(SynthMath.ResonanceCentibelsToQ(double.MaxValue)), Is.True);
        }
    }
}
