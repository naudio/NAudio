using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Collapses low frequencies to mono while leaving higher frequencies stereo — the
    /// "bass mono" tool used to keep a mix's low end phase-coherent. Everything below
    /// <see cref="Frequency"/> is summed to the centre; above it the stereo image is
    /// untouched. Frequency changes crossfade so they do not click. Only stereo signals
    /// are affected; other channel counts pass through unchanged.
    /// </summary>
    public sealed class MonoMakerEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Frequency", "Hz", 20f, 1000f, () => Frequency, v => Frequency = v)
        };

        private CrossfadingBiQuadFilter sideLowPass;
        private float frequency = 120f;

        /// <summary>
        /// Frequencies below this (Hz) are made mono. Default 120 Hz. Values below 1 Hz are
        /// clamped to 1 Hz.
        /// </summary>
        public float Frequency
        {
            get => frequency;
            set
            {
                frequency = value < 1f ? 1f : value;
                if (sideLowPass != null)
                {
                    sideLowPass.ReplaceStandby(
                        BiQuadFilter.LowPassFilter(SampleRate, ClampToNyquist(frequency), 0.707f));
                    sideLowPass.BeginCrossfade();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            var f = ClampToNyquist(frequency);
            sideLowPass = new CrossfadingBiQuadFilter(
                BiQuadFilter.LowPassFilter(format.SampleRate, f, 0.707f),
                BiQuadFilter.LowPassFilter(format.SampleRate, f, 0.707f),
                Math.Max(1, format.SampleRate / 100));
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            if (Channels != 2)
                return;

            for (var i = 0; i + 1 < buffer.Length; i += 2)
            {
                var mid = (buffer[i] + buffer[i + 1]) * 0.5f;
                var side = (buffer[i] - buffer[i + 1]) * 0.5f;

                // Remove the low-frequency content of the side signal: that band
                // collapses to mono, higher frequencies keep their stereo width.
                var sideHigh = side - sideLowPass.Transform(side);

                buffer[i] = mid + sideHigh;
                buffer[i + 1] = mid - sideHigh;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            sideLowPass?.Reset();
        }

        private float ClampToNyquist(float value)
        {
            var maximum = SampleRate * 0.5f - 1f;
            return value > maximum ? maximum : value;
        }
    }
}
