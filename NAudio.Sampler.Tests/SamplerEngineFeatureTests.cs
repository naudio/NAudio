using System;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// End-to-end tests for the SFZ Tier-1 engine features added on top of the
    /// shared engine: triggers (release/first/legato), one-shot note-off, off_by
    /// choke groups and the high/band-pass filter shapes.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SamplerEngineFeatureTests
    {
        private const int SampleRate = 44100;

        private sealed class ConstantLoader : ISfzSampleLoader
        {
            private readonly int length;
            public ConstantLoader(int length) => this.length = length;
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                left = new float[length];
                for (int i = 0; i < length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
                embeddedLoop = null;
                return true;
            }
        }

        private static SfzSampler Build(string sfz, int length = 64)
        {
            return new SfzSampler(SfzParser.Parse(sfz), new ConstantLoader(length), SampleRate, maxVoices: 16);
        }

        private static void Render(SfzSampler sampler, int frames)
        {
            sampler.Read(new float[frames * 2]);
        }

        [Test]
        public void ReleaseTriggerPlaysOnNoteOffNotNoteOn()
        {
            var sampler = Build("<region> sample=a.wav loop_mode=loop_continuous trigger=release");
            sampler.NoteOn(0, 60, 100);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0), "release region must not sound on note-on");
            sampler.NoteOff(0, 60);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "release region sounds on note-off");
        }

        [Test]
        public void PedalUpReleasesParkedNotesAndFiresReleaseTriggers()
        {
            // with the sustain pedal down a note-off is parked; the *pedal-up* is
            // the real release, so that's when the release sample must fire
            var sampler = Build(
                "<region> sample=a.wav key=60 loop_mode=loop_continuous\n" +
                "<region> sample=a.wav key=60 trigger=release loop_mode=loop_continuous");

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 127));
            sampler.NoteOn(0, 60, 100);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

            sampler.NoteOff(0, 60);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "the pedal defers both the note-off and its release trigger");

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 0));
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(2), "pedal-up releases the note and fires its release trigger");
        }

        [Test]
        public void PedalUpDoesNotKillARestruckHeldKey()
        {
            // pedal down, play-release-replay the same key, pedal up: the second
            // strike's key is still physically down, so it must keep sounding
            var sampler = Build("<region> sample=a.wav key=60 loop_mode=loop_continuous");

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 127));
            sampler.NoteOn(0, 60, 100);
            sampler.NoteOff(0, 60);     // parked by the pedal
            sampler.NoteOn(0, 60, 100); // re-strike supersedes the parked note

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, MidiController.Sustain, 0));
            Render(sampler, 4096); // let the superseded first voice release fully
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "the re-struck key is still held and must keep sounding");
        }

        [Test]
        public void OneShotIgnoresNoteOff()
        {
            // long, non-looped one-shot: note-off must not stop it
            var sampler = Build("<region> sample=a.wav loop_mode=one_shot", length: SampleRate); // 1 s
            sampler.NoteOn(0, 60, 100);
            Render(sampler, 256);
            sampler.NoteOff(0, 60);
            Render(sampler, 256);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "one-shot keeps playing through note-off");
        }

        [Test]
        public void FirstAndLegatoTriggersSelectByHeldNotes()
        {
            var sampler = Build(@"
<region> sample=a.wav loop_mode=loop_continuous trigger=first
<region> sample=b.wav loop_mode=loop_continuous trigger=legato");

            sampler.NoteOn(0, 60, 100); // first note -> 'first' plays, 'legato' does not
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

            sampler.NoteOn(0, 64, 100); // a note is already held -> 'legato' plays, 'first' does not
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(2));
        }

        [Test]
        public void OffByGroupChokesTheOtherGroup()
        {
            // group 1 plays a sustained note; group 2 (off_by=1) silences it when struck
            var sampler = Build(@"
<region> sample=a.wav lokey=60 hikey=60 loop_mode=loop_continuous group=1
<region> sample=b.wav lokey=62 hikey=62 loop_mode=loop_continuous group=2 off_by=1");

            sampler.NoteOn(0, 60, 100);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

            sampler.NoteOn(0, 62, 100); // chokes group 1 (the note-60 voice)
            Render(sampler, 2048);       // past the 5 ms choke fade
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1), "only the group-2 voice should remain");
        }

        // a one-shot sine tone, so a notch at its frequency can be heard to remove it
        private sealed class SineLoader : ISfzSampleLoader
        {
            private readonly double frequency;
            private readonly int length;
            public SineLoader(double frequency, int length) { this.frequency = frequency; this.length = length; }
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                left = new float[length];
                for (int i = 0; i < length; i++)
                    left[i] = 0.5f * (float)Math.Sin(2 * Math.PI * frequency * i / SampleRate);
                right = null;
                sampleRate = SampleRate;
                embeddedLoop = null;
                return true;
            }
        }

        [Test]
        public void BandRejectFilterRemovesItsCentreFrequency()
        {
            // a 441 Hz tone: a notch at 441 Hz removes it; a notch far away passes it
            const double tone = 441.0;
            float removed = SineSteadyPeak($"<region> sample=a.wav key=60 cutoff=441 fil_type=brf_2p", tone);
            float passed = SineSteadyPeak($"<region> sample=a.wav key=60 cutoff=8000 fil_type=brf_2p", tone);

            Assert.That(removed, Is.LessThan(0.1f), "the notch removes a tone at its centre");
            Assert.That(passed, Is.GreaterThan(0.2f), "a notch elsewhere passes the tone");
        }

        [Test]
        public void EqBandBoostsAndCutsItsCentreFrequency()
        {
            const double tone = 1000.0;
            float flat = SineSteadyPeak("<region> sample=a.wav key=60 loop_mode=loop_continuous", tone);
            float boosted = SineSteadyPeak("<region> sample=a.wav key=60 loop_mode=loop_continuous eq1_freq=1000 eq1_gain=12", tone);
            float cut = SineSteadyPeak("<region> sample=a.wav key=60 loop_mode=loop_continuous eq1_freq=1000 eq1_gain=-12", tone);

            Assert.That(boosted, Is.GreaterThan(flat * 1.5f), "+12 dB EQ at the tone boosts it");
            Assert.That(cut, Is.LessThan(flat * 0.75f), "-12 dB EQ at the tone cuts it");
        }

        private static float SineSteadyPeak(string sfz, double frequency)
        {
            var sampler = new SfzSampler(SfzParser.Parse(sfz), new SineLoader(frequency, SampleRate / 2), SampleRate, 8);
            sampler.NoteOn(0, 60, 127);
            var buffer = new float[2048 * 2];
            sampler.Read(buffer); // let the filter settle
            sampler.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        [Test]
        public void ReleaseTriggerDecaysWithHoldTime()
        {
            // a longer hold attenuates the release sample more (rt_decay dB/sec)
            float shortHold = ReleasePeak(0.1);
            float longHold = ReleasePeak(1.0);
            Assert.That(shortHold, Is.GreaterThan(0.05f));
            Assert.That(longHold, Is.LessThan(shortHold * 0.5f), "release should be quieter after a longer hold");
        }

        private static float ReleasePeak(double holdSeconds)
        {
            var sampler = Build("<region> sample=a.wav loop_mode=loop_continuous trigger=release rt_decay=24");
            sampler.NoteOn(0, 60, 127);
            Render(sampler, (int)(holdSeconds * SampleRate)); // hold (silent: release region doesn't sound yet)
            sampler.NoteOff(0, 60);                            // fire the release with rt_decay applied

            var buffer = new float[512 * 2];
            sampler.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        [Test]
        public void OnCcTriggersOnRisingEdgeOnly()
        {
            var sampler = Build("<region> sample=a.wav loop_mode=loop_continuous on_locc20=64 on_hicc20=127");

            sampler.NoteOn(0, 60, 127); // CC-triggered region ignores note-on
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, (MidiController)20, 100)); // rises into range
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, (MidiController)20, 120)); // still in range -> no new trigger
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void Effect1SendAddsReverbWet()
        {
            // a full effect1 send routes through the shared (fully wet) reverb bus:
            // after the dry voice is choked, only the wet tail remains — so the
            // send, and nothing else, produces post-note energy
            float withSend = TailEnergy("<region> sample=a.wav key=60 loop_mode=loop_continuous effect1=100");
            float noSend = TailEnergy("<region> sample=a.wav key=60 loop_mode=loop_continuous");

            Assert.That(withSend, Is.GreaterThan(1e-3f), "the reverb send should add a wet tail");
            Assert.That(withSend, Is.GreaterThan(noSend * 10f), "the tail should come from the send, not the dry path");
        }

        private static float TailEnergy(string sfz)
        {
            var sampler = Build(sfz);
            sampler.NoteOn(0, 60, 127);
            var warm = new float[4096 * 2];
            sampler.Read(warm);          // let the send buffer feed the reverb
            sampler.AllSoundOff();       // choke the dry voice (short fade)

            // render past the choke fade; remaining energy is the wet return only
            var tail = new float[8192 * 2];
            sampler.Read(tail);
            float energy = 0;
            for (int i = tail.Length / 2; i < tail.Length; i++) energy += Math.Abs(tail[i]);
            return energy;
        }

        [Test]
        public void AmpLfoModulatesAmplitude()
        {
            // a constant sample with a deep amp LFO (tremolo) -> the output level
            // swings over the LFO cycle rather than staying flat
            var sampler = Build("<region> sample=a.wav key=60 loop_mode=loop_continuous amplfo_freq=8 amplfo_depth=12");
            sampler.NoteOn(0, 60, 127);

            var buffer = new float[8820 * 2]; // ~200 ms, > one 8 Hz cycle
            sampler.Read(buffer);

            float min = float.MaxValue, max = 0f;
            for (int f = 1000; f < 8820; f++) // skip the attack transient
            {
                float a = Math.Abs(buffer[f * 2]);
                if (a < min) min = a;
                if (a > max) max = a;
            }
            Assert.That(max, Is.GreaterThan(min * 2f), "tremolo should swing the level");
        }

        [Test]
        public void HighPassFilterRemovesDc()
        {
            // a constant (DC) sample through a high-pass settles to ~0; through a
            // low-pass it passes. Confirms the filter shape is actually applied.
            float hp = SteadyStatePeak("<region> sample=a.wav key=60 loop_mode=loop_continuous cutoff=1000 fil_type=hpf_2p");
            float lp = SteadyStatePeak("<region> sample=a.wav key=60 loop_mode=loop_continuous cutoff=1000 fil_type=lpf_2p");

            Assert.That(hp, Is.LessThan(0.02f), "high-pass blocks the DC component");
            Assert.That(lp, Is.GreaterThan(0.1f), "low-pass passes it");
        }

        private static float SteadyStatePeak(string sfz)
        {
            var sampler = Build(sfz);
            sampler.NoteOn(0, 60, 127);
            var buffer = new float[4096 * 2];
            sampler.Read(buffer); // let the filter settle
            sampler.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        [Test]
        public void VoiceStealingFadesTheStolenVoiceInsteadOfHardCutting()
        {
            // A 1-voice pool playing a loud constant looped note: striking a
            // second note steals the voice. The victim's output must ramp out
            // (~5 ms), summed on top of the new note — not jump from the steady
            // level to the new note's envelope-zero start between two samples,
            // which pops audibly under polyphony pressure.
            var sampler = new SfzSampler(
                SfzParser.Parse("<region> sample=a.wav lokey=0 hikey=127 loop_mode=loop_continuous"),
                new ConstantLoader(64), SampleRate, maxVoices: 1);
            sampler.NoteOn(0, 60, 127);
            var warm = new float[4096 * 2];
            sampler.Read(warm);
            float prev = warm[warm.Length - 2]; // last left-channel sample before the steal
            Assert.That(Math.Abs(prev), Is.GreaterThan(0.2f), "the first note should be at a steady level");

            sampler.NoteOn(0, 64, 127); // pool exhausted: steals the sounding voice
            var after = new float[1024 * 2];
            sampler.Read(after);

            float maxStep = 0f;
            for (int f = 0; f < 1024; f++)
            {
                float current = after[f * 2];
                maxStep = Math.Max(maxStep, Math.Abs(current - prev));
                prev = current;
            }
            Assert.That(maxStep, Is.LessThan(0.05f),
                "stealing a sounding voice must not step the output sample-to-sample");
        }

        [Test]
        public void VoiceStealingPrefersTheLessAudibleVoice()
        {
            // The steal ranking must reflect audibility (envelope output x static
            // gain): with a full pool holding one loud and one heavily attenuated
            // voice, the quiet voice is stolen and the loud note keeps sounding.
            // Ranking by envelope output alone ties the two sustained voices and
            // steals the older — the loud — note instead.
            var sfz =
                "<region> sample=a.wav lokey=60 hikey=60 loop_mode=loop_continuous\n" +
                "<region> sample=a.wav lokey=62 hikey=62 loop_mode=loop_continuous volume=-40\n" +
                "<region> sample=a.wav lokey=64 hikey=64 loop_mode=loop_continuous volume=-40";
            var sampler = new SfzSampler(SfzParser.Parse(sfz), new ConstantLoader(64), SampleRate, maxVoices: 2);
            sampler.NoteOn(0, 60, 127); // loud
            sampler.NoteOn(0, 62, 127); // quiet (-40 dB)
            Render(sampler, 2048);      // both at sustain

            sampler.NoteOn(0, 64, 127); // pool full: must steal the quiet voice
            var output = new float[2048 * 2];
            sampler.Read(output);
            float tail = Math.Abs(output[output.Length - 2]);
            Assert.That(tail, Is.GreaterThan(0.2f), "the loud voice must survive the steal");
        }

        [Test]
        public void ResonanceBoostIsGainCompensated()
        {
            // SF2.04 §8.1.2 gen 8: the voice attenuates its output by half the
            // resonance dB, so a resonant peak at the cutoff rises by only half
            // the nominal resonance over the level. A 441 Hz tone at a 441 Hz
            // cutoff with resonance=20 dB peaks ~Q (+17 dB) but is compensated
            // by -10 dB; against the Butterworth (-3 dB) flat response that is
            // ~+10 dB (~3.2x), well under the uncompensated ~10x.
            const double tone = 441.0;
            float flat = SineSteadyPeak("<region> sample=a.wav key=60 cutoff=441", tone);
            float resonant = SineSteadyPeak("<region> sample=a.wav key=60 cutoff=441 resonance=20", tone);

            Assert.That(resonant, Is.GreaterThan(flat * 1.5f), "resonance at the tone should still boost it");
            Assert.That(resonant, Is.LessThan(flat * 5f),
                "half the resonance dB must be compensated out of the voice gain");
        }

        [Test]
        public void MalformedHugeResonanceStaysFiniteAndBounded()
        {
            // resonance=2000 (20000 cB) is far outside the SF2 0..960 cB range:
            // the clamp must keep Q finite (alpha > 0) so the filter neither
            // rings forever nor produces NaN/Inf, even driven right at cutoff
            var sampler = new SfzSampler(
                SfzParser.Parse("<region> sample=a.wav key=60 cutoff=441 resonance=2000"),
                new SineLoader(441.0, SampleRate / 2), SampleRate, 8);
            sampler.NoteOn(0, 60, 127);

            var buffer = new float[4096 * 2];
            float peak = 0;
            bool finite = true;
            for (int block = 0; block < 2; block++)
            {
                sampler.Read(buffer);
                foreach (var s in buffer)
                {
                    finite &= float.IsFinite(s);
                    peak = Math.Max(peak, Math.Abs(s));
                }
            }
            Assert.That(finite, Is.True, "the filtered output must stay finite");
            Assert.That(peak, Is.LessThan(2f), "a clamped, gain-compensated Q must not blow the level up");
        }
    }
}
