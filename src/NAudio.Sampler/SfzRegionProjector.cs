using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Sfz;
using NAudio.SoundFont;

namespace NAudio.Sampler;

/// <summary>
/// Projects SFZ regions onto the format-neutral <see cref="SamplerRegion"/>
/// the voice engine plays: loads the sample (honouring loop points embedded
/// in a WAV's <c>smpl</c> chunk), and expresses the SFZ opcodes as SoundFont
/// generators in engine units (tuning in cents, attenuation in centibels,
/// pan in 0.1% units, envelopes in timecents, cutoff in absolute cents).
/// A positive <c>volume</c> boost rides <see cref="SamplerRegion.GainLinear"/>
/// because SF2 attenuation cannot go below zero. SFZ velocity tracking rides
/// on <see cref="SamplerRegion.VelocityTrackingPercent"/>; SFZ has no SF2
/// modulators, so <see cref="SamplerRegion.Modulators"/> is left null.
///
/// Tier 1 and Tier 2 are fully honoured by the engine: stereo samples,
/// <c>release</c>/<c>first</c>/<c>legato</c> triggers, all four
/// <c>fil_type</c> shapes, cross-group <c>off_by</c> chokes (with
/// <c>off_mode</c>), per-region <c>polyphony</c> caps, note-on selection
/// (keyswitches, round-robin, random, CC gates), crossfades,
/// <c>amp_velcurve_N</c> velocity curves (baked to a per-velocity table at
/// projection time), per-region EGs/LFOs, EQ bands and effect sends.
/// </summary>
internal static class SfzRegionProjector
{
    /// <summary>
    /// Projects one mapped region, or returns null if it has no sample, the
    /// sample cannot be loaded, or the region is disabled (<c>end=-1</c>).
    /// </summary>
    public static SamplerRegion Project(SfzMappedRegion region, ISfzSampleLoader loader)
    {
        if (region.Sample == null) return null;
        // an explicit end=-1 means "this region is not played" per the SFZ spec
        if (region.HasEnd && region.End < 0) return null;
        if (!loader.TryLoad(region.Sample, out var data, out var dataRight, out var sampleRate,
            out var embeddedLoop)) return null;

        int length = data.Length;
        int start = Clamp(region.Offset, 0, length);
        // SFZ `end` is INCLUSIVE ("will play up to and including this
        // sample"); SampleData.End is exclusive, hence +1
        int end = region.HasEnd ? Clamp(region.End + 1, start, length) : length;

        // loop_start/loop_end opcodes (also inclusive ends) override loop
        // points embedded in the sample file (a WAV smpl chunk — the
        // FLAC/Ogg path doesn't surface loops); with neither, the loop
        // spans the playable region. SampleLoop.End is already exclusive.
        int loopStart = region.HasLoopStart && region.LoopStart >= 0
            ? region.LoopStart
            : embeddedLoop?.Start ?? -1;
        int loopEnd = region.HasLoopEnd && region.LoopEnd >= 0
            ? region.LoopEnd + 1
            : embeddedLoop?.End ?? -1;
        loopStart = loopStart >= 0 ? Clamp(loopStart, start, end) : start;
        loopEnd = loopEnd >= 0 ? Clamp(loopEnd, start, end) : end;

        // per the spec an absent loop_mode defaults to loop_continuous for
        // samples whose file defines a loop, and no_loop otherwise; an
        // explicit opcode (including no_loop) always wins
        var loopMode = region.HasLoopMode
            ? region.LoopMode
            : embeddedLoop != null ? SfzLoopMode.LoopContinuous : SfzLoopMode.NoLoop;

        var sample = new SampleData
        {
            Data = data,
            DataRight = dataRight,
            SampleRate = sampleRate,
            RootKey = region.PitchKeycenter,
            PitchCorrectionCents = 0,
            Start = start,
            End = end,
            LoopStart = loopStart,
            LoopEnd = loopEnd
        };

        return new SamplerRegion
        {
            Sample = sample,
            Generators = BuildGenerators(region, loopMode),
            Modulators = null, // SFZ Tier 1 has no SF2-style modulators
            VelocityTrackingPercent = region.AmpVelTrack,
            VelocityCurve = region.BuildVelocityCurve(),
            // a volume boost is carried as linear gain (see BuildGenerators)
            GainLinear = region.VolumeDb > 0 ? (float)Math.Pow(10.0, region.VolumeDb / 20.0) : 1f,
            FilterType = MapFilterType(region.FilterType),
            Trigger = MapTrigger(region.Trigger),
            IgnoreNoteOff = loopMode == SfzLoopMode.OneShot,
            Group = region.Group,
            OffByGroup = region.OffBy,
            // off_mode=normal releases a choked voice via its own ampeg_release;
            // anything else (fast, or ARIA's unsupported time) is the fast cut
            OffMode = region.OffMode == SfzOffMode.Normal ? SamplerOffMode.Normal : SamplerOffMode.Fast,
            Polyphony = Math.Max(0, region.Polyphony), // parsed −1 = absent = unlimited
            KeyswitchLast = region.KeyswitchLast,
            KeyswitchDefault = region.KeyswitchDefault,
            SequenceLength = region.SequenceLength,
            SequencePosition = region.SequencePosition,
            LowRandom = region.LowRandom,
            HighRandom = region.HighRandom,
            CcGates = region.CcGates,
            ReleaseDecayDbPerSecond = region.ReleaseDecayDbPerSecond,
            OnCcTriggers = region.OnCcTriggers,
            EqBands = BuildEqBands(region.EqBands),
            KeyFadeInLow = region.KeyFadeInLow,
            KeyFadeInHigh = region.KeyFadeInHigh,
            KeyFadeOutLow = region.KeyFadeOutLow,
            KeyFadeOutHigh = region.KeyFadeOutHigh,
            VelocityFadeInLow = region.VelocityFadeInLow,
            VelocityFadeInHigh = region.VelocityFadeInHigh,
            VelocityFadeOutLow = region.VelocityFadeOutLow,
            VelocityFadeOutHigh = region.VelocityFadeOutHigh,
            KeyFadeCurve = MapCrossfadeCurve(region.KeyFadeCurve),
            VelocityFadeCurve = MapCrossfadeCurve(region.VelocityFadeCurve),
            LoKey = (byte)Clamp(region.LoKey, 0, 127),
            HiKey = (byte)Clamp(region.HiKey, 0, 127),
            LoVelocity = (byte)Clamp(region.LoVel, 0, 127),
            HiVelocity = (byte)Clamp(region.HiVel, 0, 127)
        };
    }

    private static SamplerFilterType MapFilterType(SfzFilterType type)
    {
        switch (type)
        {
            case SfzFilterType.HighPass: return SamplerFilterType.HighPass;
            case SfzFilterType.BandPass: return SamplerFilterType.BandPass;
            case SfzFilterType.BandReject: return SamplerFilterType.BandReject;
            default: return SamplerFilterType.LowPass;
        }
    }

    // SFZ has up to three EQ bands (eq1/eq2/eq3); only bands with a non-zero
    // gain are applied (a flat band is a no-op). The semantic layer supplies
    // the bands in natural units; the octave bandwidth becomes the peaking
    // filter's Q here.
    private static IReadOnlyList<SamplerEqBand> BuildEqBands(IReadOnlyList<SfzEqBand> bands)
    {
        if (bands == null) return null;
        List<SamplerEqBand> result = null;
        foreach (var band in bands)
        {
            if (band.GainDb == 0f) continue; // flat band -> no-op
            result ??= new List<SamplerEqBand>(3);
            result.Add(new SamplerEqBand(band.FrequencyHz, band.GainDb, BandwidthToQ(band.BandwidthOctaves)));
        }
        return result;
    }

    // octave bandwidth -> peaking-filter Q
    private static float BandwidthToQ(float bandwidth)
    {
        if (bandwidth <= 0f) bandwidth = 1f;
        double t = Math.Pow(2.0, bandwidth);
        return (float)(Math.Sqrt(t) / (t - 1.0));
    }

    private static SamplerCrossfadeCurve MapCrossfadeCurve(SfzCrossfadeCurve curve) =>
        curve == SfzCrossfadeCurve.Linear ? SamplerCrossfadeCurve.Linear : SamplerCrossfadeCurve.Power;

    private static SamplerTrigger MapTrigger(SfzTrigger trigger)
    {
        switch (trigger)
        {
            case SfzTrigger.Release: return SamplerTrigger.Release;
            case SfzTrigger.First: return SamplerTrigger.First;
            case SfzTrigger.Legato: return SamplerTrigger.Legato;
            default: return SamplerTrigger.Attack;
        }
    }

    private static SoundFontGenerators BuildGenerators(SfzMappedRegion region, SfzLoopMode loopMode)
    {
        var gen = SoundFontGenerators.CreateWithDefaults();

        // pitch: keytrack -> scaleTuning; tune cents split into coarse semitones + fine cents
        gen[GeneratorEnum.ScaleTuning] = (short)region.PitchKeytrack;
        int coarse = (int)Math.Round(region.TuneCents / 100.0);
        gen[GeneratorEnum.CoarseTune] = (short)coarse;
        gen[GeneratorEnum.FineTune] = (short)Math.Round(region.TuneCents - coarse * 100.0);

        // amplitude: a negative SFZ volume (dB, +louder) maps to SF2
        // attenuation (cB, +quieter) so it still sums with the modulators; a
        // positive volume (a boost, spec range up to +6 dB and beyond) cannot
        // ride this slot — the voice clamps total attenuation at >= 0 per the
        // SF2 spec — so it is carried as SamplerRegion.GainLinear instead
        gen[GeneratorEnum.InitialAttenuation] = (short)Math.Round(Math.Max(0f, -region.VolumeDb) * 10.0);
        gen[GeneratorEnum.Pan] = (short)Math.Round(region.Pan * 500.0);

        // amplitude envelope (seconds -> timecents; sustain fraction -> attenuation cB)
        gen[GeneratorEnum.DelayVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegDelay);
        gen[GeneratorEnum.AttackVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegAttack);
        gen[GeneratorEnum.HoldVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegHold);
        gen[GeneratorEnum.DecayVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegDecay);
        gen[GeneratorEnum.ReleaseVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegRelease);
        gen[GeneratorEnum.SustainVolumeEnvelope] = GeneratorUnits.SustainCentibels(region.AmpegSustain);

        // filter cutoff/resonance (the voice applies the shape from FilterType)
        if (region.HasCutoff && region.CutoffHz > 0)
        {
            gen[GeneratorEnum.InitialFilterCutoffFrequency] =
                GeneratorUnits.Clamp16(SynthMath.HertzToAbsoluteCents(region.CutoffHz));
            gen[GeneratorEnum.InitialFilterQ] = (short)Math.Round(region.ResonanceDb * 10.0);
        }

        gen[GeneratorEnum.SampleModes] = (short)MapLoopMode(loopMode);

        // effect sends: effect1 -> reverb bus, effect2 -> chorus bus (0..100% -> 0.1% units)
        gen[GeneratorEnum.ReverbEffectsSend] = GeneratorUnits.Clamp16(region.Effect1Percent * 10.0);
        gen[GeneratorEnum.ChorusEffectsSend] = GeneratorUnits.Clamp16(region.Effect2Percent * 10.0);

        ApplyModulation(region, gen);
        return gen;
    }

    // Maps the SFZ per-region EGs/LFOs onto the voice's existing modulation
    // slots: pitch LFO -> vibrato LFO, amp/filter LFO -> the modulation LFO,
    // filter/pitch EG -> the modulation envelope. The mod LFO and mod envelope
    // are each one source, so a region using both amp+filter LFOs (or both
    // filter+pitch EGs) with different rates/shapes shares the rate/shape
    // (the amp LFO / filter EG wins) while keeping independent depths.
    private static void ApplyModulation(SfzMappedRegion region, SoundFontGenerators gen)
    {
        // pitch LFO (vibrato) -> dedicated vibrato LFO slot
        var pitchLfo = region.PitchLfo;
        if (pitchLfo.IsActive)
        {
            gen[GeneratorEnum.VibratoLFOToPitch] = GeneratorUnits.Clamp16(pitchLfo.Depth); // cents
            gen[GeneratorEnum.FrequencyVibratoLFO] = GeneratorUnits.Clamp16(SynthMath.HertzToAbsoluteCents(pitchLfo.FrequencyHz));
            gen[GeneratorEnum.DelayVibratoLFO] = GeneratorUnits.ToTimecents(pitchLfo.DelaySeconds);
        }

        // amp LFO (tremolo) + filter LFO (wah) -> the shared modulation LFO
        var ampLfo = region.AmpLfo;
        var filterLfo = region.FilterLfo;
        if (ampLfo.IsActive || filterLfo.IsActive)
        {
            var shared = ampLfo.IsActive ? ampLfo : filterLfo; // amp LFO rate/delay win if both
            gen[GeneratorEnum.FrequencyModulationLFO] = GeneratorUnits.Clamp16(SynthMath.HertzToAbsoluteCents(shared.FrequencyHz));
            gen[GeneratorEnum.DelayModulationLFO] = GeneratorUnits.ToTimecents(shared.DelaySeconds);
            if (ampLfo.IsActive) gen[GeneratorEnum.ModulationLFOToVolume] = GeneratorUnits.Clamp16(ampLfo.Depth * 10.0); // dB -> cB
            if (filterLfo.IsActive) gen[GeneratorEnum.ModulationLFOToFilterCutoffFrequency] = GeneratorUnits.Clamp16(filterLfo.Depth); // cents
        }

        // filter EG + pitch EG -> the shared modulation envelope
        var filterEg = region.FilterEg;
        var pitchEg = region.PitchEg;
        if (filterEg.IsActive || pitchEg.IsActive)
        {
            var shape = filterEg.IsActive ? filterEg : pitchEg; // filter EG shape wins if both
            gen[GeneratorEnum.DelayModulationEnvelope] = GeneratorUnits.ToTimecents(shape.DelaySeconds);
            gen[GeneratorEnum.AttackModulationEnvelope] = GeneratorUnits.ToTimecents(shape.AttackSeconds);
            gen[GeneratorEnum.HoldModulationEnvelope] = GeneratorUnits.ToTimecents(shape.HoldSeconds);
            gen[GeneratorEnum.DecayModulationEnvelope] = GeneratorUnits.ToTimecents(shape.DecaySeconds);
            gen[GeneratorEnum.ReleaseModulationEnvelope] = GeneratorUnits.ToTimecents(shape.ReleaseSeconds);
            // mod-env sustain generator is 0.1% "decreasing": level = 1 - permille/1000
            gen[GeneratorEnum.SustainModulationEnvelope] = GeneratorUnits.Clamp16(1000.0 - 10.0 * shape.SustainPercent);
            if (filterEg.IsActive) gen[GeneratorEnum.ModulationEnvelopeToFilterCutoffFrequency] = GeneratorUnits.Clamp16(filterEg.DepthCents);
            if (pitchEg.IsActive) gen[GeneratorEnum.ModulationEnvelopeToPitch] = GeneratorUnits.Clamp16(pitchEg.DepthCents);
        }
    }

    private static int MapLoopMode(SfzLoopMode mode)
    {
        switch (mode)
        {
            case SfzLoopMode.LoopContinuous: return (int)SampleMode.LoopContinuously;
            case SfzLoopMode.LoopSustain: return (int)SampleMode.LoopAndContinue;
            default: return (int)SampleMode.NoLoop; // no_loop and one_shot
        }
    }

    private static int Clamp(int value, int lo, int hi) => value < lo ? lo : value > hi ? hi : value;
}
