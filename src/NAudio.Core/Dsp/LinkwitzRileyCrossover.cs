using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Linkwitz–Riley 4th-order (24 dB/oct) crossover that splits a single channel into
    /// N contiguous bands at N-1 ascending crossover frequencies. Each split is two
    /// cascaded Butterworth biquads (LR4); a two-band split sums back to a
    /// magnitude-flat (all-pass) response. For more than two bands the split is serial,
    /// which carries a small recombination ripple — fine for band-wise dynamics, which
    /// is its purpose. A reusable building block for multiband compression and the
    /// de-esser.
    /// </summary>
    public sealed class LinkwitzRileyCrossover
    {
        private const float ButterworthQ = 0.70710678f;

        private readonly int sampleRate;
        private readonly float[] frequencies;
        private readonly BiQuadFilter[][] lowPass;  // [crossover][2 cascaded stages]
        private readonly BiQuadFilter[][] highPass;

        /// <summary>
        /// Creates a crossover.
        /// </summary>
        /// <param name="sampleRate">Sample rate in Hz. Must be positive.</param>
        /// <param name="crossoverFrequencies">Ascending crossover frequencies in Hz
        /// (each strictly between 0 and Nyquist). One frequency yields two bands.</param>
        public LinkwitzRileyCrossover(int sampleRate, params float[] crossoverFrequencies)
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
            ArgumentNullException.ThrowIfNull(crossoverFrequencies);
            if (crossoverFrequencies.Length < 1)
                throw new ArgumentException("At least one crossover frequency is required.", nameof(crossoverFrequencies));

            var nyquist = sampleRate * 0.5f;
            for (var i = 0; i < crossoverFrequencies.Length; i++)
            {
                if (crossoverFrequencies[i] <= 0f || crossoverFrequencies[i] >= nyquist)
                    throw new ArgumentOutOfRangeException(nameof(crossoverFrequencies),
                        "Each crossover frequency must be between 0 and Nyquist.");
                if (i > 0 && crossoverFrequencies[i] <= crossoverFrequencies[i - 1])
                    throw new ArgumentException("Crossover frequencies must be strictly ascending.", nameof(crossoverFrequencies));
            }

            this.sampleRate = sampleRate;
            frequencies = (float[])crossoverFrequencies.Clone();
            lowPass = new BiQuadFilter[frequencies.Length][];
            highPass = new BiQuadFilter[frequencies.Length][];
            Build();
        }

        /// <summary>Number of output bands (crossover count + 1).</summary>
        public int Bands => frequencies.Length + 1;

        /// <summary>
        /// Splits one input sample into <see cref="Bands"/> band outputs (low → high).
        /// </summary>
        public void Process(float input, Span<float> bandOutputs)
        {
            if (bandOutputs.Length < Bands)
                throw new ArgumentException($"bandOutputs must have at least {Bands} elements.", nameof(bandOutputs));

            var remainder = input;
            for (var i = 0; i < frequencies.Length; i++)
            {
                var low = lowPass[i][1].Transform(lowPass[i][0].Transform(remainder));
                var high = highPass[i][1].Transform(highPass[i][0].Transform(remainder));
                bandOutputs[i] = low;
                remainder = high;
            }
            bandOutputs[frequencies.Length] = remainder;
        }

        /// <summary>
        /// Clears the filter state.
        /// </summary>
        public void Reset() => Build();

        private void Build()
        {
            for (var i = 0; i < frequencies.Length; i++)
            {
                lowPass[i] = new[]
                {
                    BiQuadFilter.LowPassFilter(sampleRate, frequencies[i], ButterworthQ),
                    BiQuadFilter.LowPassFilter(sampleRate, frequencies[i], ButterworthQ)
                };
                highPass[i] = new[]
                {
                    BiQuadFilter.HighPassFilter(sampleRate, frequencies[i], ButterworthQ),
                    BiQuadFilter.HighPassFilter(sampleRate, frequencies[i], ButterworthQ)
                };
            }
        }
    }
}
