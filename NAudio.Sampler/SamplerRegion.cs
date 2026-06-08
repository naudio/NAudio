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

        /// <summary>The resonant filter shape (default low-pass).</summary>
        public SamplerFilterType FilterType { get; init; }

        /// <summary>
        /// How a triggered region treats note-off: false (default) follows the amp
        /// envelope's release; true (SFZ <c>loop_mode=one_shot</c>) plays to the end
        /// of the sample regardless of note-off.
        /// </summary>
        public bool IgnoreNoteOff { get; init; }

        /// <summary>
        /// The group whose sounding voices this region silences when it starts
        /// (SFZ <c>off_by</c>); 0 for none. For a self-choking group (SoundFont
        /// <c>exclusiveClass</c>) <see cref="Group"/> and this are equal.
        /// </summary>
        public int OffByGroup { get; init; }

        /// <summary>
        /// When the region triggers (SFZ <c>trigger</c>). Note-on triggers
        /// (<see cref="SamplerTrigger.Attack"/>/<see cref="SamplerTrigger.First"/>/
        /// <see cref="SamplerTrigger.Legato"/>) sound on note-on; <see cref="SamplerTrigger.Release"/>
        /// sounds on note-off.
        /// </summary>
        public SamplerTrigger Trigger { get; init; }

        /// <summary>Lowest MIDI key (inclusive) this region responds to.</summary>
        public byte LoKey { get; init; }
        /// <summary>Highest MIDI key (inclusive) this region responds to.</summary>
        public byte HiKey { get; init; }
        /// <summary>Lowest velocity (inclusive) this region responds to.</summary>
        public byte LoVelocity { get; init; }
        /// <summary>Highest velocity (inclusive) this region responds to.</summary>
        public byte HiVelocity { get; init; }

        /// <summary>
        /// The choke group this region's voices belong to (SoundFont
        /// <c>exclusiveClass</c>, SFZ <c>group</c>); 0 for none. A starting voice
        /// silences sounding voices whose <see cref="Group"/> equals some region's
        /// <see cref="OffByGroup"/>.
        /// </summary>
        public int Group { get; init; }

        /// <summary>Whether this region should sound for the given key and velocity.</summary>
        public bool Matches(int key, int velocity) =>
            key >= LoKey && key <= HiKey && velocity >= LoVelocity && velocity <= HiVelocity;
    }
}
