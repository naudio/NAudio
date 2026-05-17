using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Attack/release envelope follower (one-pole peak detector). The envelope rises
    /// towards the input magnitude with the attack time constant and falls with the
    /// release time constant. A core building block for dynamics processors (compressor,
    /// limiter, gate) and for synthesiser amplitude tracking. Allocation-free and
    /// denormal-safe.
    /// </summary>
    public sealed class EnvelopeFollower
    {
        private float envelope;
        private float attackCoefficient;
        private float releaseCoefficient;
        private float attackMs;
        private float releaseMs;
        private int sampleRate;

        /// <summary>
        /// Creates an envelope follower.
        /// </summary>
        /// <param name="attackMilliseconds">Attack time constant in milliseconds. Must be positive.</param>
        /// <param name="releaseMilliseconds">Release time constant in milliseconds. Must be positive.</param>
        /// <param name="sampleRate">Sample rate in Hz. Must be positive.</param>
        public EnvelopeFollower(float attackMilliseconds, float releaseMilliseconds, int sampleRate)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
            if (attackMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackMilliseconds), "Attack time must be positive");
            if (releaseMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(releaseMilliseconds), "Release time must be positive");
            this.sampleRate = sampleRate;
            attackMs = attackMilliseconds;
            releaseMs = releaseMilliseconds;
            RecomputeCoefficients();
        }

        /// <summary>
        /// Attack time constant in milliseconds.
        /// </summary>
        public float AttackMilliseconds
        {
            get => attackMs;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Attack time must be positive");
                attackMs = value;
                attackCoefficient = CoefficientFor(attackMs);
            }
        }

        /// <summary>
        /// Release time constant in milliseconds.
        /// </summary>
        public float ReleaseMilliseconds
        {
            get => releaseMs;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Release time must be positive");
                releaseMs = value;
                releaseCoefficient = CoefficientFor(releaseMs);
            }
        }

        /// <summary>
        /// Sample rate in Hz. Setting it recomputes the attack and release coefficients.
        /// </summary>
        public int SampleRate
        {
            get => sampleRate;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Sample rate must be positive");
                sampleRate = value;
                RecomputeCoefficients();
            }
        }

        /// <summary>
        /// The current envelope value.
        /// </summary>
        public float Envelope => envelope;

        /// <summary>
        /// Feeds one sample (its absolute value is taken) and returns the updated
        /// envelope.
        /// </summary>
        public float ProcessSample(float sample)
            => ProcessRectified(sample < 0f ? -sample : sample);

        /// <summary>
        /// Feeds one already-rectified, non-negative magnitude and returns the updated
        /// envelope. Use this when the caller already has a detector signal (for
        /// example an RMS estimate or a precomputed gain key).
        /// </summary>
        public float ProcessRectified(float magnitude)
        {
            var coefficient = magnitude > envelope ? attackCoefficient : releaseCoefficient;
            envelope = magnitude + coefficient * (envelope - magnitude);
            envelope = DenormalGuard.Flush(envelope);
            return envelope;
        }

        /// <summary>
        /// Resets the envelope to zero.
        /// </summary>
        public void Reset() => envelope = 0f;

        private void RecomputeCoefficients()
        {
            attackCoefficient = CoefficientFor(attackMs);
            releaseCoefficient = CoefficientFor(releaseMs);
        }

        private float CoefficientFor(float milliseconds)
            => MathF.Exp(-1f / (milliseconds * 0.001f * sampleRate));
    }
}
