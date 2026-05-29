using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// One-pole exponential smoother for click-free parameter changes. Ramps a value
    /// towards a target with a configurable time constant, evaluated one sample at a
    /// time. Allocation-free.
    /// </summary>
    public sealed class ParameterSmoother
    {
        // A one-pole only approaches its target asymptotically and, in float, stalls a
        // few times 1e-5 short of it. Snapping (and reporting "settled") within an
        // inaudible margin lets the ramp actually finish — so IsSettled becomes true
        // and Current reaches the target exactly, which the AudioEffect fully-wet /
        // fully-dry fast paths rely on. 1e-4 on a 0..1 control is ≈ -80 dB.
        private const float SettleEpsilon = 1e-4f;

        private float current;
        private float target;
        private float coefficient = 1f;
        private float smoothingMs = 10f;
        private int sampleRate;

        /// <summary>
        /// The most recent smoothed value.
        /// </summary>
        public float Current => current;

        /// <summary>
        /// The value the smoother is ramping towards.
        /// </summary>
        public float Target => target;

        /// <summary>
        /// True when the smoothed value has effectively reached the target.
        /// </summary>
        public bool IsSettled => MathF.Abs(current - target) <= SettleEpsilon;

        /// <summary>
        /// Sets the sample rate and smoothing time and recomputes the one-pole
        /// coefficient. Does not change the current or target value.
        /// </summary>
        /// <param name="sampleRate">Sample rate in Hz. Must be positive.</param>
        /// <param name="smoothingMilliseconds">Approximate time to converge towards a new target. Must be positive.</param>
        public void Configure(int sampleRate, float smoothingMilliseconds = 10f)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
            if (smoothingMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(smoothingMilliseconds), "Smoothing time must be positive");
            this.sampleRate = sampleRate;
            smoothingMs = smoothingMilliseconds;
            RecomputeCoefficient();
        }

        /// <summary>
        /// Sets the target the smoother ramps towards.
        /// </summary>
        public void SetTarget(float value) => target = value;

        /// <summary>
        /// Immediately jumps the current and target value, with no ramp.
        /// </summary>
        public void Reset(float value)
        {
            current = value;
            target = value;
        }

        /// <summary>
        /// Advances one sample and returns the next smoothed value.
        /// </summary>
        public float Process()
        {
            current += coefficient * (target - current);
            if (MathF.Abs(target - current) <= SettleEpsilon)
                current = target;
            return current;
        }

        private void RecomputeCoefficient()
        {
            // 1 - e^(-1/N), where N is the time constant in samples: reaches ~63% of a
            // step within the smoothing time, asymptotically with no overshoot or click.
            var samples = smoothingMs * 0.001f * sampleRate;
            coefficient = samples > 0f ? 1f - MathF.Exp(-1f / samples) : 1f;
        }
    }
}
