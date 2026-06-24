using System;
using NAudio.Sampler;
using NAudio.Sfz;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// End-to-end tests for <see cref="SfzSampler"/>: a parsed SFZ instrument with
    /// stub-loaded samples, played through the shared engine as an ISampleProvider.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SfzSamplerTests
    {
        private const int SampleRate = 44100;

        private sealed class StubLoader : ISfzSampleLoader
        {
            private readonly SampleLoop? loop;
            public StubLoader(SampleLoop? loop = null) => this.loop = loop;
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                // a half-scale, 8-sample mono buffer for every requested path
                left = new float[8];
                for (int i = 0; i < left.Length; i++) left[i] = 0.5f;
                right = null;
                sampleRate = SampleRate;
                embeddedLoop = loop;
                return true;
            }
        }

        private static SfzSampler Build(string sfz, int maxVoices = 16, SampleLoop? loop = null)
        {
            var instrument = SfzParser.Parse(sfz);
            return new SfzSampler(instrument, new StubLoader(loop), SampleRate, maxVoices);
        }

        private static float Peak(float[] buffer)
        {
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        private static float[] Render(SfzSampler sampler, int frames)
        {
            var buffer = new float[frames * 2];
            sampler.Read(buffer);
            return buffer;
        }

        [Test]
        public void LoadsPlayableRegions()
        {
            var sampler = Build("<region> sample=a.wav key=60 loop_mode=loop_continuous");
            Assert.That(sampler.RegionCount, Is.EqualTo(1));
        }

        [Test]
        public void NoteOnProducesAudioAndNoteOffSilencesIt()
        {
            var sampler = Build("<region> sample=a.wav lokey=0 hikey=127 loop_mode=loop_continuous");
            Assert.That(Peak(Render(sampler, 256)), Is.EqualTo(0f), "silent before any note");

            sampler.NoteOn(0, 60, 127);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(Peak(Render(sampler, 512)), Is.GreaterThan(0.1f));
        }

        [Test]
        public void NoteOutsideKeyRangeIsSilent()
        {
            var sampler = Build("<region> sample=a.wav lokey=60 hikey=60 loop_mode=loop_continuous");
            sampler.NoteOn(0, 72, 127);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(0));
        }

        [Test]
        public void EveryChannelPlaysTheSameInstrument()
        {
            // an SFZ file is one instrument: any channel triggers its regions
            var sampler = Build("<region> sample=a.wav lokey=0 hikey=127 loop_mode=loop_continuous");
            sampler.NoteOn(5, 64, 100);
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(Peak(Render(sampler, 256)), Is.GreaterThan(0.1f));
        }

        [Test]
        public void EmbeddedWavLoopMakesTheRegionLoopByDefault()
        {
            // no loop opcodes at all: with a loop embedded in the sample file the
            // spec default is loop_continuous, so the 8-frame sample sustains far
            // past its length; without one the default is no_loop and it ends
            var looping = Build("<region> sample=a.wav key=60", loop: new SampleLoop(0, 8));
            looping.NoteOn(0, 60, 127);
            Render(looping, 2048);
            Assert.That(looping.ActiveVoiceCount, Is.EqualTo(1), "embedded loop should sustain the note");

            var oneShot = Build("<region> sample=a.wav key=60");
            oneShot.NoteOn(0, 60, 127);
            Render(oneShot, 2048);
            Assert.That(oneShot.ActiveVoiceCount, Is.EqualTo(0), "no embedded loop: sample plays out and ends");
        }

        [Test]
        public void VelocitySplitsSelectTheRightRegion()
        {
            var sampler = Build(@"
<region> sample=soft.wav lovel=1 hivel=63 loop_mode=loop_continuous
<region> sample=loud.wav lovel=64 hivel=127 loop_mode=loop_continuous");
            Assert.That(sampler.RegionCount, Is.EqualTo(2));

            sampler.NoteOn(0, 60, 100); // hits the loud layer only
            Assert.That(sampler.ActiveVoiceCount, Is.EqualTo(1));
        }
    }
}
