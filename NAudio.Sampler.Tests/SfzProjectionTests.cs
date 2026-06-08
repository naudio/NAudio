using System;
using NAudio.Sampler;
using NAudio.Sfz;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for projecting parsed/mapped SFZ regions onto the neutral
    /// <see cref="SamplerRegion"/>, and playing them through the shared voice.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SfzProjectionTests
    {
        private const int SampleRate = 44100;

        // a loader that hands back a fixed buffer for any path
        private sealed class StubLoader : ISfzSampleLoader
        {
            private readonly float[] data;
            private readonly int rate;
            public StubLoader(float[] data, int rate) { this.data = data; this.rate = rate; }
            public bool TryLoad(string path, out float[] d, out int sampleRate)
            {
                d = data; sampleRate = rate; return data != null;
            }
        }

        private static StubLoader ConstantSample(float value = 0.5f, int length = 8) =>
            new StubLoader(Filled(value, length), SampleRate);

        private static float[] Filled(float value, int length)
        {
            var a = new float[length];
            for (int i = 0; i < length; i++) a[i] = value;
            return a;
        }

        private static SamplerRegion ProjectFirst(string sfz, ISfzSampleLoader loader)
        {
            var instrument = SfzParser.Parse(sfz);
            return SfzRegionProjector.Project(instrument.MapRegions()[0], loader);
        }

        [Test]
        public void VolumeMapsToAttenuationAndPanToTenthsOfPercent()
        {
            var r = ProjectFirst("<region> sample=a.wav volume=-6 pan=50", ConstantSample());
            Assert.That(r.Generators[GeneratorEnum.InitialAttenuation], Is.EqualTo(60)); // -(-6)*10
            Assert.That(r.Generators[GeneratorEnum.Pan], Is.EqualTo(250));               // 0.5 * 500
        }

        [Test]
        public void TransposeMapsToCoarseTuneAndKeytrackToScaleTuning()
        {
            var r = ProjectFirst("<region> sample=a.wav transpose=12 pitch_keytrack=50", ConstantSample());
            Assert.That(r.Generators[GeneratorEnum.CoarseTune], Is.EqualTo(12));
            Assert.That(r.Generators[GeneratorEnum.ScaleTuning], Is.EqualTo(50));
        }

        [Test]
        public void LoopModeAndGroupsMap()
        {
            var r = ProjectFirst("<region> sample=a.wav loop_mode=loop_continuous group=1 off_by=1", ConstantSample());
            Assert.That(r.Generators[GeneratorEnum.SampleModes], Is.EqualTo((short)SampleMode.LoopContinuously));
            Assert.That(r.Group, Is.EqualTo(1));
            Assert.That(r.OffByGroup, Is.EqualTo(1));
        }

        [Test]
        public void CrossGroupOffByIsPreservedDirectionally()
        {
            var r = ProjectFirst("<region> sample=a.wav group=1 off_by=2", ConstantSample());
            Assert.That(r.Group, Is.EqualTo(1));
            Assert.That(r.OffByGroup, Is.EqualTo(2));
        }

        [Test]
        public void FilterAndTriggerMap()
        {
            var hp = ProjectFirst("<region> sample=a.wav cutoff=500 fil_type=hpf_2p", ConstantSample());
            Assert.That(hp.FilterType, Is.EqualTo(SamplerFilterType.HighPass));

            var rel = ProjectFirst("<region> sample=a.wav trigger=release", ConstantSample());
            Assert.That(rel, Is.Not.Null);
            Assert.That(rel.Trigger, Is.EqualTo(SamplerTrigger.Release));

            var oneShot = ProjectFirst("<region> sample=a.wav loop_mode=one_shot", ConstantSample());
            Assert.That(oneShot.IgnoreNoteOff, Is.True);
        }

        [Test]
        public void RangesAndVelocityTrackingCarryThrough()
        {
            var r = ProjectFirst("<region> sample=a.wav lokey=48 hikey=72 lovel=20 hivel=100 amp_veltrack=80",
                ConstantSample());
            Assert.That(r.LoKey, Is.EqualTo(48));
            Assert.That(r.HiKey, Is.EqualTo(72));
            Assert.That(r.LoVelocity, Is.EqualTo(20));
            Assert.That(r.HiVelocity, Is.EqualTo(100));
            Assert.That(r.VelocityTrackingPercent, Is.EqualTo(80f));
        }

        [Test]
        public void OffsetSetsSampleStart()
        {
            var r = ProjectFirst("<region> sample=a.wav offset=3", ConstantSample(length: 8));
            Assert.That(r.Sample.Start, Is.EqualTo(3));
            Assert.That(r.Sample.End, Is.EqualTo(8));
        }


        [Test]
        public void RegionWithoutSampleIsSkipped()
        {
            Assert.That(ProjectFirst("<region> lokey=60", ConstantSample()), Is.Null);
        }

        [Test]
        public void MissingSampleFileIsSkipped()
        {
            var loader = new StubLoader(null, 0); // TryLoad returns false
            Assert.That(ProjectFirst("<region> sample=missing.wav", loader), Is.Null);
        }

        // ---- play a projected region through the shared voice ----

        private static float RenderPeak(SamplerRegion region, int note, int velocity, int frames = 512)
        {
            var voice = new SamplerVoice(SampleRate);
            var channel = new MidiChannelState();
            Assert.That(voice.Start(region, channel, 0, note, velocity, 0), Is.True);

            var buffer = new float[frames * 2];
            var send = new float[frames * 2];
            voice.Mix(buffer, send, new float[frames * 2], frames, channel);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }

        [Test]
        public void ProjectedRegionPlaysAudio()
        {
            var r = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous", ConstantSample(0.5f));
            Assert.That(RenderPeak(r, 60, 127), Is.GreaterThan(0.1f));
        }

        [Test]
        public void AmpVelTrackMakesHighVelocityLouder()
        {
            var r = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous amp_veltrack=100",
                ConstantSample(0.5f));
            float loud = RenderPeak(r, 60, 127);
            float soft = RenderPeak(r, 60, 40);
            Assert.That(loud, Is.GreaterThan(soft * 2f));
        }
    }
}
