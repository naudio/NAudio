using System;
using System.Collections.Generic;
using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// The SoundFont 2.04 modulator transform curves (§8.2.4) and source
    /// evaluation. A modulator's source controller value is normalised to
    /// [0, 1], shaped by one of four curves (linear / concave / convex / switch)
    /// in the direction and polarity selected by the source's <c>D</c> and
    /// <c>P</c> bits, and the result drives the modulator amount.
    ///
    /// The spec gives the concave law only "as variations of" a logarithmic
    /// equation, so these use the standard 96&#160;dB convention: the concave
    /// curve spans the SoundFont peak-attenuation range, which makes the implicit
    /// velocity&#8594;attenuation default modulator (§8.4.1) reproduce the
    /// familiar near-square-law velocity response. The bar is musical fidelity to
    /// the spec, not sample-identity with any particular synth (SamplerDesign §12).
    /// Public and static so the transforms can be unit-tested directly.
    /// </summary>
    public static class SoundFontModulatorMath
    {
        // SoundFont peak attenuation is 960 cB (96 dB); the concave/convex curves
        // are defined over that range. 40 = 2 * 20 (the value is squared in the
        // spec equation, and 20*log10 converts an amplitude ratio to decibels).
        private const double ConcaveScale = 40.0 / 96.0;

        /// <summary>
        /// The SoundFont concave curve over [0, 1]: 0 at 0, rising slowly then
        /// steeply to 1 at 1. Output is clamped to [0, 1].
        /// </summary>
        public static double Concave(double x)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;
            double v = -ConcaveScale * Math.Log10(1.0 - x);
            return v > 1.0 ? 1.0 : v;
        }

        /// <summary>
        /// The SoundFont convex curve over [0, 1]: the concave curve with its
        /// start and end points reversed (§8.2.4). Output is clamped to [0, 1].
        /// </summary>
        public static double Convex(double x)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;
            double v = 1.0 + ConcaveScale * Math.Log10(x);
            return v < 0.0 ? 0.0 : v;
        }

        /// <summary>
        /// Applies one of the four source-type curves to a value in [0, 1].
        /// </summary>
        public static double Curve(SourceTypeEnum type, double x)
        {
            switch (type)
            {
                case SourceTypeEnum.Linear: return x;
                case SourceTypeEnum.Concave: return Concave(x);
                case SourceTypeEnum.Convex: return Convex(x);
                case SourceTypeEnum.Switch: return x >= 0.5 ? 1.0 : 0.0;
                default: return x;
            }
        }

        /// <summary>
        /// Evaluates a modulator source: normalises <paramref name="raw"/> against
        /// <paramref name="max"/>, applies the source's direction, curve and
        /// polarity, and returns the multiplier — [0, 1] for a unipolar source,
        /// [-1, 1] for a bipolar one.
        /// </summary>
        public static double EvaluateSource(ModulatorType source, double raw, double max)
        {
            double u = max <= 0.0 ? 0.0 : raw / max;
            if (u < 0.0) u = 0.0; else if (u > 1.0) u = 1.0;
            double d = source.Direction ? 1.0 - u : u;
            double c = Curve(source.SourceType, d);
            return source.Polarity ? (2.0 * c - 1.0) : c;
        }

        /// <summary>
        /// Whether a source type is one the spec defines (linear / concave /
        /// convex / switch). Any other value means a reserved source-type bit is
        /// set, so the whole modulator must be ignored (§8.2.4).
        /// </summary>
        internal static bool IsKnownSourceType(SourceTypeEnum type) => (uint)type <= (uint)SourceTypeEnum.Switch;
    }

    /// <summary>
    /// The 10 implicit default modulators of SoundFont 2.04 (§8.4), built from
    /// their raw source enumerations. These are "default at the instrument level":
    /// every voice has them unless a file modulator with identical routing
    /// supersedes one (§9.5).
    /// </summary>
    internal static class SoundFontDefaultModulators
    {
        private static Modulator Mod(ushort source, GeneratorEnum destination, short amount,
            ushort amountSource = 0, TransformEnum transform = TransformEnum.Linear) => new()
        {
            SourceModulationData = new ModulatorType(source),
            DestinationGenerator = destination,
            Amount = amount,
            SourceModulationAmount = new ModulatorType(amountSource),
            SourceTransform = transform
        };

        /// <summary>
        /// The default modulators that target generator destinations the voice
        /// engine honours. §8.4.10 (pitch-wheel &#8594; initial pitch) is omitted:
        /// it is realised by the channel pitch-bend path
        /// (<see cref="MidiChannelState.PitchBendRatio"/>) rather than the
        /// modulator list, which keeps a single source of truth for pitch bend.
        /// </summary>
        public static readonly Modulator[] All =
        {
            // §8.4.1  Note-on velocity -> initial attenuation (negative unipolar concave)
            Mod(0x0502, GeneratorEnum.InitialAttenuation, 960),
            // §8.4.2  Note-on velocity -> initial filter cutoff (negative unipolar linear)
            Mod(0x0102, GeneratorEnum.InitialFilterCutoffFrequency, -2400),
            // §8.4.3  Channel pressure -> vibrato LFO pitch depth
            Mod(0x000D, GeneratorEnum.VibratoLFOToPitch, 50),
            // §8.4.4  CC1 (mod wheel) -> vibrato LFO pitch depth
            Mod(0x0081, GeneratorEnum.VibratoLFOToPitch, 50),
            // §8.4.5  CC7 (channel volume) -> initial attenuation. NB: the spec
            // prints the source as 0x0582, but 0x0582 & 0x7F = 2 (note-on
            // velocity), not the "index = 7" its own annotation states — a typo
            // for 0x0587 (CC7), which is what is used here.
            Mod(0x0587, GeneratorEnum.InitialAttenuation, 960),
            // §8.4.6  CC10 (pan) -> pan. NB: the spec text lists the destination as
            // "Initial Attenuation", but its prose ("added to the Pan generator
            // summing node", "1000 tenths of a percent panned-right") makes clear
            // this is a typo for Pan — honoured here as Pan.
            Mod(0x028A, GeneratorEnum.Pan, 1000),
            // §8.4.7  CC11 (expression) -> initial attenuation
            Mod(0x058B, GeneratorEnum.InitialAttenuation, 960),
            // §8.4.8  CC91 -> reverb effects send
            Mod(0x00DB, GeneratorEnum.ReverbEffectsSend, 200),
            // §8.4.9  CC93 -> chorus effects send
            Mod(0x00DD, GeneratorEnum.ChorusEffectsSend, 200),
        };
    }

    /// <summary>
    /// The resolved modulator list for one region: the default modulators merged
    /// with the region's instrument- and preset-level modulators per SoundFont
    /// 2.04 §9.5, ready to evaluate against live MIDI controller state.
    ///
    /// Combination: start from the defaults; each instrument modulator either
    /// supersedes a same-routing entry (a default or an earlier instrument
    /// modulator) or is appended — these form the <em>absolute</em> set. Preset
    /// modulators are resolved among themselves the same way and then
    /// <em>added</em> on top. Once combined, every surviving modulator simply sums
    /// its output into its destination, so the engine keeps one flat list.
    /// </summary>
    internal sealed class ModulatorSet
    {
        private readonly Modulator[] modulators;

        private ModulatorSet(Modulator[] modulators)
        {
            this.modulators = modulators;
        }

        public static ModulatorSet Build(SoundFontRegion region)
        {
            var instrumentLevel = new List<Modulator>(SoundFontDefaultModulators.All);
            foreach (var m in region.InstrumentModulators) Merge(instrumentLevel, m);

            var presetLevel = new List<Modulator>();
            foreach (var m in region.PresetModulators) Merge(presetLevel, m);

            var all = new Modulator[instrumentLevel.Count + presetLevel.Count];
            instrumentLevel.CopyTo(all, 0);
            presetLevel.CopyTo(all, instrumentLevel.Count);
            return new ModulatorSet(all);
        }

        // Replace an identically-routed entry (the more-local modulator wins), or
        // append. Skips modulators with a reserved source-type bit set (§8.2.4).
        private static void Merge(List<Modulator> list, Modulator m)
        {
            if (!IsUsable(m)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].HasIdenticalRouting(m)) { list[i] = m; return; }
            }
            list.Add(m);
        }

        private static bool IsUsable(Modulator m) =>
            m.SourceModulationData != null && m.SourceModulationAmount != null &&
            SoundFontModulatorMath.IsKnownSourceType(m.SourceModulationData.SourceType) &&
            SoundFontModulatorMath.IsKnownSourceType(m.SourceModulationAmount.SourceType);

        /// <summary>
        /// Sums every modulator's output into <paramref name="byDestination"/>,
        /// indexed by <see cref="GeneratorEnum"/>. The caller clears the array and
        /// then reads the destination slots it cares about. Evaluated at control
        /// rate against the channel's live controllers and the voice's fixed
        /// note-on velocity and key.
        /// </summary>
        public void Accumulate(MidiChannelState channel, int velocity, int key, double[] byDestination)
        {
            foreach (var m in modulators)
            {
                int destination = (int)m.DestinationGenerator;
                if ((uint)destination >= (uint)byDestination.Length) continue; // unsupported destination

                double primary = SourceValue(m.SourceModulationData, channel, velocity, key);
                double secondary = SourceValue(m.SourceModulationAmount, channel, velocity, key);
                double value = m.Amount * primary * secondary;
                if (m.SourceTransform == TransformEnum.AbsoluteValue) value = Math.Abs(value);
                byDestination[destination] += value;
            }
        }

        private static double SourceValue(ModulatorType source, MidiChannelState channel, int velocity, int key)
        {
            // "No controller" multiplies by 1: as the amount source it means "no
            // secondary control"; as the primary source it makes the modulator a
            // constant offset of its amount.
            if (!source.IsMidiContinuousController &&
                source.ControllerSource == ControllerSourceEnum.NoController)
            {
                return 1.0;
            }

            double raw;
            double max = 127.0;
            if (source.IsMidiContinuousController)
            {
                raw = channel.Controller(source.MidiContinuousControllerNumber);
            }
            else
            {
                switch (source.ControllerSource)
                {
                    case ControllerSourceEnum.NoteOnVelocity: raw = velocity; break;
                    case ControllerSourceEnum.NoteOnKeyNumber: raw = key; break;
                    case ControllerSourceEnum.ChannelPressure: raw = channel.ChannelPressure; break;
                    case ControllerSourceEnum.PitchWheel: raw = channel.PitchBend; max = 16383.0; break;
                    case ControllerSourceEnum.PitchWheelSensitivity: raw = channel.PitchBendRangeSemitones; break;
                    // poly pressure is not tracked per-note yet; treat as 0
                    case ControllerSourceEnum.PolyPressure: raw = 0.0; break;
                    default: return 1.0;
                }
            }

            return SoundFontModulatorMath.EvaluateSource(source, raw, max);
        }
    }
}
