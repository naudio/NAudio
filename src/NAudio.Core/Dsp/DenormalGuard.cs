using System.Runtime.CompilerServices;

namespace NAudio.Dsp
{
    /// <summary>
    /// Helpers for keeping denormalised (subnormal) floating-point values out of
    /// feedback paths. .NET exposes no portable flush-to-zero control, so feedback
    /// effects flush explicitly — subnormal arithmetic is otherwise orders of magnitude
    /// slower on most CPUs, which can stall an audio thread as a feedback tail decays.
    /// </summary>
    public static class DenormalGuard
    {
        // Below this magnitude a sample is inaudible; treating it as zero stops a
        // decaying feedback tail from ever entering the subnormal range.
        private const float Threshold = 1e-15f;

        /// <summary>
        /// Returns zero if the value is smaller in magnitude than an inaudible
        /// threshold (and therefore at risk of becoming a denormal), otherwise returns
        /// the value unchanged.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Flush(float value)
            => value < Threshold && value > -Threshold ? 0f : value;
    }
}
