namespace NAudio.SoundFont
{
    /// <summary>
    /// A single fully-resolved, playable SoundFont region: one sample, the
    /// key/velocity rectangle over which it sounds, and the accumulated
    /// generator values that apply (preset offsets already added to the
    /// instrument values, per SoundFont 2.04 §9.4). Produced by
    /// <see cref="SoundFontInstrumentResolver"/>.
    ///
    /// A region is the SoundFont equivalent of an SFZ <c>&lt;region&gt;</c>; the
    /// forthcoming format-neutral sampler model is a projection of this.
    /// </summary>
    public sealed class SoundFontRegion
    {
        internal SoundFontRegion(SampleHeader sample, SoundFontGenerators generators,
            byte lowKey, byte highKey, byte lowVelocity, byte highVelocity)
        {
            Sample = sample;
            Generators = generators;
            LowKey = lowKey;
            HighKey = highKey;
            LowVelocity = lowVelocity;
            HighVelocity = highVelocity;
        }

        /// <summary>The sample this region plays.</summary>
        public SampleHeader Sample { get; }

        /// <summary>The accumulated generator values that apply to this region.</summary>
        public SoundFontGenerators Generators { get; }

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
