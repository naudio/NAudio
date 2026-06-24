using System;

namespace NAudio.Sampler
{
    /// <summary>
    /// Conversions from natural synthesis units (seconds, 0..1 levels) into the
    /// SoundFont generator units the voice consumes. Shared by the SFZ and
    /// single-sample projections.
    /// </summary>
    internal static class GeneratorUnits
    {
        /// <summary>
        /// Seconds to timecents for an envelope stage. 0 (or less) maps to ~1 ms
        /// (the SoundFont minimum), so an instant stage does not become the
        /// 1-second default through the timecent round-trip.
        /// </summary>
        public static short ToTimecents(double seconds) =>
            seconds <= 0.0 ? (short)-12000 : Clamp16(1200.0 * Math.Log2(seconds));

        /// <summary>
        /// A sustain level (0..1) to the SoundFont sustain attenuation in
        /// centibels (gain = 10^(-cB/200)); 0 maps to full attenuation.
        /// </summary>
        public static short SustainCentibels(double sustain) =>
            sustain <= 0.0 ? (short)1440 : Clamp16(-200.0 * Math.Log10(sustain));

        /// <summary>Rounds and clamps a value to the signed-16-bit generator range.</summary>
        public static short Clamp16(double value)
        {
            if (value > short.MaxValue) return short.MaxValue;
            if (value < short.MinValue) return short.MinValue;
            return (short)Math.Round(value);
        }
    }
}
