using System;
using NAudio.Sampler;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Sampler.Tests
{
    /// <summary>
    /// Tests for the SoundFont 2.04 modulator engine (§8.2.4 transform curves and
    /// §8.4 default modulators): the transform maths, the implicit default
    /// modulators evaluated against live controllers, and the audible effect of
    /// velocity on level.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SoundFontModulatorTests
    {
        private const int SampleRate = 44100;
        private const int Attenuation = (int)GeneratorEnum.InitialAttenuation;
        private const int Pan = (int)GeneratorEnum.Pan;
        private const int Vibrato = (int)GeneratorEnum.VibratoLFOToPitch;

        // ---- transform curves (§8.2.4) ----

        [Test]
        public void ConcaveAndConvexHaveUnitEndpoints()
        {
            Assert.That(SoundFontModulatorMath.Concave(0), Is.EqualTo(0).Within(1e-9));
            Assert.That(SoundFontModulatorMath.Concave(1), Is.EqualTo(1).Within(1e-9));
            Assert.That(SoundFontModulatorMath.Convex(0), Is.EqualTo(0).Within(1e-9));
            Assert.That(SoundFontModulatorMath.Convex(1), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void ConcaveIsBelowLinearAndConvexAboveIt()
        {
            // concave rises slowly then steeply; convex is its mirror
            Assert.That(SoundFontModulatorMath.Concave(0.5), Is.LessThan(0.5));
            Assert.That(SoundFontModulatorMath.Convex(0.5), Is.GreaterThan(0.5));
            // convex(x) == 1 - concave(1-x)
            Assert.That(SoundFontModulatorMath.Convex(0.5),
                Is.EqualTo(1 - SoundFontModulatorMath.Concave(0.5)).Within(1e-9));
            Assert.That(SoundFontModulatorMath.Convex(0.3),
                Is.EqualTo(1 - SoundFontModulatorMath.Concave(0.7)).Within(1e-9));
        }

        [Test]
        public void ConcaveIsMonotonicIncreasing()
        {
            double previous = -1;
            for (int i = 0; i <= 100; i++)
            {
                double v = SoundFontModulatorMath.Concave(i / 100.0);
                Assert.That(v, Is.GreaterThanOrEqualTo(previous));
                previous = v;
            }
        }

        [Test]
        public void SwitchCurveStepsAtHalf()
        {
            Assert.That(SoundFontModulatorMath.Curve(SourceTypeEnum.Switch, 0.49), Is.EqualTo(0));
            Assert.That(SoundFontModulatorMath.Curve(SourceTypeEnum.Switch, 0.5), Is.EqualTo(1));
        }

        [Test]
        public void BipolarSourceSpansMinusOneToOne()
        {
            // CC10 source: positive bipolar linear (0x028A) -> 0 maps to -1, 127 to +1
            var pan = new ModulatorType(0x028A);
            Assert.That(SoundFontModulatorMath.EvaluateSource(pan, 0, 127), Is.EqualTo(-1).Within(1e-9));
            Assert.That(SoundFontModulatorMath.EvaluateSource(pan, 127, 127), Is.EqualTo(1).Within(0.02));
            Assert.That(SoundFontModulatorMath.EvaluateSource(pan, 63.5, 127), Is.EqualTo(0).Within(1e-9));
        }

        // ---- default modulators (§8.4) evaluated against controllers ----

        private static ModulatorSet DefaultsOnly()
        {
            // a region with no file modulators -> the default modulators only
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(58, 60),  // overridingRootKey
                SoundFontTestBuilder.Gen(53, 0))); // sampleID (index generator last)
            var sf = SoundFontTestBuilder.BuildSingleRegion(new byte[8], igen, 0, 4, 0, 4, SampleRate, 60);
            var region = sf.Presets[0].ResolveRegions()[0];
            Assert.That(region.InstrumentModulators, Is.Empty, "expected no file modulators");
            return ModulatorSet.Build(region);
        }

        private static double[] Evaluate(ModulatorSet set, MidiChannelState channel, int velocity, int key = 60)
        {
            var accum = new double[(int)GeneratorEnum.UnusedEnd + 1];
            set.Accumulate(channel, velocity, key, accum);
            return accum;
        }

        [Test]
        public void VelocityToAttenuationIsZeroAtFullAndLargeAtLow()
        {
            var set = DefaultsOnly();
            var channel = new MidiChannelState(); // CC7/CC11 default to 127 (no extra attenuation)

            double full = Evaluate(set, channel, 127)[Attenuation];
            double mid = Evaluate(set, channel, 64)[Attenuation];
            double low = Evaluate(set, channel, 1)[Attenuation];

            Assert.That(full, Is.EqualTo(0).Within(0.5), "full velocity should add no attenuation");
            Assert.That(mid, Is.EqualTo(960 * SoundFontModulatorMath.Concave(1 - 64 / 127.0)).Within(1.0));
            Assert.That(low, Is.GreaterThan(800), "low velocity should be heavily attenuated");
            Assert.That(mid, Is.GreaterThan(full).And.LessThan(low));
        }

        [Test]
        public void Cc11ExpressionAttenuates()
        {
            var set = DefaultsOnly();
            var channel = new MidiChannelState();

            channel.SetController((int)NAudio.Midi.MidiController.Expression, 127);
            double open = Evaluate(set, channel, 127)[Attenuation];
            channel.SetController((int)NAudio.Midi.MidiController.Expression, 0);
            double closed = Evaluate(set, channel, 127)[Attenuation];

            Assert.That(open, Is.EqualTo(0).Within(0.5));
            Assert.That(closed, Is.EqualTo(960).Within(0.5), "expression 0 = full 96 dB attenuation");
        }

        [Test]
        public void Cc7VolumeUsesController7NotVelocity()
        {
            // regression: the spec mistypes this source as 0x0582 (= velocity);
            // it must read CC7. With CC7 at 0, the default-only set is fully
            // attenuated regardless of velocity.
            var set = DefaultsOnly();
            var channel = new MidiChannelState();
            channel.SetController((int)NAudio.Midi.MidiController.MainVolume, 0);

            Assert.That(Evaluate(set, channel, 127)[Attenuation], Is.EqualTo(960).Within(0.5));
        }

        [Test]
        public void Cc10PansFullRange()
        {
            var set = DefaultsOnly();
            var channel = new MidiChannelState();

            channel.SetController((int)NAudio.Midi.MidiController.Pan, 0);
            Assert.That(Evaluate(set, channel, 127)[Pan], Is.EqualTo(-1000).Within(0.5));
            channel.SetController((int)NAudio.Midi.MidiController.Pan, 127);
            Assert.That(Evaluate(set, channel, 127)[Pan], Is.EqualTo(1000).Within(20));
            channel.SetController((int)NAudio.Midi.MidiController.Pan, 64);
            Assert.That(Evaluate(set, channel, 127)[Pan], Is.EqualTo(0).Within(10));
        }

        [Test]
        public void ModWheelAndChannelPressureAddVibratoDepth()
        {
            var set = DefaultsOnly();
            var channel = new MidiChannelState();

            Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(0).Within(1e-6));

            channel.SetController(1, 127); // CC1 mod wheel
            Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(50).Within(0.5));

            channel.SetController(1, 0);
            channel.ChannelPressure = 127;
            Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(50).Within(0.5));
        }

        // ---- audible effect ----

        [Test]
        public void HigherVelocityIsLouder()
        {
            float loud = RenderPeak(velocity: 127);
            float soft = RenderPeak(velocity: 40);
            Assert.That(loud, Is.GreaterThan(soft * 2f),
                "the velocity->attenuation default modulator should make 127 much louder than 40");
        }

        [Test]
        public void ReverbSendAddsWetSignalToTheOutput()
        {
            // a sustained looped note: with a full reverb send the shared reverb's
            // wet return adds energy on top of the dry mix
            float withSend = TotalEnergy(reverbSend: 1000);
            float noSend = TotalEnergy(reverbSend: 0);

            Assert.That(noSend, Is.GreaterThan(0f), "the dry voice should produce sound");
            Assert.That(withSend, Is.GreaterThan(noSend * 1.05f),
                "a full reverb send should add audible wet energy");
        }

        [Test]
        public void ReverbTailContinuesAfterTheNoteIsReleased()
        {
            var sampler = new SoundFontSampler(MakeLoopedReverbFont(reverbSend: 1000), SampleRate);
            sampler.NoteOn(0, 60, 127);
            var warm = new float[256 * 2];
            sampler.Read(warm);          // let the reverb build up
            sampler.AllSoundOff();        // choke the dry voice with a short fade

            // render past the choke fade; remaining energy is the reverb tail
            var tail = new float[8192 * 2];
            sampler.Read(tail);
            float late = 0;
            for (int i = tail.Length / 2; i < tail.Length; i++) late += Math.Abs(tail[i]);
            Assert.That(late, Is.GreaterThan(1e-3f), "the reverb tail should outlast the note");
        }

        private static NAudio.SoundFont.SoundFont MakeLoopedReverbFont(int reverbSend)
        {
            var data = new byte[8];
            for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // 0.5 fs
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen((ushort)GeneratorEnum.ReverbEffectsSend, (ushort)reverbSend),
                SoundFontTestBuilder.Gen(54, 1),   // loop continuously
                SoundFontTestBuilder.Gen(58, 60),  // root key
                SoundFontTestBuilder.Gen(53, 0)));  // sampleID
            return SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
        }

        private static float TotalEnergy(int reverbSend)
        {
            var sampler = new SoundFontSampler(MakeLoopedReverbFont(reverbSend), SampleRate);
            sampler.NoteOn(0, 60, 127);
            var buffer = new float[4096 * 2];
            sampler.Read(buffer);
            float energy = 0;
            foreach (var s in buffer) energy += Math.Abs(s);
            return energy;
        }

        private static float RenderPeak(int velocity)
        {
            // constant-amplitude looped instrument at root key, instant attack
            var data = new byte[8];
            for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // 0.5 fs
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(54, 1),   // loop
                SoundFontTestBuilder.Gen(58, 60),  // root key
                SoundFontTestBuilder.Gen(53, 0))); // sampleID
            var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60);
            var sampler = new SoundFontSampler(sf, SampleRate);
            sampler.NoteOn(0, 60, velocity);

            var buffer = new float[512 * 2];
            sampler.Read(buffer);
            float peak = 0;
            foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
            return peak;
        }
    }
}
