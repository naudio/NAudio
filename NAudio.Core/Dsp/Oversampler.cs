using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Integer-factor (1×, 2× or 4×) oversampler for a single channel. Zero-stuffs and
    /// low-pass filters on the way up, low-pass filters and decimates on the way down,
    /// so a non-linear stage (saturation, clipping) can run at a higher rate where the
    /// alias products it generates fall above the original Nyquist and are filtered
    /// out. A factor of 1 is a transparent pass-through.
    /// </summary>
    public sealed class Oversampler
    {
        private readonly int factor;
        private readonly int highRate;
        private readonly float cutoff;
        private readonly BiQuadFilter[] upFilters;
        private readonly BiQuadFilter[] downFilters;

        /// <summary>
        /// Creates an oversampler.
        /// </summary>
        /// <param name="factor">Oversampling factor: 1, 2 or 4.</param>
        /// <param name="sampleRate">The (base) sample rate in Hz.</param>
        public Oversampler(int factor, int sampleRate)
        {
            if (factor != 1 && factor != 2 && factor != 4)
                throw new ArgumentOutOfRangeException(nameof(factor), "Factor must be 1, 2 or 4");
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive");
            this.factor = factor;

            if (factor == 1)
            {
                upFilters = Array.Empty<BiQuadFilter>();
                downFilters = Array.Empty<BiQuadFilter>();
                return;
            }

            // Anti-alias/anti-image low-pass a little below the base Nyquist, cascaded
            // for a steeper (~24 dB/oct) roll-off. Coefficients are at the high rate.
            highRate = sampleRate * factor;
            cutoff = sampleRate * 0.5f * 0.9f;
            upFilters = new[]
            {
                BiQuadFilter.LowPassFilter(highRate, cutoff, 0.707f),
                BiQuadFilter.LowPassFilter(highRate, cutoff, 0.707f)
            };
            downFilters = new[]
            {
                BiQuadFilter.LowPassFilter(highRate, cutoff, 0.707f),
                BiQuadFilter.LowPassFilter(highRate, cutoff, 0.707f)
            };
        }

        /// <summary>
        /// The oversampling factor.
        /// </summary>
        public int Factor => factor;

        /// <summary>
        /// Upsamples one input sample into <paramref name="destination"/>, which must be
        /// at least <see cref="Factor"/> long.
        /// </summary>
        public void Upsample(float input, Span<float> destination)
        {
            if (destination.Length < factor)
                throw new ArgumentException("Destination must be at least Factor samples long.", nameof(destination));

            if (factor == 1)
            {
                destination[0] = input;
                return;
            }

            for (var i = 0; i < factor; i++)
            {
                // Zero-stuff (keep unity gain by scaling the kept sample by the factor),
                // then reconstruct with the low-pass cascade.
                var s = i == 0 ? input * factor : 0f;
                s = upFilters[0].Transform(s);
                destination[i] = upFilters[1].Transform(s);
            }
        }

        /// <summary>
        /// Low-pass filters the high-rate samples and returns the single decimated
        /// output sample. <paramref name="source"/> must be exactly <see cref="Factor"/>
        /// long.
        /// </summary>
        public float Downsample(ReadOnlySpan<float> source)
        {
            if (source.Length < factor)
                throw new ArgumentException("Source must be at least Factor samples long.", nameof(source));

            if (factor == 1)
                return source[0];

            var kept = 0f;
            for (var i = 0; i < factor; i++)
            {
                var s = downFilters[0].Transform(source[i]);
                s = downFilters[1].Transform(s);
                if (i == 0)
                    kept = s;
            }
            return kept;
        }

        /// <summary>
        /// Clears the anti-alias filter state.
        /// </summary>
        public void Reset()
        {
            if (factor == 1)
                return;
            // Re-applying coefficients zeroes the biquad state (see PR #1259).
            for (var i = 0; i < upFilters.Length; i++)
            {
                upFilters[i].SetLowPassFilter(highRate, cutoff, 0.707f);
                downFilters[i].SetLowPassFilter(highRate, cutoff, 0.707f);
            }
        }
    }
}
