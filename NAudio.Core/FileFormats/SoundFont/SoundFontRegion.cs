using System;
using System.Collections.Generic;

namespace NAudio.SoundFont
{
    /// <summary>
    /// A single fully-resolved, playable SoundFont region: one sample, the
    /// key/velocity rectangle over which it sounds, and the accumulated
    /// generator values that apply (preset offsets already added to the
    /// instrument values, per SoundFont 2.04 §9.4). Produced by
    /// <see cref="SoundFontInstrumentResolver"/>.
    ///
    /// A region is the SoundFont equivalent of an SFZ <c>&lt;region&gt;</c>;
    /// NAudio.Sampler's format-neutral region model is a projection of this.
    /// </summary>
    public sealed class SoundFontRegion
    {
        private static readonly Modulator[] NoModulators = Array.Empty<Modulator>();

        internal SoundFontRegion(SampleHeader sample, SoundFontGenerators generators,
            byte lowKey, byte highKey, byte lowVelocity, byte highVelocity,
            IReadOnlyList<Modulator> instrumentModulators = null,
            IReadOnlyList<Modulator> presetModulators = null)
        {
            Sample = sample;
            Generators = generators;
            LowKey = lowKey;
            HighKey = highKey;
            LowVelocity = lowVelocity;
            HighVelocity = highVelocity;
            InstrumentModulators = instrumentModulators ?? NoModulators;
            PresetModulators = presetModulators ?? NoModulators;
        }

        /// <summary>The sample this region plays.</summary>
        public SampleHeader Sample { get; }

        /// <summary>The accumulated generator values that apply to this region.</summary>
        public SoundFontGenerators Generators { get; }

        /// <summary>
        /// The instrument-level modulators that apply to this region (the global
        /// instrument zone's modulators followed by the local zone's), in the
        /// order they should be combined — later entries supersede earlier ones
        /// with identical routing (SoundFont 2.04 §9.5). These are <em>absolute</em>
        /// modulators: they replace the implicit default modulators of the same
        /// routing. Modulator <em>resolution</em> (merging with the defaults and
        /// evaluating against live controllers) is the synthesiser's job.
        /// </summary>
        public IReadOnlyList<Modulator> InstrumentModulators { get; }

        /// <summary>
        /// The preset-level modulators that apply to this region (global preset
        /// zone followed by local), ordered for §9.5 combination. These are
        /// <em>additive</em>: their effect is summed on top of the instrument-level
        /// result rather than replacing it.
        /// </summary>
        public IReadOnlyList<Modulator> PresetModulators { get; }

        /// <summary>Lowest MIDI key (inclusive) this region responds to.</summary>
        public byte LowKey { get; }

        /// <summary>Highest MIDI key (inclusive) this region responds to.</summary>
        public byte HighKey { get; }

        /// <summary>Lowest MIDI velocity (inclusive) this region responds to.</summary>
        public byte LowVelocity { get; }

        /// <summary>Highest MIDI velocity (inclusive) this region responds to.</summary>
        public byte HighVelocity { get; }

        /// <summary>
        /// Whether this region should sound for the given key and velocity.
        /// </summary>
        public bool Matches(int key, int velocity) =>
            key >= LowKey && key <= HighKey &&
            velocity >= LowVelocity && velocity <= HighVelocity;

        /// <summary>
        /// <see cref="object.ToString"/>
        /// </summary>
        public override string ToString() =>
            $"Region {Sample?.SampleName} keys {LowKey}-{HighKey} vel {LowVelocity}-{HighVelocity}";
    }
}
