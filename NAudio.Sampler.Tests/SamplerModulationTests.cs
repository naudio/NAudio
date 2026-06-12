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
        public void VibratoStartsFromZeroAfterItsDelay()
        {
            // SF2.04 §8.1.2 (gens 21/23): when the vibrato LFO's delay expires it
            // "begins its upward ramp from zero", so the pitch modulation must
            // grow gradually from the delay point — not step instantly to full
            // depth (which clicks on every delayed-vibrato note). A smooth looped
            // sine sample maps read position to value, so a pitch offset shows up
            // as divergence from the vibrato-free render.
            const double delaySeconds = 0.05;
            var withVibrato = new SoundFontSampler(BuildSineMod(
                (GenVibLfoToPitch, unchecked((ushort)(short)600)), // +/-600 cents
                (GenFreqVibLfo, AbsoluteCents(5.0)),               // 5 Hz
                (GenDelayVibLfo, Timecents(delaySeconds))), SampleRate, 8);
            var noVibrato = new SoundFontSampler(BuildSineMod(), SampleRate, 8);

            withVibrato.NoteOn(0, 60, 127);
            noVibrato.NoteOn(0, 60, 127);
            int delayFrames = (int)(delaySeconds * SampleRate) + 16; // just past expiry (+ timecent rounding slack)
            int total = delayFrames + 1400;
            var modulated = Render(withVibrato, total);
            var reference = Render(noVibrato, total);

            float MaxDiff(int from, int to)
            {
                float d = 0;
                for (int f = from; f < to; f++)
                    d = Math.Max(d, Math.Abs(modulated[f * 2] - reference[f * 2]));
                return d;
            }

            Assert.That(MaxDiff(0, delayFrames - 32), Is.LessThan(1e-6f),
                "no pitch modulation during the vibrato delay");
            Assert.That(MaxDiff(delayFrames, delayFrames + 128), Is.LessThan(0.1f),
                "right after the delay the LFO must still be near zero — no instant full-depth pitch step");
            Assert.That(MaxDiff(delayFrames, total), Is.GreaterThan(0.1f),
                "the vibrato should grow to an audible pitch bend over the following cycle");
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
        public void KeynumToVolEnvDecayLengthensDecayForLowKeys()
        {
            // SF2.04 §8.1.2 gen 40 (keynumToVolEnvDecay): the decay time scales
            // by 2^(amount x (60 - key) / 1200), key 60 neutral. At 50 tc/key,
            // key 36 decays over twice the base time and key 84 over half — the
            // piano "bass notes ring longer" behaviour — so in a fixed window
            // after note-on the low key's tail still sounds when the high key's
            // is gone. (Constant looped sample: pitch does not affect level.)
            float low = DecayTailEnergy(36);
            float high = DecayTailEnergy(84);
            Assert.That(low, Is.GreaterThan(high * 5f),
                "a low key must decay audibly longer than a high key under keynumToVolEnvDecay");
        }

        private static float DecayTailEnergy(int note)
        {
            // a decay-only voice: sustain fully attenuated, base decay 0.2 s,
            // keynumToVolEnvDecay = 50 timecents per key number
            var sf = BuildModInstrument(
                (36, Timecents(0.2)), // decayVolEnv
                (37, 1440),           // sustainVolEnv: silence
                (40, 50));            // keynumToVolEnvDecay
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, note, 127);
            int frames = SampleRate / 4; // 0.25 s
            var output = Render(sampler, frames);

            float energy = 0;
            for (int f = (int)(0.15 * SampleRate); f < frames; f++)
                energy += Math.Abs(output[f * 2]);
            return energy;
        }

        [Test]
        public void KeynumToVolEnvHoldLengthensHoldForLowKeys()
        {
            // SF2.04 §8.1.2 gen 39 (keynumToVolEnvHold): the hold time scales by
            // 2^(50 x (60 - key) / 1200). With a fast decay to a silent sustain,
            // the audible note length is essentially the hold time: ~0.1 s for
            // key 36 against ~0.025 s for key 84.
            int low = FramesAboveHalfLevel(36);
            int high = FramesAboveHalfLevel(84);
            Assert.That(low, Is.GreaterThan(high * 2),
                "a low key must hold longer than a high key under keynumToVolEnvHold");
        }

        private static int FramesAboveHalfLevel(int note)
        {
            var sf = BuildModInstrument(
                (35, Timecents(0.05)),                  // holdVolEnv: 50 ms base
                (36, unchecked((ushort)(short)-7200)),  // decayVolEnv ~16 ms
                (37, 1440),                             // sustainVolEnv: silence
                (39, 50));                              // keynumToVolEnvHold
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, note, 127);
            var output = Render(sampler, SampleRate / 5); // 0.2 s

            int frames = 0;
            for (int f = 0; f < output.Length / 2; f++)
                if (Math.Abs(output[f * 2]) > 0.17f) frames++; // ~half the 0.35 steady level
            return frames;
        }

        [Test]
        public void KeynumToModEnvHoldScalesTheFilterEnvelope()
        {
            // SF2.04 §8.1.2 gen 31 (keynumToModEnvHold), observed through the
            // filter: the mod envelope holds the cutoff open (+6000 cents over a
            // 50 Hz base) for a hold time scaled by key number; scaleTuning=0
            // keeps the rendered tone identical across keys. In the 0.10-0.15 s
            // window key 36 (hold x2 = 0.2 s) is still bright while key 84's
            // envelope (hold /2 = 0.05 s, then a 0.05 s decay to zero) has fully
            // closed the filter onto the tone.
            float low = LateToneRms(36);
            float high = LateToneRms(84);
            Assert.That(low, Is.GreaterThan(high * 3f),
                "a low key must hold the filter open longer under keynumToModEnvHold");
        }

        private static float LateToneRms(int note)
        {
            var sf = BuildSineMod(
                (GenInitialFilterFc, AbsoluteCents(50.0)),          // low base cutoff
                (GenModEnvToFilter, unchecked((ushort)(short)6000)),// +6000 cents at full env
                (27, Timecents(0.1)),                               // holdModEnv: 100 ms base
                (28, Timecents(0.05)),                              // decayModEnv: 50 ms
                (29, 1000),                                         // sustainModEnv -> 0
                (31, 50),                                           // keynumToModEnvHold
                (56, 0));                                           // scaleTuning 0: same tone per key
            var sampler = new SoundFontSampler(sf, SampleRate, 8);
            sampler.NoteOn(0, note, 127);
            var output = Render(sampler, (int)(0.15 * SampleRate));
            return Rms(output, (int)(0.10 * SampleRate) * 2, output.Length);
        }

        [Test]
        public void FileModulatorOnOpenCutoffEngagesTheFilterWhenTheCcDrops()
        {
            // A region whose base cutoff is fully open (the 13500-cent default)
            // but which carries a file modulator CC74 -> initialFilterFc
            // (negative direction, -9600 cents at CC74 = 0): the open cutoff
            // alone would bypass the filter, but the channel-driven routing must
            // keep it engaged so dropping CC74 darkens the tone. This was a real
            // bug — the activation rule only looked at the cutoff and the
            // LFO/mod-env generators, so a brightness CC on an open-cutoff region
            // never engaged the filter. The 32 kHz output rate puts the open
            // cutoff (~19.9 kHz) beyond the old rule's Nyquist guard, which is
            // where the old code failed (at 44.1 kHz it scraped by only because
            // the lax guard engaged the filter on every open-cutoff voice).
            const int outputRate = 32000;
            const int points = 256;
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++)
            {
                short val = (short)(16000 * Math.Sin(2 * Math.PI * i / points)); // ~172 Hz tone
                data[i * 2] = (byte)(val & 0xFF);
                data[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(GenSampleModes, 1),
                SoundFontTestBuilder.Gen(GenRootKey, 60),
                SoundFontTestBuilder.Gen(GenSampleId, 0)));
            // source 0x01CA: CC74, unipolar, linear, max-to-min (127 -> 0, 0 -> 1)
            var cc74ToCutoff = SoundFontTestBuilder.Mod(0x01CA, GenInitialFilterFc, -9600);
            var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, points, 0, points,
                SampleRate, 60, instrumentModulators: cc74ToCutoff);

            var sampler = new SoundFontSampler(sf, outputRate, 8);
            sampler.ProcessMidiEvent(new NAudio.Midi.ControlChangeEvent(0, 1, (NAudio.Midi.MidiController)74, 127));
            sampler.NoteOn(0, 60, 127); // full velocity: no velocity->cutoff attenuation either

            var bright = Render(sampler, outputRate / 4);
            sampler.ProcessMidiEvent(new NAudio.Midi.ControlChangeEvent(0, 1, (NAudio.Midi.MidiController)74, 0));
            var dark = Render(sampler, outputRate / 4);

            // skip the first half of each window (attack / filter settling)
            float brightRms = Rms(bright, bright.Length / 2, bright.Length);
            float darkRms = Rms(dark, dark.Length / 2, dark.Length);
            Assert.That(brightRms, Is.GreaterThan(0.1f), "the open-cutoff note should pass the tone");
            Assert.That(darkRms, Is.LessThan(brightRms * 0.5f),
                "dropping CC74 must darken the note through the file modulator");
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

        // looped single-cycle sine instrument: smooth across the loop seam, so a
        // small read-position offset produces only a small output difference
        // (unlike the ramp, whose loop wrap is a discontinuity)
        private static NAudio.SoundFont.SoundFont BuildSineMod(params (ushort oper, ushort amount)[] extra)
        {
            const int points = 256;
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++)
            {
                short val = (short)(16000 * Math.Sin(2 * Math.PI * i / points));
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
