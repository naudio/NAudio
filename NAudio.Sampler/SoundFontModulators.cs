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
    /// the spec, not sample-identity with any particular synth (the fidelity bar
    /// stated in Docs/Architecture/SamplerDesign.md).
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
        /// [-1, 1] for a bipolar one. Bipolar concave/convex follow the §8.2.4
        /// figures: zero at the controller centre, odd-symmetric to ±1 at the
        /// extremes (matching FluidSynth's reading of the spec).
        /// </summary>
        public static double EvaluateSource(ModulatorType source, double raw, double max)
        {
            double u = max <= 0.0 ? 0.0 : raw / max;
            if (u < 0.0) u = 0.0; else if (u > 1.0) u = 1.0;

            if (source.Polarity &&
                (source.SourceType == SourceTypeEnum.Concave || source.SourceType == SourceTypeEnum.Convex))
            {
                // SF2.04 §8.2.4: the bipolar concave/convex curves are zero at the
                // controller centre and odd-symmetric — sign(2u−1)·curve(|2u−1|),
                // with the negative direction mirroring the sign. The unipolar
                // 2·curve(u)−1 mapping (correct for linear/switch) is wrong here:
                // it left a bipolar-concave source at ≈ −0.75 at centre.
                double b = 2.0 * u - 1.0;
                double value = b >= 0.0
                    ? Curve(source.SourceType, b)
                    : -Curve(source.SourceType, -b);
                return source.Direction ? -value : value;
            }

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
        // Per-modulator pass assignment, computed once at Build: a modulator is
        // evaluated in the static pass (once, at note-on) when its sources are
        // note-fixed (velocity/key/constant) AND every contributor to its
        // destination is too — otherwise it joins the dynamic pass, which is
        // re-run only when the channel state changes. Keeping a mixed
        // destination's static contributors in the dynamic pass preserves the
        // original per-destination summation order exactly, so splitting the
        // passes cannot drift the double-precision sums.
        private readonly bool[] staticPass;

        /// <summary>
        /// Whether any modulator reads a live channel source (CC, pitch wheel,
        /// bend range, channel pressure). When false the whole set is note-fixed
        /// and never needs re-evaluating after note-on.
        /// </summary>
        public bool HasDynamicModulators { get; }

        /// <summary>
        /// Whether a dynamic (channel-driven) modulator routes into the filter
        /// (initialFilterFc or initialFilterQ) with a non-zero amount — i.e. the
        /// filter cutoff can change after note-on through the modulator list.
        /// Used both to skip retunes and to force the filter ACTIVE even when the
        /// note-on cutoff is fully open (a CC-driven brightness modulator must be
        /// able to pull an open cutoff down later).
        /// </summary>
        public bool HasDynamicFilterRouting { get; }

        private ModulatorSet(Modulator[] modulators)
        {
            this.modulators = modulators;
            staticPass = new bool[modulators.Length];

            // a destination is "mixed" if any contributor reads channel state;
            // its static contributors must then sum in the dynamic pass to keep
            // the original (flat list) summation order per destination
            Span<bool> destinationDynamic = stackalloc bool[(int)GeneratorEnum.UnusedEnd + 1];
            bool anyDynamic = false;
            bool dynamicFilter = false;
            foreach (var m in modulators)
            {
                if (IsStaticModulator(m)) continue;
                anyDynamic = true;
                int destination = (int)m.DestinationGenerator;
                if ((uint)destination < (uint)destinationDynamic.Length) destinationDynamic[destination] = true;
                if (m.Amount != 0 &&
                    (m.DestinationGenerator == GeneratorEnum.InitialFilterCutoffFrequency ||
                     m.DestinationGenerator == GeneratorEnum.InitialFilterQ))
                {
                    dynamicFilter = true;
                }
            }
            for (int i = 0; i < modulators.Length; i++)
            {
                int destination = (int)modulators[i].DestinationGenerator;
                bool mixedDestination = (uint)destination < (uint)destinationDynamic.Length && destinationDynamic[destination];
                staticPass[i] = IsStaticModulator(modulators[i]) && !mixedDestination;
            }
            HasDynamicModulators = anyDynamic;
            HasDynamicFilterRouting = dynamicFilter;
        }

        // whether both sources are fixed for the lifetime of a note (velocity,
        // key number, "no controller" constants); unknown/unsupported sources
        // never get here — Build rejects their modulators outright (§7.4)
        private static bool IsStaticModulator(Modulator m) =>
            IsStaticSource(m.SourceModulationData) && IsStaticSource(m.SourceModulationAmount);

        private static bool IsStaticSource(ModulatorType source)
        {
            if (source.IsMidiContinuousController) return false;
            switch (source.ControllerSource)
            {
                case ControllerSourceEnum.ChannelPressure:
                case ControllerSourceEnum.PitchWheel:
                case ControllerSourceEnum.PitchWheelSensitivity:
                    return false;
                default:
                    return true; // velocity, key, no-controller
            }
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
        // append. Skips modulators with a reserved source-type bit set (§8.2.4)
        // or an unknown/unsupported source enumeration (§7.4, §8.2).
        private static void Merge(List<Modulator> list, Modulator m)
        {
            if (!IsUsable(m)) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].HasIdenticalRouting(m)) { list[i] = m; return; }
            }
            list.Add(m);
        }

        // SF2.04 §7.4: "If the source enumerator contains an illegal or
        // unsupported value, the modulator should be ignored." Applies to both
        // the primary and the amount source — a junk enumeration must disable
        // the modulator, not degenerate into a constant offset of its amount.
        private static bool IsUsable(Modulator m) =>
            m.SourceModulationData != null && m.SourceModulationAmount != null &&
            SoundFontModulatorMath.IsKnownSourceType(m.SourceModulationData.SourceType) &&
            SoundFontModulatorMath.IsKnownSourceType(m.SourceModulationAmount.SourceType) &&
            IsSupportedSource(m.SourceModulationData) &&
            IsSupportedSource(m.SourceModulationAmount);

        private static bool IsSupportedSource(ModulatorType source)
        {
            if (source.IsMidiContinuousController)
                return IsAllowedCcSource(source.MidiContinuousControllerNumber);
            switch (source.ControllerSource)
            {
                case ControllerSourceEnum.NoController: // the constant 1 — §9.5.1 default amount-source behaviour
                case ControllerSourceEnum.NoteOnVelocity:
                case ControllerSourceEnum.NoteOnKeyNumber:
                case ControllerSourceEnum.ChannelPressure:
                case ControllerSourceEnum.PitchWheel:
                case ControllerSourceEnum.PitchWheelSensitivity:
                    return true;
                default:
                    // Poly pressure (10) is defined by §8.2.1 but per-note pressure
                    // is not tracked yet, so a modulator naming it is disabled until
                    // it is (previously it evaluated as a bogus constant 0 — a
                    // bipolar source then contributed −1 × amount forever). The
                    // Link source (127) is unsupported, and any other value is an
                    // unknown enumeration; all must be ignored per §7.4/§8.2.
                    return false;
            }
        }

        // SF2.04 §8.2.2 prohibits the CCs with reserved channel-level meanings as
        // modulator sources: 0/32 (bank select), 6/38 (data entry), 98–101
        // (NRPN/RPN select) and 120–127 (channel mode messages). A modulator
        // naming one must be ignored entirely.
        private static bool IsAllowedCcSource(int cc)
        {
            switch (cc)
            {
                case 0:
                case 6:
                case 32:
                case 38:
                case 98:
                case 99:
                case 100:
                case 101:
                    return false;
                default:
                    return cc < 120; // 120–127 prohibited; >127 cannot occur (7-bit field)
            }
        }

        /// <summary>
        /// Sums every modulator's output into <paramref name="byDestination"/>,
        /// indexed by <see cref="GeneratorEnum"/>. The caller clears the array and
        /// then reads the destination slots it cares about. Equivalent to the
        /// static pass plus the dynamic pass; kept for callers (and tests) that
        /// want a single full evaluation.
        /// </summary>
        public void Accumulate(MidiChannelState channel, int velocity, int key, double[] byDestination)
        {
            for (int i = 0; i < modulators.Length; i++)
                AccumulateOne(modulators[i], channel, velocity, key, byDestination);
        }

        /// <summary>
        /// Sums the note-fixed modulators (the static pass) into
        /// <paramref name="byDestination"/>. Evaluated once at note-on into the
        /// voice's baseline; the channel is read only for pass-irrelevant sources
        /// and never changes the result.
        /// </summary>
        public void AccumulateStatic(MidiChannelState channel, int velocity, int key, double[] byDestination)
        {
            for (int i = 0; i < modulators.Length; i++)
                if (staticPass[i])
                    AccumulateOne(modulators[i], channel, velocity, key, byDestination);
        }

        /// <summary>
        /// Sums the channel-dependent modulators (the dynamic pass, including the
        /// static contributors of mixed destinations — see the pass-assignment
        /// comment) on top of the static baseline. Re-run only when
        /// <see cref="MidiChannelState.Version"/> changed.
        /// </summary>
        public void AccumulateDynamic(MidiChannelState channel, int velocity, int key, double[] byDestination)
        {
            for (int i = 0; i < modulators.Length; i++)
                if (!staticPass[i])
                    AccumulateOne(modulators[i], channel, velocity, key, byDestination);
        }

        private static void AccumulateOne(Modulator m, MidiChannelState channel, int velocity, int key,
            double[] byDestination)
        {
            int destination = (int)m.DestinationGenerator;
            if ((uint)destination >= (uint)byDestination.Length) return; // unsupported destination

            double primary = SourceValue(m.SourceModulationData, channel, velocity, key);
            double secondary = SourceValue(m.SourceModulationAmount, channel, velocity, key);
            double value = m.Amount * primary * secondary;
            if (m.SourceTransform == TransformEnum.AbsoluteValue) value = Math.Abs(value);
            byDestination[destination] += value;
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
                    // pitch-bend sensitivity (RPN 0, decoded by MidiChannelState)
                    // normalised over the spec's 0..127 semitone span
                    case ControllerSourceEnum.PitchWheelSensitivity: raw = channel.PitchBendRangeSemitones; break;
                    default:
                        // unreachable: Build rejects modulators whose source is
                        // unknown or unsupported (poly pressure, Link, reserved
                        // values) per §7.4 — contribute nothing if ever hit
                        return 0.0;
                }
            }

            return SoundFontModulatorMath.EvaluateSource(source, raw, max);
        }
    }
}
