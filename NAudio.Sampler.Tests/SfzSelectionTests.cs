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
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate)
            {
                left = new float[64];
                for (int i = 0; i < left.Length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
                return true;
            }
        }

        private static SfzSampler Build(string sfz) =>
            new SfzSampler(SfzParser.Parse(sfz), new ConstantLoader(), SampleRate, maxVoices: 32);

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
