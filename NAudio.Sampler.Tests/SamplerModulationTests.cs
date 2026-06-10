using System;
using NAudio.Sampler;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for the continuous modulation engine: LFO/mod-envelope routed to
    /// pitch, filter cutoff and volume. Asserted on rendered output, since the
    /// effects (vibrato, tremolo, filter sweep) are observable in the signal.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SamplerModulationTests
    {
        private const int SampleRate = 44100;

        // generator operator numbers used here
        private const ushort GenModLfoToPitch = 5;
        private const ushort GenVibLfoToPitch = 6;
        private const ushort GenModLfoToVolume = 13;
        private const ushort GenModEnvToFilter = 11;
        private const ushort GenInitialFilterFc = 8;
        private const ushort GenDelayVibLfo = 23;
        private const ushort GenFreqVibLfo = 24;
        private const ushort GenFreqModLfo = 22;
        private const ushort GenAttackModEnv = 26;
        private const ushort GenSampleModes = 54;
        private const ushort GenRootKey = 58;
        private const ushort GenSampleId = 53;

        // absolute cents for a frequency: 1200*log2(hz/8.176)
        private static ushort AbsoluteCents(double hz) =>
            unchecked((ushort)(short)Math.Round(1200.0 * Math.Log2(hz / 8.1757989156437073336)));

        // timecents for seconds: 1200*log2(seconds)
        private static ushort Timecents(double seconds) =>
            unchecked((ushort)(short)Math.Round(1200.0 * Math.Log2(seconds)));

        /// <summary>Builds a looped constant-amplitude instrument with extra generators.</summary>
        private static NAudio.SoundFont.SoundFont BuildModInstrument(params (ushort oper, ushort amount)[] extra)
        {
            var data = new byte[8];
            for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // 0.5 fs

            var gens = new System.Collections.Generic.List<byte[]>();
            foreach (var (oper, amount) in extra)
                gens.Add(SoundFontTestBuilder.Gen(oper, amount));
            gens.Add(SoundFontTestBuilder.Gen(GenSampleModes, 1)); // loop
            gens.Add(SoundFontTestBuilder.Gen(GenRootKey, 60));
            gens.Add(SoundFontTestBuilder.Gen(GenSampleId, 0));    // index generator last

            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(gens.ToArray()));
            return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
        }

        private static float[] Render(SoundFontSampler sampler, int frames)
        {
            var buffer = new float[frames * 2];
            sampler.Read(buffer);
            return buffer;
        }

        [Test]
        public void VibratoLfoModulatesPitchPeriodically()
        {
            // A ramp sample so pitch maps to value: with vibrato the read position
            // speeds up and slows down, so the render must diverge from the same
            // instrument rendered with no vibrato generators.
            var withVibrato = new SoundFontSampler(BuildRampMod(
                (GenVibLfoToPitch, unchecked((ushort)(short)600)), // +/-600 cents
                (GenFreqVibLfo, AbsoluteCents(8.0))),               // 8 Hz
                SampleRate, 8);
            var noVibrato = new SoundFontSampler(BuildRampMod(), SampleRate, 8);

            withVibrato.NoteOn(0, 60, 127);
            noVibrato.NoteOn(0, 60, 127);
            var modulated = Render(withVibrato, SampleRate / 4); // 0.25 s ≈ two LFO cycles
            var reference = Render(noVibrato, SampleRate / 4);

            Assert.That(Peak(modulated), Is.GreaterThan(0.05f));
            float divergence = 0;
            for (int i = 0; i < modulated.Length; i++)
                divergence = Math.Max(divergence, Math.Abs(modulated[i] - reference[i]));
            Assert.That(divergence, Is.GreaterThan(0.1f),
                "vibrato should bend the pitch away from the unmodulated render");
        }

        [Test]
        public void TremoloLfoModulatesVolume()
        {
            // modLfoToVolume in centibels: -600 cB swing => audible tremolo.
            // A fast LFO on a constant sample makes the output amplitude pulse.
            var sf = BuildModInstrument(
                (GenModLfoToVolume, unchecked((ushort)(short)(-600))),
                (GenFreqModLfo, AbsoluteCents(10.0))); // 10 Hz
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, 60, 127);
            var output = Render(sampler, SampleRate / 2); // 0.5 s

            // amplitude should vary over the run (tremolo). Skip the attack
            // transient: including the initial ramp from silence would let the
            // assertion pass even with the LFO ignored.
            float min = float.MaxValue, max = 0;
            for (int f = 1000; f < SampleRate / 2; f++)
            {
                float a = Math.Abs(output[f * 2]);
                if (a > max) max = a;
                if (a < min) min = a;
            }
            Assert.That(max, Is.GreaterThan(0.05f));
            Assert.That(max, Is.GreaterThan(min * 2f), "tremolo should swing the level");
        }

        [Test]
        public void ModEnvelopeToFilterBrightensOverAttack()
        {
            // Start with a low base cutoff and a positive mod-env->filter amount
            // with a slow attack: the filter opens over time, so a bright (ramp)
            // sample gets brighter — later RMS should exceed earlier RMS.
            var sf = BuildRampMod(
                (GenInitialFilterFc, AbsoluteCents(300.0)),         // low base cutoff
                (GenModEnvToFilter, unchecked((ushort)(short)6000)),// +6000 cents at full env
                (GenAttackModEnv, Timecents(0.3)));                 // 0.3 s attack
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, 60, 127);
            var output = Render(sampler, SampleRate / 2); // 0.5 s

            float early = Rms(output, 0, output.Length / 8);
            float late = Rms(output, output.Length / 2, output.Length);
            Assert.That(late, Is.GreaterThan(early),
                "filter opening over the attack should let more signal through later");
        }

        [Test]
        public void NoModulationGeneratorsMatchesStaticVoice()
        {
            // an instrument with no modulation generators should still play
            var sf = BuildModInstrument();
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, 60, 127);
            var output = Render(sampler, 1024);
            Assert.That(Peak(output), Is.GreaterThan(0.1f));
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        #region helpers

        // ramp instrument variant so pitch maps to amplitude
        private static NAudio.SoundFont.SoundFont BuildRampMod(params (ushort oper, ushort amount)[] extra)
        {
            const int points = 256;
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++)
            {
                short val = (short)((i - points / 2) * 200); // bright, bipolar ramp
                data[i * 2] = (byte)(val & 0xFF);
                data[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            var gens = new System.Collections.Generic.List<byte[]>();
            foreach (var (oper, amount) in extra)
                gens.Add(SoundFontTestBuilder.Gen(oper, amount));
            gens.Add(SoundFontTestBuilder.Gen(GenSampleModes, 1)); // loop
            gens.Add(SoundFontTestBuilder.Gen(GenRootKey, 60));
            gens.Add(SoundFontTestBuilder.Gen(GenSampleId, 0));
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(gens.ToArray()));
            return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, points, 0, points, SampleRate, 60);
        }

        private static float Peak(float[] buf)
        {
            float p = 0;
            foreach (var s in buf) p = Math.Max(p, Math.Abs(s));
            return p;
        }

        private static bool IsConstant(float[] buf)
        {
            float first = buf[0];
            foreach (var s in buf) if (Math.Abs(s - first) > 1e-6f) return false;
            return true;
        }

        private static float Rms(float[] buf, int start, int end)
        {
            double sum = 0;
            int n = 0;
            for (int i = start; i < end; i++) { sum += buf[i] * (double)buf[i]; n++; }
            return n == 0 ? 0 : (float)Math.Sqrt(sum / n);
        }

        #endregion
    }
}
