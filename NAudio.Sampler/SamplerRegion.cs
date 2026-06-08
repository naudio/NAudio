using NAudio.SoundFont;

namespace NAudio.Sampler
{
    /// <summary>
    /// The format-neutral region the voice engine plays: a <see cref="SampleData"/>
    /// reference plus a resolved parameter set (carried as
    /// <see cref="SoundFontGenerators"/> in engine units — the neutral currency,
    /// per the sampler design), the SF2 modulator list, and a velocity-to-amplitude
    /// tracking amount.
    ///
    /// SoundFont and SFZ each project their resolved regions onto this; the voice
    /// reads only this, so the same engine plays both. The generator vector is a
    /// pragmatic neutral representation — SFZ fills the same slots (timecents,
    /// centibels, absolute cents, 0.1% pan) its opcodes map to.
    /// </summary>
    internal sealed class SamplerRegion
    {
        /// <summary>The sample to play.</summary>
        public SampleData Sample { get; init; }

        /// <summary>The resolved synthesis parameters, in SoundFont generator units.</summary>
        public SoundFontGenerators Generators { get; init; }

        /// <summary>
        /// The SF2 modulator list (defaults + file modulators). For formats that
        /// do not use SF2 modulators (e.g. SFZ Tier 1) this is empty.
        /// </summary>
        public ModulatorSet Modulators { get; init; }

        /// <summary>
        /// Velocity-to-amplitude tracking as a percentage (SFZ <c>amp_veltrack</c>).
        /// 0 means velocity does not affect gain here — used by SoundFont, where
        /// velocity drives attenuation through the modulator list instead.
        /// </summary>
        public float VelocityTrackingPercent { get; init; }

        /// <summary>Lowest MIDI key (inclusive) this region responds to.</summary>
        public byte LoKey { get; init; }
        /// <summary>Highest MIDI key (inclusive) this region responds to.</summary>
        public byte HiKey { get; init; }
        /// <summary>Lowest velocity (inclusive) this region responds to.</summary>
        public byte LoVelocity { get; init; }
        /// <summary>Highest velocity (inclusive) this region responds to.</summary>
        public byte HiVelocity { get; init; }

        /// <summary>The exclusive (choke) class, or 0 for none.</summary>
        public int ExclusiveClass => Generators.ExclusiveClass;

        /// <summary>Whether this region should sound for the given key and velocity.</summary>
        public bool Matches(int key, int velocity) =>
            key >= LoKey && key <= HiKey && velocity >= LoVelocity && velocity <= HiVelocity;
    }
}
