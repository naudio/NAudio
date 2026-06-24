using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Split-band de-esser. A Linkwitz–Riley crossover separates the sibilant high band,
    /// which is compressed when it exceeds the threshold; the low band passes through
    /// untouched and the two are recombined. More transparent than wideband ducking
    /// because only the harsh band is attenuated. Detection is channel-linked.
    /// </summary>
    public sealed class DeEsserEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Crossover", "Hz", 2000f, 16000f, () => CrossoverFrequency, v => CrossoverFrequency = v),
            EffectParameter.Continuous("Threshold", "dB", -60f, 0f, () => ThresholdDb, v => ThresholdDb = v),
            EffectParameter.Continuous("Ratio", "", 1f, 20f, () => Ratio, v => Ratio = v),
            EffectParameter.Continuous("Attack", "ms", 0.1f, 20f, () => AttackMs, v => AttackMs = v),
            EffectParameter.Continuous("Release", "ms", 10f, 500f, () => ReleaseMs, v => ReleaseMs = v),
            EffectParameter.Meter("Gain Reduction", "dB", 0f, 24f, () => GainReductionDb)
        };

        private LinkwitzRileyCrossover[] crossovers = Array.Empty<LinkwitzRileyCrossover>();
        private EnvelopeFollower reductionFollower;
        private float[] low = Array.Empty<float>();
        private float[] high = Array.Empty<float>();

        private float crossoverFrequency = 6000f;

        /// <summary>Crossover frequency separating the sibilant band, in Hz. Default 6000.</summary>
        public float CrossoverFrequency
        {
            get => crossoverFrequency;
            set
            {
                crossoverFrequency = value;
                if (WaveFormat != null)
                    BuildCrossovers();
            }
        }

        /// <summary>Sibilant-band threshold in dBFS. Default -30 dB.</summary>
        public float ThresholdDb { get; set; } = -30f;

        /// <summary>Compression ratio applied to the sibilant band (≥ 1). Default 4.</summary>
        public float Ratio { get; set; } = 4f;

        /// <summary>Attack time in milliseconds. Default 1 ms.</summary>
        public float AttackMs { get; set; } = 1f;

        /// <summary>Release time in milliseconds. Default 60 ms.</summary>
        public float ReleaseMs { get; set; } = 60f;

        /// <summary>The most recent sibilant-band gain reduction in dB (≥ 0), for metering.</summary>
        public float GainReductionDb { get; private set; }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            BuildCrossovers();
            reductionFollower = new EnvelopeFollower(AttackMs, ReleaseMs, format.SampleRate);
            low = new float[format.Channels];
            high = new float[format.Channels];
        }

        private void BuildCrossovers()
        {
            var freq = Math.Clamp(crossoverFrequency, 1000f, SampleRate * 0.5f - 1f);
            var built = new LinkwitzRileyCrossover[Channels];
            for (var ch = 0; ch < Channels; ch++)
                built[ch] = new LinkwitzRileyCrossover(SampleRate, freq);
            crossovers = built;
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            reductionFollower.AttackMilliseconds = AttackMs;
            reductionFollower.ReleaseMilliseconds = ReleaseMs;
            var ratio = Ratio < 1f ? 1f : Ratio;
            Span<float> bands = stackalloc float[2];

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var sibilantPeak = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    crossovers[ch].Process(buffer[i + ch], bands);
                    low[ch] = bands[0];
                    high[ch] = bands[1];
                    var a = MathF.Abs(bands[1]);
                    if (a > sibilantPeak)
                        sibilantPeak = a;
                }

                var keyDb = 20f * MathF.Log10(sibilantPeak < 1e-9f ? 1e-9f : sibilantPeak);
                var over = keyDb - ThresholdDb;
                var targetReduction = over > 0f ? over - over / ratio : 0f;
                var reduction = reductionFollower.ProcessRectified(targetReduction);
                GainReductionDb = reduction;

                var gain = MathF.Pow(10f, -reduction * (1f / 20f));
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] = low[ch] + high[ch] * gain;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            foreach (var crossover in crossovers)
                crossover.Reset();
            reductionFollower?.Reset();
            GainReductionDb = 0f;
        }
    }
}
