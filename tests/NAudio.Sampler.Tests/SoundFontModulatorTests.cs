using System;
using NAudio.SoundFont;
using NUnit.Framework;

namespace NAudio.Sampler.Tests;

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

    // ---- bipolar concave/convex curves (§8.2.4): zero at centre, odd-symmetric ----

    // raw source enumerations: curve type in bits 10-15, bipolar = 0x0200,
    // negative direction = 0x0100 (the controller index is irrelevant here)
    private const ushort BipolarConcavePositive = 0x0600;
    private const ushort BipolarConcaveNegative = 0x0700;
    private const ushort BipolarConvexPositive = 0x0A00;
    private const ushort BipolarConvexNegative = 0x0B00;

    [TestCase(BipolarConcavePositive, 1)]
    [TestCase(BipolarConcaveNegative, -1)]
    [TestCase(BipolarConvexPositive, 1)]
    [TestCase(BipolarConvexNegative, -1)]
    public void BipolarConcaveConvexIsZeroAtCentreAndFullScaleAtTheExtremes(ushort raw, int sign)
    {
        // §8.2.4 figures: a bipolar concave/convex source is zero at the
        // controller centre and reaches ±1 at the extremes, the sign pair
        // mirrored by the direction bit. The old 2·curve(u)−1 mapping put a
        // bipolar-concave source at ≈ −0.749 at centre instead of 0.
        var source = new ModulatorType(raw);
        Assert.That(SoundFontModulatorMath.EvaluateSource(source, 63.5, 127), Is.EqualTo(0).Within(1e-9),
            "the controller centre must map to zero");
        Assert.That(SoundFontModulatorMath.EvaluateSource(source, 127, 127), Is.EqualTo(sign).Within(1e-9),
            "the controller maximum must map to full scale");
        Assert.That(SoundFontModulatorMath.EvaluateSource(source, 0, 127), Is.EqualTo(-sign).Within(1e-9),
            "the controller minimum must map to full scale, mirrored");
    }

    [TestCase(BipolarConcavePositive)]
    [TestCase(BipolarConcaveNegative)]
    [TestCase(BipolarConvexPositive)]
    [TestCase(BipolarConvexNegative)]
    public void BipolarConcaveConvexIsOddSymmetricAboutTheCentre(ushort raw)
    {
        var source = new ModulatorType(raw);
        foreach (double x in new[] { 5.0, 10.0, 25.4, 47.0, 63.5 })
        {
            double above = SoundFontModulatorMath.EvaluateSource(source, 63.5 + x, 127);
            double below = SoundFontModulatorMath.EvaluateSource(source, 63.5 - x, 127);
            Assert.That(above, Is.EqualTo(-below).Within(1e-9),
                $"f(centre+{x}) must equal -f(centre-{x})");
        }
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

    // ---- unknown/unsupported modulator sources are ignored (§7.4, §8.2) ----

    // builds the modulator set of a region carrying the given file
    // (instrument-level) modulator record(s)
    private static ModulatorSet WithFileModulator(byte[] instrumentModulators)
    {
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(58, 60),  // overridingRootKey
            SoundFontTestBuilder.Gen(53, 0))); // sampleID (index generator last)
        var sf = SoundFontTestBuilder.BuildSingleRegion(new byte[8], igen, 0, 4, 0, 4, SampleRate, 60,
            instrumentModulators: instrumentModulators);
        return ModulatorSet.Build(sf.Presets[0].ResolveRegions()[0]);
    }

    [TestCase((ushort)0x0004, TestName = "UnknownGeneralControllerSourceContributesNothing")]
    [TestCase((ushort)0x007F, TestName = "LinkSourceContributesNothing")]
    public void UnknownPrimarySourceDisablesTheModulator(ushort sourceEnum)
    {
        // SF2.04 §7.4: a modulator whose source enumeration is illegal or
        // unsupported must be IGNORED — index 4 is undefined by §8.2.1 and
        // the Link source (127) is unsupported by this engine. Pre-fix the
        // junk source evaluated as the constant 1, turning the modulator
        // into a permanent +960 cB (silence) offset.
        var set = WithFileModulator(SoundFontTestBuilder.Mod(
            sourceEnum, (ushort)GeneratorEnum.InitialAttenuation, 960));
        var channel = new MidiChannelState();
        Assert.That(Evaluate(set, channel, 127)[Attenuation], Is.EqualTo(0).Within(0.5),
            "a modulator with an unknown/unsupported source must contribute nothing");
    }

    [Test]
    public void UnknownAmountSourceDisablesTheModulator()
    {
        // primary = "no controller" (valid, evaluates 1); amount source =
        // undefined index 5 -> the whole modulator must be ignored (§7.4)
        var set = WithFileModulator(SoundFontTestBuilder.Mod(
            0x0000, (ushort)GeneratorEnum.InitialAttenuation, 960, amountSourceOper: 0x0005));
        var channel = new MidiChannelState();
        Assert.That(Evaluate(set, channel, 127)[Attenuation], Is.EqualTo(0).Within(0.5));
    }

    [TestCase(0)]   // bank select MSB
    [TestCase(6)]   // data entry MSB
    [TestCase(32)]  // bank select LSB
    [TestCase(38)]  // data entry LSB
    [TestCase(98)]  // NRPN LSB
    [TestCase(99)]  // NRPN MSB
    [TestCase(100)] // RPN LSB
    [TestCase(101)] // RPN MSB
    [TestCase(120)] // channel mode messages 120-127
    [TestCase(127)]
    public void ProhibitedCcSourceDisablesTheModulator(int cc)
    {
        // SF2.04 §8.2.2 prohibits these controller numbers as modulator
        // sources; a modulator naming one must be ignored entirely
        var set = WithFileModulator(SoundFontTestBuilder.Mod(
            (ushort)(0x0080 | cc), (ushort)GeneratorEnum.InitialAttenuation, 960));
        var channel = new MidiChannelState();
        channel.SetController(cc, 127); // would drive the modulator to full if honoured
        Assert.That(Evaluate(set, channel, 127)[Attenuation], Is.EqualTo(0).Within(0.5),
            $"a modulator sourced from prohibited CC{cc} must contribute nothing");
    }

    [Test]
    public void PolyPressureSourceDisablesTheModulatorEntirely()
    {
        // Poly pressure (source 10) is defined by §8.2.1 but per-note
        // pressure is not tracked, so the modulator must be disabled until
        // it is. Pre-fix it evaluated as the raw constant 0, so a BIPOLAR
        // poly-pressure source contributed a constant -1 x amount.
        var set = WithFileModulator(SoundFontTestBuilder.Mod(
            0x020A, (ushort)GeneratorEnum.VibratoLFOToPitch, 50)); // bipolar linear poly pressure
        var channel = new MidiChannelState();
        Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(0).Within(1e-9),
            "an untracked poly-pressure source must contribute nothing (was a constant -50)");
    }

    [Test]
    public void PitchWheelSensitivitySourceEvaluatesFromTheDecodedRpn()
    {
        // Source 16 (pitch-bend sensitivity) is a DEFINED source the engine
        // evaluates from the decoded RPN 0 value, normalised over the spec's
        // 0..127 semitone span — it must not be rejected with the unknowns.
        var set = WithFileModulator(SoundFontTestBuilder.Mod(
            0x0010, (ushort)GeneratorEnum.VibratoLFOToPitch, 127)); // unipolar linear positive
        var channel = new MidiChannelState();

        // default bend range ±2 semitones -> 127 * 2/127 = 2
        Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(2.0).Within(0.01));

        // RPN 0 data entry widens the range to 24 -> 127 * 24/127 = 24
        channel.SelectRpnMsb(0);
        channel.SelectRpnLsb(0);
        channel.DataEntryMsb(24);
        Assert.That(Evaluate(set, channel, 127)[Vibrato], Is.EqualTo(24.0).Within(0.01),
            "the source must track the decoded RPN 0 bend range");
    }

    [Test]
    public void JunkSourcedFileModulatorDoesNotSilenceTheNote()
    {
        // render-level guard for §7.4: a file modulator with an undefined
        // source routed at full amount into attenuation is ignored, so the
        // note plays at full level (pre-fix it became a constant +960 cB
        // offset — total silence)
        var data = new byte[8];
        for (int i = 0; i < 4; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; } // 0.5 fs
        var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
            SoundFontTestBuilder.Gen(54, 1),   // loop
            SoundFontTestBuilder.Gen(58, 60),  // root key
            SoundFontTestBuilder.Gen(53, 0))); // sampleID
        var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, 4, 0, 4, SampleRate, 60,
            instrumentModulators: SoundFontTestBuilder.Mod(0x0004, (ushort)GeneratorEnum.InitialAttenuation, 960));
        var sampler = new SoundFontSampler(sf, SampleRate);
        sampler.NoteOn(0, 60, 127);

        var buffer = new float[512 * 2];
        sampler.Read(buffer);
        float peak = 0;
        foreach (var s in buffer) peak = Math.Max(peak, Math.Abs(s));
        Assert.That(peak, Is.GreaterThan(0.1f), "the junk-sourced modulator must not attenuate the note");
    }

    [Test]
    public void BipolarConcaveCcToPitchSitsAtZeroAtTheControllerCentre()
    {
        // Render-level guard for the §8.2.4 fix: a bipolar-concave CC21
        // source (0x0695) routed to modEnvToPitch (the default mod envelope
        // sustains at 1, making the routing a direct pitch offset). At the
        // controller centre the source evaluates ~0, so a one-shot finishes
        // in (essentially) its unmodulated time; pre-fix the centre sat at
        // 2·concave(0.5)−1 ≈ −0.75 -> ≈ −900 cents, playing ~68% longer.
        int FramesUntilSilent(byte[] instrumentModulators)
        {
            const int points = 4410; // ~100 ms one-shot
            var data = new byte[points * 2];
            for (int i = 0; i < points; i++) { data[i * 2] = 0x00; data[i * 2 + 1] = 0x40; }
            var igen = SoundFontTestBuilder.Chunk("igen", SoundFontTestBuilder.Concat(
                SoundFontTestBuilder.Gen(58, 60),
                SoundFontTestBuilder.Gen(53, 0)));
            var sf = SoundFontTestBuilder.BuildSingleRegion(data, igen, 0, points, 0, points,
                SampleRate, 60, instrumentModulators: instrumentModulators);
            var sampler = new SoundFontSampler(sf, SampleRate);
            sampler.ProcessMidiEvent(new NAudio.Midi.ControlChangeEvent(0, 1,
                (NAudio.Midi.MidiController)21, 64)); // the controller centre
            sampler.NoteOn(0, 60, 127);

            int frames = 0;
            var buf = new float[2];
            while (sampler.ActiveVoiceCount > 0 && frames < 20000) { sampler.Read(buf); frames++; }
            return frames;
        }

        int reference = FramesUntilSilent(null);
        int modulated = FramesUntilSilent(SoundFontTestBuilder.Mod(
            0x0695, (ushort)GeneratorEnum.ModulationEnvelopeToPitch, 1200));
        Assert.That(modulated, Is.EqualTo(reference).Within(reference / 20),
            "a bipolar-concave CC source at its centre must not detune the note");
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
        // the shared reverb bus is fully wet, so after the dry voice is choked
        // the only remaining energy is the wet return fed by the send
        float withSend = TailEnergy(reverbSend: 1000);
        float noSend = TailEnergy(reverbSend: 0);

        Assert.That(withSend, Is.GreaterThan(1e-3f),
            "a full reverb send should add audible wet energy");
        Assert.That(withSend, Is.GreaterThan(noSend * 10f),
            "the wet tail should come from the send, not the dry path");
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

    private static float TailEnergy(int reverbSend)
    {
        var sampler = new SoundFontSampler(MakeLoopedReverbFont(reverbSend), SampleRate);
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
