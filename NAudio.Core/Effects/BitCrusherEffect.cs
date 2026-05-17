using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Lo-fi bit crusher: reduces amplitude resolution (bit depth) and, optionally,
    /// effective sample rate via sample-and-hold decimation. Per-channel state keeps a
    /// stereo image coherent.
    /// </summary>
    public sealed class BitCrusherEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Bit Depth", "bits", 1f, 32f, () => BitDepth, v => BitDepth = (int)MathF.Round(v)),
            EffectParameter.Continuous("Downsample", "x", 1f, 50f, () => SampleRateReduction, v => SampleRateReduction = (int)MathF.Round(v))
        };

        private float[] held = Array.Empty<float>();
        private int counter;
        private int bitDepth = 8;
        private int sampleRateReduction = 1;

        /// <summary>Quantisation resolution in bits, 1–32. Default 8.</summary>
        public int BitDepth
        {
            get => bitDepth;
            set
            {
                if (value < 1 || value > 32)
                    throw new ArgumentOutOfRangeException(nameof(value), "Bit depth must be 1–32");
                bitDepth = value;
            }
        }

        /// <summary>
        /// Sample-and-hold factor: 1 = none, N = hold each sample for N input samples
        /// (effective rate = sampleRate / N). Default 1.
        /// </summary>
        public int SampleRateReduction
        {
            get => sampleRateReduction;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Reduction factor must be at least 1");
                sampleRateReduction = value;
            }
        }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            held = new float[format.Channels];
            counter = 0;
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            var step = 2f / (1L << bitDepth); // 2^bits quantisation levels over [-1, 1]
            var hold = sampleRateReduction;

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var refresh = counter == 0;
                for (var ch = 0; ch < channels; ch++)
                {
                    if (refresh)
                    {
                        var x = buffer[i + ch];
                        if (x < -1f) x = -1f;
                        else if (x > 1f) x = 1f;
                        held[ch] = MathF.Round(x / step) * step;
                    }
                    buffer[i + ch] = held[ch];
                }
                if (++counter >= hold)
                    counter = 0;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            Array.Clear(held);
            counter = 0;
        }
    }
}
