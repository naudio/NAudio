using System;
using NAudio.Dsp;
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

        // a loader that hands back fixed buffers (and optional embedded loop
        // points, standing in for a WAV smpl chunk) for any path
        private sealed class StubLoader : ISfzSampleLoader
        {
            private readonly float[] data;
            private readonly float[] dataRight;
            private readonly int rate;
            private readonly SampleLoop? loop;
            public StubLoader(float[] data, int rate, float[] dataRight = null, SampleLoop? loop = null)
            {
                this.data = data; this.rate = rate; this.dataRight = dataRight; this.loop = loop;
            }
            public bool TryLoad(string path, out float[] left, out float[] right, out int sampleRate,
                out SampleLoop? embeddedLoop)
            {
                left = data; right = dataRight; sampleRate = rate; embeddedLoop = loop;
                return data != null;
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
        public void EndOpcodeIsInclusive()
        {
            // SFZ `end` plays up to AND INCLUDING that sample; SampleData.End is
            // exclusive, so end=3 projects to End=4
            var r = ProjectFirst("<region> sample=a.wav end=3", ConstantSample(length: 8));
            Assert.That(r.Sample.End, Is.EqualTo(4));
        }

        [Test]
        public void EndMinusOneDisablesTheRegion()
        {
            // an explicit end=-1 means "this region is not played" per the spec
            // (distinct from an absent `end`, which plays the whole sample)
            Assert.That(ProjectFirst("<region> sample=a.wav end=-1", ConstantSample(length: 8)), Is.Null);
        }

        [Test]
        public void LoopOpcodesAreInclusiveEnd()
        {
            var r = ProjectFirst("<region> sample=a.wav loop_mode=loop_continuous loop_start=1 loop_end=4",
                ConstantSample(length: 8));
            Assert.That(r.Sample.LoopStart, Is.EqualTo(1));
            Assert.That(r.Sample.LoopEnd, Is.EqualTo(5), "loop_end=4 is inclusive -> exclusive 5");
        }

        [Test]
        public void EmbeddedWavLoopSuppliesDefaultLoopPointsAndLoopMode()
        {
            // a loop authored in the WAV's smpl chunk supplies the loop points,
            // and per the spec an absent loop_mode then defaults to loop_continuous
            var loader = new StubLoader(Filled(0.5f, 8), SampleRate, loop: new SampleLoop(2, 6));
            var r = ProjectFirst("<region> sample=a.wav", loader);
            Assert.That(r.Sample.LoopStart, Is.EqualTo(2));
            Assert.That(r.Sample.LoopEnd, Is.EqualTo(6));
            Assert.That(r.Generators[GeneratorEnum.SampleModes], Is.EqualTo((short)SampleMode.LoopContinuously));
        }

        [Test]
        public void LoopModeDefaultsToNoLoopWithoutAnEmbeddedLoop()
        {
            var r = ProjectFirst("<region> sample=a.wav", ConstantSample(length: 8));
            Assert.That(r.Generators[GeneratorEnum.SampleModes], Is.EqualTo((short)SampleMode.NoLoop));
        }

        [Test]
        public void ExplicitNoLoopOverridesAnEmbeddedLoop()
        {
            var loader = new StubLoader(Filled(0.5f, 8), SampleRate, loop: new SampleLoop(2, 6));
            var r = ProjectFirst("<region> sample=a.wav loop_mode=no_loop", loader);
            Assert.That(r.Generators[GeneratorEnum.SampleModes], Is.EqualTo((short)SampleMode.NoLoop));
        }

        [Test]
        public void LoopOpcodesOverrideTheEmbeddedLoop()
        {
            var loader = new StubLoader(Filled(0.5f, 8), SampleRate, loop: new SampleLoop(2, 6));
            var r = ProjectFirst("<region> sample=a.wav loop_start=1 loop_end=4", loader);
            Assert.That(r.Sample.LoopStart, Is.EqualTo(1));
            Assert.That(r.Sample.LoopEnd, Is.EqualTo(5), "inclusive loop_end=4 beats the embedded loop");
        }

        [Test]
        public void ControlNoteOffsetTransposesIncomingNotes()
        {
            // note_offset transposes incoming MIDI notes UP, so the projected
            // region's key range and root key shift DOWN: with note_offset=12 a
            // played key 48 matches this region and sounds at the recording's
            // pitch (i.e. the instrument sounds an octave higher)
            var instrument = SfzParser.Parse("<control> note_offset=12\n<region> sample=a.wav key=c4");
            var r = SfzRegionProjector.Project(instrument.MapRegions()[0], ConstantSample());
            Assert.That(r.LoKey, Is.EqualTo(48));
            Assert.That(r.HiKey, Is.EqualTo(48));
            Assert.That(r.Sample.RootKey, Is.EqualTo(48));
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
        public void PitchLfoMapsToVibrato()
        {
            var g = ProjectFirst("<region> sample=a.wav pitchlfo_freq=5 pitchlfo_depth=50", ConstantSample()).Generators;
            Assert.That(g[GeneratorEnum.VibratoLFOToPitch], Is.EqualTo(50));
            Assert.That(g[GeneratorEnum.FrequencyVibratoLFO],
                Is.EqualTo((short)Math.Round(SynthMath.HertzToAbsoluteCents(5))));
        }

        [Test]
        public void AmpLfoMapsToModulationLfoVolume()
        {
            var g = ProjectFirst("<region> sample=a.wav amplfo_freq=4 amplfo_depth=6", ConstantSample()).Generators;
            Assert.That(g[GeneratorEnum.ModulationLFOToVolume], Is.EqualTo(60)); // 6 dB -> 60 cB
            Assert.That(g[GeneratorEnum.FrequencyModulationLFO],
                Is.EqualTo((short)Math.Round(SynthMath.HertzToAbsoluteCents(4))));
        }

        [Test]
        public void FilterEgMapsToModulationEnvelopeFilter()
        {
            var g = ProjectFirst("<region> sample=a.wav fileg_depth=1200 fileg_attack=0.1 fileg_sustain=50", ConstantSample()).Generators;
            Assert.That(g[GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency], Is.EqualTo(1200));
            Assert.That(g[GeneratorEnum.SustainModulationEnvelope], Is.EqualTo(500)); // 1000 - 10*50
            Assert.That(g[GeneratorEnum.AttackModulationEnvelope],
                Is.EqualTo((short)Math.Round(SynthMath.SecondsToTimecents(0.1))));
        }

        [Test]
        public void PitchEgMapsToModulationEnvelopePitch()
        {
            var g = ProjectFirst("<region> sample=a.wav pitcheg_depth=200", ConstantSample()).Generators;
            Assert.That(g[GeneratorEnum.ModulationEnvelopeToPitch], Is.EqualTo(200));
        }

        [Test]
        public void RtDecayAndOnCcTriggerMap()
        {
            var decay = ProjectFirst("<region> sample=a.wav trigger=release rt_decay=18", ConstantSample());
            Assert.That(decay.ReleaseDecayDbPerSecond, Is.EqualTo(18f));
            Assert.That(decay.IsCcTriggered, Is.False);

            var ccTrig = ProjectFirst("<region> sample=a.wav on_locc20=64 on_hicc20=127", ConstantSample());
            Assert.That(ccTrig.IsCcTriggered, Is.True);
            Assert.That(ccTrig.OnCcTriggers, Has.Member((20, 64, 127)));
        }

        [Test]
        public void EqBandsMapForNonFlatBandsOnly()
        {
            var r = ProjectFirst("<region> sample=a.wav eq1_freq=800 eq1_gain=6 eq2_gain=-3", ConstantSample());
            Assert.That(r.EqBands, Has.Count.EqualTo(2)); // eq1 + eq2 have gain; eq3 is flat
            Assert.That(r.EqBands[0].FrequencyHz, Is.EqualTo(800f));
            Assert.That(r.EqBands[0].GainDb, Is.EqualTo(6f));
            Assert.That(r.EqBands[1].GainDb, Is.EqualTo(-3f));
        }

        [Test]
        public void EffectSendsMapToReverbAndChorus()
        {
            var g = ProjectFirst("<region> sample=a.wav effect1=50 effect2=25", ConstantSample()).Generators;
            Assert.That(g[GeneratorEnum.ReverbEffectsSend], Is.EqualTo(500)); // 50% -> 500 tenths
            Assert.That(g[GeneratorEnum.ChorusEffectsSend], Is.EqualTo(250));
        }

        [Test]
        public void StereoSampleKeepsChannelsSeparate()
        {
            // left = +0.5, right = -0.5: a down-mix would cancel to silence, so
            // opposite-sign output channels prove the two channels stay independent
            var loader = new StubLoader(Filled(0.5f, 16), SampleRate, Filled(-0.5f, 16));
            var r = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous pan=0", loader);
            Assert.That(r.Sample.IsStereo, Is.True);

            var voice = new SamplerVoice(SampleRate);
            var channel = new MidiChannelState();
            voice.Start(r, channel, 0, 60, 127, 0);
            var buffer = new float[256 * 2];
            voice.Mix(buffer, new float[256 * 2], new float[256 * 2], 256, channel);

            // a settled late frame: left positive, right negative
            Assert.That(buffer[200 * 2], Is.GreaterThan(0.05f));
            Assert.That(buffer[200 * 2 + 1], Is.LessThan(-0.05f));
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

        [Test]
        public void PositiveVolumeActuallyBoostsTheOutput()
        {
            // volume=+6 dB must raise the rendered peak by ~2x over volume=0. It
            // cannot ride the SF2 attenuation slot (the voice clamps attenuation
            // at >= 0), so it is carried as the region's linear gain.
            var flat = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous volume=0",
                ConstantSample(0.25f));
            var boosted = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous volume=6",
                ConstantSample(0.25f));

            float basePeak = RenderPeak(flat, 60, 127);
            float boostPeak = RenderPeak(boosted, 60, 127);
            Assert.That(boostPeak, Is.EqualTo(basePeak * 1.995f).Within(basePeak * 0.05f),
                "+6 dB should be ~2x the linear level");
        }

        [Test]
        public void NegativeVolumeStillAttenuates()
        {
            var flat = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous",
                ConstantSample(0.5f));
            var quiet = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous volume=-12",
                ConstantSample(0.5f));
            Assert.That(quiet.Generators[GeneratorEnum.InitialAttenuation], Is.EqualTo(120));
            Assert.That(RenderPeak(quiet, 60, 127),
                Is.EqualTo(RenderPeak(flat, 60, 127) * 0.251f).Within(0.02f));
        }

        [Test]
        public void PartialAmpVelTrackInterpolatesInTheGainDomain()
        {
            // SFZ amp_veltrack interpolates gain = 1 - p*(1 - (v/127)^2): at
            // p=0.5, v=1 that is ~0.5 (-6 dB). The old law scaled the dB curve
            // linearly instead, giving ~-42 dB — wildly too quiet.
            var r = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous amp_veltrack=50",
                ConstantSample(0.5f));
            float full = RenderPeak(r, 60, 127);
            float low = RenderPeak(r, 60, 1);
            Assert.That(low / full, Is.EqualTo(0.5f).Within(0.02f));
        }

        [Test]
        public void NegativeAmpVelTrackBoostsLowVelocities()
        {
            // negative tracking inverts the curve: gain = 1 + |p|*(1 - (v/127)^2)
            // exceeds unity at low velocity (~2x at v=1, p=-1) and is 1 at v=127.
            // A boost cannot pass through the clamped attenuation path.
            var r = ProjectFirst("<region> sample=a.wav key=60 loop_mode=loop_continuous amp_veltrack=-100",
                ConstantSample(0.25f));
            float full = RenderPeak(r, 60, 127);
            float low = RenderPeak(r, 60, 1);
            Assert.That(low / full, Is.EqualTo(2f).Within(0.05f));
        }
    }
}
