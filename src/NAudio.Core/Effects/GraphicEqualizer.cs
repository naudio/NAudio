using System;

namespace NAudio.Effects
{
    /// <summary>
    /// Standard graphic-equaliser band layouts.
    /// </summary>
    public enum GraphicEqualizerLayout
    {
        /// <summary>Ten ISO octave bands (31.5 Hz … 16 kHz).</summary>
        TenBandOctave,
        /// <summary>Thirty-one ISO third-octave bands (20 Hz … 20 kHz).</summary>
        ThirtyOneBandThirdOctave
    }

    /// <summary>
    /// A graphic equaliser: a fixed set of ISO-standard peaking bands over the
    /// <see cref="Equalizer"/> engine, exposing one gain per band. Setting a band gain
    /// retunes click-free.
    /// </summary>
    public sealed class GraphicEqualizer : Equalizer
    {
        private static readonly float[] OctaveCentres =
            { 31.5f, 63f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f };

        private static readonly float[] ThirdOctaveCentres =
        {
            20f, 25f, 31.5f, 40f, 50f, 63f, 80f, 100f, 125f, 160f, 200f, 250f, 315f,
            400f, 500f, 630f, 800f, 1000f, 1250f, 1600f, 2000f, 2500f, 3150f, 4000f,
            5000f, 6300f, 8000f, 10000f, 12500f, 16000f, 20000f
        };

        /// <summary>
        /// Creates a graphic equaliser with the given band layout, all gains flat.
        /// </summary>
        public GraphicEqualizer(GraphicEqualizerLayout layout = GraphicEqualizerLayout.TenBandOctave)
            : base(BuildBands(layout))
        {
        }

        /// <summary>
        /// Number of bands.
        /// </summary>
        public int BandCount => Bands.Count;

        /// <summary>
        /// Centre frequency (Hz) of the band at <paramref name="index"/>.
        /// </summary>
        public float GetCentreFrequency(int index) => Bands[index].Frequency;

        /// <summary>
        /// The gain in decibels of the band at <paramref name="index"/>.
        /// </summary>
        public float GetBandGain(int index) => Bands[index].GainDb;

        /// <summary>
        /// Sets the gain in decibels of the band at <paramref name="index"/> and
        /// applies it (click-free).
        /// </summary>
        public void SetBandGain(int index, float gainDb)
        {
            Bands[index].GainDb = gainDb;
            Update();
        }

        private static EqualizerBand[] BuildBands(GraphicEqualizerLayout layout)
        {
            var centres = layout == GraphicEqualizerLayout.ThirtyOneBandThirdOctave
                ? ThirdOctaveCentres
                : OctaveCentres;
            // Q ≈ Fc / bandwidth: ~1.414 for octave-spaced, ~4.318 for third-octave.
            var q = layout == GraphicEqualizerLayout.ThirtyOneBandThirdOctave ? 4.318f : 1.414f;

            var bands = new EqualizerBand[centres.Length];
            for (var i = 0; i < centres.Length; i++)
                bands[i] = EqualizerBand.Peaking(centres[i], q, 0f);
            return bands;
        }
    }
}
