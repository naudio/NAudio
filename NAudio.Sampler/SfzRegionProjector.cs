using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Sfz;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// Projects SFZ regions onto the format-neutral <see cref="SamplerRegion"/>
    /// the voice engine plays: loads the sample, and expresses the SFZ opcodes as
    /// SoundFont generators in engine units (tuning in cents, attenuation in
    /// centibels, pan in 0.1% units, envelopes in timecents, cutoff in absolute
    /// cents). SFZ velocity tracking rides on
    /// <see cref="SamplerRegion.VelocityTrackingPercent"/>; SFZ has no SF2
    /// modulators, so <see cref="SamplerRegion.Modulators"/> is left null.
    ///
    /// Tier-1 coverage: <c>release</c>/<c>first</c>/<c>legato</c> triggers, true
    /// stereo samples, high/band-pass filters and cross-group <c>off_by</c> are
    /// not yet honoured (see <see cref="Project"/>).
    /// </summary>
    internal static class SfzRegionProjector
    {
        /// <summary>
        /// Maps and projects every region of an instrument, loading samples via
        /// <paramref name="loader"/>. Regions whose sample is missing or that use
        /// an unsupported trigger are skipped.
        /// </summary>
        public static List<SamplerRegion> ProjectAll(SfzInstrument instrument, ISfzSampleLoader loader)
        {
            var result = new List<SamplerRegion>();
            foreach (var mapped in instrument.MapRegions())
            {
                var region = Project(mapped, loader);
                if (region != null) result.Add(region);
            }
            return result;
        }

        /// <summary>
        /// Projects one mapped region, or returns null if it has no sample or the
        /// sample cannot be loaded.
        /// </summary>
        public static SamplerRegion Project(SfzMappedRegion region, ISfzSampleLoader loader)
        {
            if (region.Sample == null) return null;
            if (!loader.TryLoad(region.Sample, out var data, out var dataRight, out var sampleRate)) return null;

            int length = data.Length;
            int start = Clamp(region.Offset, 0, length);
            int end = region.End >= 0 ? Clamp(region.End, start, length) : length;
            int loopStart = region.LoopStart >= 0 ? Clamp(region.LoopStart, start, end) : start;
            int loopEnd = region.LoopEnd >= 0 ? Clamp(region.LoopEnd, start, end) : end;

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
                Generators = BuildGenerators(region),
                Modulators = null, // SFZ Tier 1 has no SF2-style modulators
                VelocityTrackingPercent = region.AmpVelTrack,
                FilterType = MapFilterType(region.FilterType),
                Trigger = MapTrigger(region.Trigger),
                IgnoreNoteOff = region.LoopMode == SfzLoopMode.OneShot,
                Group = region.Group,
                OffByGroup = region.OffBy,
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
                default: return SamplerFilterType.LowPass; // low-pass; band-reject not yet supported
            }
        }

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

        private static SoundFontGenerators BuildGenerators(SfzMappedRegion region)
        {
            var gen = SoundFontGenerators.CreateWithDefaults();

            // pitch: keytrack -> scaleTuning; tune cents split into coarse semitones + fine cents
            gen[GeneratorEnum.ScaleTuning] = (short)region.PitchKeytrack;
            int coarse = (int)Math.Round(region.TuneCents / 100.0);
            gen[GeneratorEnum.CoarseTune] = (short)coarse;
            gen[GeneratorEnum.FineTune] = (short)Math.Round(region.TuneCents - coarse * 100.0);

            // amplitude: SFZ volume (dB, +louder) -> SF2 attenuation (cB, +quieter)
            gen[GeneratorEnum.InitialAttenuation] = (short)Math.Round(-region.VolumeDb * 10.0);
            gen[GeneratorEnum.Pan] = (short)Math.Round(region.Pan * 500.0);

            // amplitude envelope (seconds -> timecents; sustain fraction -> attenuation cB)
            gen[GeneratorEnum.DelayVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegDelay);
            gen[GeneratorEnum.AttackVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegAttack);
            gen[GeneratorEnum.HoldVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegHold);
            gen[GeneratorEnum.DecayVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegDecay);
            gen[GeneratorEnum.ReleaseVolumeEnvelope] = GeneratorUnits.ToTimecents(region.AmpegRelease);
            gen[GeneratorEnum.SustainVolumeEnvelope] = GeneratorUnits.SustainCentibels(region.AmpegSustain);

            // filter cutoff/resonance (the voice applies the shape from FilterType);
            // band-reject is not yet supported, so it leaves the filter open
            if (region.HasCutoff && region.CutoffHz > 0 && region.FilterType != SfzFilterType.BandReject)
            {
                gen[GeneratorEnum.InitialFilterCutoffFrequency] =
                    GeneratorUnits.Clamp16(SynthMath.HertzToAbsoluteCents(region.CutoffHz));
                gen[GeneratorEnum.InitialFilterQ] = (short)Math.Round(region.ResonanceDb * 10.0);
            }

            gen[GeneratorEnum.SampleModes] = (short)MapLoopMode(region.LoopMode);
            return gen;
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
}
