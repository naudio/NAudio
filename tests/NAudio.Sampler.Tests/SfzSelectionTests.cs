using System;
using NAudio.Midi;
using NAudio.Sampler;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for the SFZ Tier-2 note-on selection: the per-region gate predicates
    /// (random / CC / round-robin) and the engine-level keyswitch, round-robin and
    /// CC-gating behaviour driven through <see cref="SfzSampler"/>.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SfzSelectionTests
    {
        private const int SampleRate = 44100;

        private sealed class ConstantLoader : ISfzSampleLoader
        {
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                left = new float[64];
                for (int i = 0; i < left.Length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
                embeddedLoop = null;
                return true;
            }
        }

        private static SfzSampler Build(string sfz) =>
            new SfzSampler(SfzParser.Parse(sfz), new ConstantLoader(), SampleRate, maxVoices: 32);

        private static void Render(SfzSampler sampler, int frames) =>
            sampler.Read(new float[frames * 2]);

        // ---- gate predicates (unit) ----

        [Test]
        public void PassesRandomWindow()
        {
            var region = new SamplerRegion { LowRandom = 0.25f, HighRandom = 0.75f };
            Assert.That(region.PassesRandom(0.5), Is.True);
            Assert.That(region.PassesRandom(0.1), Is.False);
            Assert.That(region.PassesRandom(0.75), Is.False); // upper bound exclusive

            // default (0,0) means "no random gate" -> always passes
            Assert.That(new SamplerRegion().PassesRandom(0.99), Is.True);
        }

        [Test]
        public void PassesSequenceRotates()
        {
            var first = new SamplerRegion { SequenceLength = 2, SequencePosition = 1 };
            var second = new SamplerRegion { SequenceLength = 2, SequencePosition = 2 };
            Assert.That(new[] { first.PassesSequence(), first.PassesSequence(), first.PassesSequence() },
                Is.EqualTo(new[] { true, false, true }));
            Assert.That(new[] { second.PassesSequence(), second.PassesSequence(), second.PassesSequence() },
                Is.EqualTo(new[] { false, true, false }));
        }

        [Test]
        public void PassesCcGatesChecksControllerValue()
        {
            var region = new SamplerRegion { CcGates = new[] { (1, 64, 127) } };
            var channel = new MidiChannelState();
            channel.SetController(1, 100);
            Assert.That(region.PassesCcGates(channel), Is.True);
            channel.SetController(1, 10);
            Assert.That(region.PassesCcGates(channel), Is.False);
        }

        // ---- engine selection (integration) ----

        [Test]
        public void KeyswitchGatesAndMakesNoSound()
        {
            // region only sounds once keyswitch 0 has been pressed; key 0 is a switch
            var sampler = Build("<region> sample=a.wav lokey=36 hikey=96 loop_mode=loop_continuous sw_lokey=0 sw_hikey=1 sw_last=0");

            sampler.NoteOn(0, 60, 100); // before any switch -> gated out
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.NoteOn(0, 0, 100);  // press the keyswitch -> no sound
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.NoteOn(0, 60, 100); // now the articulation is selected
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void RoundRobinFiresExactlyOneVariantPerNote()
        {
            var sampler = Build(@"
<region> sample=a.wav lokey=60 hikey=60 loop_mode=loop_continuous seq_length=2 seq_position=1
<region> sample=b.wav lokey=60 hikey=60 loop_mode=loop_continuous seq_length=2 seq_position=2");

            for (int i = 0; i < 4; i++) sampler.NoteOn(0, 60, 100);

            // exactly one of the two variants per note-on (not both, not zero)
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(4));
        }

        // ---- release-trigger selection gates ----

        [Test]
        public void ReleaseTriggersHonourTheActiveKeyswitch()
        {
            // a keyswitched instrument: each articulation carries its own release
            // sample, and a note-off must fire only the ACTIVE articulation's —
            // not every articulation's on every note-off
            var sampler = Build(@"
<region> sample=a.wav key=60 sw_lokey=0 sw_hikey=1 sw_last=0
<region> sample=a_rel.wav key=60 sw_lokey=0 sw_hikey=1 sw_last=0 trigger=release loop_mode=loop_continuous
<region> sample=b.wav key=60 sw_lokey=0 sw_hikey=1 sw_last=1
<region> sample=b_rel.wav key=60 sw_lokey=0 sw_hikey=1 sw_last=1 trigger=release loop_mode=loop_continuous");

            sampler.NoteOn(0, 0, 100);  // select articulation 0 (keyswitch, silent)
            sampler.NoteOn(0, 60, 100);
            Render(sampler, 2048);      // the short non-looped attack sample plays out
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.NoteOff(0, 60);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1),
                "only the active articulation's release sample fires");
        }

        [Test]
        public void ReleaseTriggersHonourCcGates()
        {
            // the CC gate is read against the channel state at note-off time
            var sampler = Build(
                "<region> sample=a.wav key=60\n" +
                "<region> sample=rel.wav key=60 trigger=release locc20=64 hicc20=127 loop_mode=loop_continuous");

            sampler.NoteOn(0, 60, 100);
            sampler.NoteOff(0, 60); // CC20 defaults to 0 -> the release region is gated out
            Render(sampler, 4096);  // the released attack voice dies away
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0),
                "a CC-gated release region must not fire while its gate fails");

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, (MidiController)20, 100));
            sampler.NoteOn(0, 60, 100);
            sampler.NoteOff(0, 60); // the gate now holds at note-off
            Render(sampler, 4096);  // the attack tail dies; the looped release voice remains
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseTriggersAdvanceRoundRobin()
        {
            // two release variants in a 2-step round-robin: each note-off fires
            // exactly one of them, and successive note-offs alternate (the second
            // variant is -20 dB so the alternation is audible in the peak)
            var sampler = Build(@"
<region> sample=a.wav key=60
<region> sample=r1.wav key=60 trigger=release seq_length=2 seq_position=1 loop_mode=loop_continuous
<region> sample=r2.wav key=60 trigger=release seq_length=2 seq_position=2 volume=-20 loop_mode=loop_continuous");

            float first = ReleaseCyclePeak(sampler);  // fires seq_position=1 (full level)
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1),
                "exactly one release variant per note-off");
            sampler.AllSoundOff();
            Render(sampler, 2048); // past the choke fade

            float second = ReleaseCyclePeak(sampler); // fires seq_position=2 (-20 dB)
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(second, Is.LessThan(first * 0.5f),
                "successive note-offs must alternate the release variants");
        }

        // plays and releases key 60, returning the post-note-off peak (the short
        // non-looped attack sample has already played out, so only the looped
        // release voice sounds in the measured block)
        private static float ReleaseCyclePeak(SfzSampler sampler)
        {
            sampler.NoteOn(0, 60, 127);
            Render(sampler, 1024);
            sampler.NoteOff(0, 60);
            var buffer = new float[2048 * 2];
            sampler.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        // ---- <control> set_ccN initial controller values ----

        [Test]
        public void SetCcSeedsControllerStateSoCcGatedRegionsSoundByDefault()
        {
            // locc/hicc gates read controller values that default to 0, so a
            // gated region is silent until set_ccN seeds the channel state
            var sampler = Build(
                "<control> set_cc20=100\n" +
                "<region> sample=a.wav loop_mode=loop_continuous locc20=50 hicc20=127");

            sampler.NoteOn(0, 60, 100);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1),
                "set_cc20=100 must satisfy the locc20=50/hicc20=127 gate at load");
        }

        [Test]
        public void SetCcDoesNotFireOnCcTriggerRegionsAtLoad()
        {
            // the initial values are applied silently to the channel state —
            // visible to gates and modulator sources, but not a controller
            // *change*, so they must not edge-fire on_locc/on_hicc regions
            var sampler = Build(
                "<control> set_cc20=100\n" +
                "<region> sample=a.wav loop_mode=loop_continuous on_locc20=64 on_hicc20=127");

            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0),
                "seeding CC20 inside the trigger window must not fire the region at load");
        }

        // ---- crossfades ----

        [Test]
        public void NoCrossfadeIsUnityGain()
        {
            Assert.That(new SamplerRegion().CrossfadeGain(60, 100), Is.EqualTo(1f));
        }

        [Test]
        public void VelocityCrossfadeInLinearRamps()
        {
            var r = new SamplerRegion
            {
                VelocityFadeInLow = 0,
                VelocityFadeInHigh = 100,
                VelocityFadeCurve = SamplerCrossfadeCurve.Linear
            };
            Assert.That(r.CrossfadeGain(60, 0), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(r.CrossfadeGain(60, 50), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(r.CrossfadeGain(60, 100), Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void PowerCrossfadesSumToConstantPower()
        {
            // two layers crossfading over the same velocity span with the power curve
            var fadingOut = new SamplerRegion { VelocityFadeOutLow = 0, VelocityFadeOutHigh = 100, VelocityFadeCurve = SamplerCrossfadeCurve.Power };
            var fadingIn = new SamplerRegion { VelocityFadeInLow = 0, VelocityFadeInHigh = 100, VelocityFadeCurve = SamplerCrossfadeCurve.Power };

            for (int v = 0; v <= 100; v += 10)
            {
                float a = fadingOut.CrossfadeGain(60, v);
                float b = fadingIn.CrossfadeGain(60, v);
                Assert.That(a * a + b * b, Is.EqualTo(1f).Within(1e-5f), $"constant power at velocity {v}");
            }
        }

        [Test]
        public void CrossfadeGatesAndAttenuatesByVelocity()
        {
            var sampler = Build("<region> sample=a.wav loop_mode=loop_continuous xfin_lovel=64 xfin_hivel=127 xf_velcurve=gain");

            sampler.NoteOn(0, 60, 32);  // below the fade-in -> gain 0 -> no voice
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.NoteOn(0, 62, 127); // at the top -> full gain -> sounds
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void CcGatingControlsWhetherARegionSounds()
        {
            var sampler = Build("<region> sample=a.wav loop_mode=loop_continuous locc1=64 hicc1=127");

            sampler.NoteOn(0, 60, 100); // CC1 defaults to 0 -> gated out
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));

            sampler.ProcessMidiEvent(new ControlChangeEvent(0, 1, (MidiController)1, 100));
            sampler.NoteOn(0, 62, 100); // CC1 now in range
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }
    }
}
