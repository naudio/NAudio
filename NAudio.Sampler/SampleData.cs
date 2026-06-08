namespace NAudio.Sampler
{
    /// <summary>
    /// A format-neutral reference to a region's sample: the float buffer to read
    /// from, the playable bounds within it, loop points, source sample rate, and
    /// the pitch the recording sounds at. Both SoundFont (a slice of the shared
    /// sample pool) and SFZ (an individually loaded file) produce this so the
    /// voice engine never sees a file format.
    /// </summary>
    internal sealed class SampleData
    {
        /// <summary>The sample buffer (mono, -1..1). May be shared by many regions (SF2 pool).</summary>
        public float[] Data { get; init; }

        /// <summary>First sample index (inclusive) of the region within <see cref="Data"/>.</summary>
        public int Start { get; init; }

        /// <summary>End sample index (exclusive) of the region within <see cref="Data"/>.</summary>
        public int End { get; init; }

        /// <summary>Loop start index within <see cref="Data"/>.</summary>
        public int LoopStart { get; init; }

        /// <summary>Loop end index within <see cref="Data"/>.</summary>
        public int LoopEnd { get; init; }

        /// <summary>The sample's source/recording rate in Hz.</summary>
        public int SampleRate { get; init; }

        /// <summary>The MIDI key at which the sample plays back at its recorded pitch.</summary>
        public int RootKey { get; init; }

        /// <summary>A fixed pitch correction for the recording, in cents.</summary>
        public double PitchCorrectionCents { get; init; }
    }
}
