using System;
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
