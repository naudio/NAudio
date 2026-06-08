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
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate)
            {
                left = new float[length];
                for (int i = 0; i < length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
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
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate)
            {
                left = new float[length];
                for (int i = 0; i < length; i++)
                    left[i] = 0.5f * (float)Math.Sin(2 * Math.PI * frequency * i / SampleRate);
                right = null;
                sampleRate = SampleRate;
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
            // a full effect1 send routes through the shared reverb bus, adding wet
            // energy the dry-only render doesn't have
            float withSend = TotalEnergy("<region> sample=a.wav key=60 loop_mode=loop_continuous effect1=100");
            float noSend = TotalEnergy("<region> sample=a.wav key=60 loop_mode=loop_continuous");

            Assert.That(noSend, Is.GreaterThan(0f));
            Assert.That(withSend, Is.GreaterThan(noSend * 1.05f), "the reverb send should add wet energy");
        }

        private static float TotalEnergy(string sfz)
        {
            var sampler = Build(sfz);
            sampler.NoteOn(0, 60, 127);
            var buffer = new float[4096 * 2];
            sampler.Read(buffer);
            float energy = 0;
            foreach (var s in buffer) energy += Math.Abs(s);
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
    }
}
