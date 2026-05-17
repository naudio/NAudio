namespace NAudio.Effects
{
    /// <summary>
    /// One band of an <see cref="Equalizer"/>. Mutate the properties and call
    /// <see cref="Equalizer.Update"/> to apply the change (click-free).
    /// </summary>
    public sealed class EqualizerBand
    {
        /// <summary>
        /// Filter shape. Defaults to <see cref="EqualizerBandType.Peaking"/>.
        /// </summary>
        public EqualizerBandType Type { get; set; } = EqualizerBandType.Peaking;

        /// <summary>
        /// Centre or corner frequency in Hz.
        /// </summary>
        public float Frequency { get; set; } = 1000f;

        /// <summary>
        /// Quality factor for peaking / low-pass / high-pass / notch / band-pass /
        /// all-pass shapes. Higher is narrower. Ignored by the shelves.
        /// </summary>
        public float Q { get; set; } = 0.707f;

        /// <summary>
        /// Boost/cut in decibels for peaking and shelf shapes. Ignored by the others.
        /// </summary>
        public float GainDb { get; set; }

        /// <summary>
        /// Shelf slope for <see cref="EqualizerBandType.LowShelf"/> /
        /// <see cref="EqualizerBandType.HighShelf"/>. 1 is the steepest monotonic slope.
        /// Ignored by the other shapes.
        /// </summary>
        public float ShelfSlope { get; set; } = 1f;

        /// <summary>
        /// Creates a peaking band.
        /// </summary>
        public static EqualizerBand Peaking(float frequency, float q, float gainDb)
            => new EqualizerBand { Type = EqualizerBandType.Peaking, Frequency = frequency, Q = q, GainDb = gainDb };

        /// <summary>
        /// Creates a low-shelf band.
        /// </summary>
        public static EqualizerBand LowShelf(float frequency, float gainDb, float shelfSlope = 1f)
            => new EqualizerBand { Type = EqualizerBandType.LowShelf, Frequency = frequency, GainDb = gainDb, ShelfSlope = shelfSlope };

        /// <summary>
        /// Creates a high-shelf band.
        /// </summary>
        public static EqualizerBand HighShelf(float frequency, float gainDb, float shelfSlope = 1f)
            => new EqualizerBand { Type = EqualizerBandType.HighShelf, Frequency = frequency, GainDb = gainDb, ShelfSlope = shelfSlope };
    }
}
