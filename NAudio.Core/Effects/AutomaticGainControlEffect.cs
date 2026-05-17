using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Automatic gain control: a slow leveller that drives the signal towards a target
    /// loudness. Gain reduction (too loud) reacts faster than gain boost (too quiet) so
    /// it does not pump, and — with <see cref="UseVoiceDetection"/> — gain is frozen
    /// while no voice is present so background noise is not amplified in pauses. Pair
    /// with a <see cref="LimiterEffect"/> downstream for a brick-wall safety ceiling.
    /// </summary>
    public sealed class AutomaticGainControlEffect : AudioEffect
    {
        private VoiceActivityDetector vad;
        private float meanSquare;
        private float rmsCoefficient;
        private float gainDb;
        private float attackCoefficient;
        private float releaseCoefficient;
        private float attackMs = 50f;
        private float releaseMs = 400f;

        /// <summary>Target RMS level in dBFS. Default -18 dB.</summary>
        public float TargetDb { get; set; } = -18f;

        /// <summary>Maximum boost in dB. Default 30 dB.</summary>
        public float MaxGainDb { get; set; } = 30f;

        /// <summary>Maximum attenuation in dB (negative). Default -20 dB.</summary>
        public float MinGainDb { get; set; } = -20f;

        /// <summary>RMS detector window in milliseconds. Default 50 ms.</summary>
        public float RmsWindowMs { get; set; } = 50f;

        /// <summary>When true, gain is held while no voice is detected. Default true.</summary>
        public bool UseVoiceDetection { get; set; } = true;

        /// <summary>Time (ms) to pull gain down when too loud. Default 50 ms.</summary>
        public float AttackMs
        {
            get => attackMs;
            set { attackMs = value; RecomputeTimes(); }
        }

        /// <summary>Time (ms) to bring gain up when too quiet. Default 400 ms.</summary>
        public float ReleaseMs
        {
            get => releaseMs;
            set { releaseMs = value; RecomputeTimes(); }
        }

        /// <summary>The current applied gain in dB, for metering.</summary>
        public float GainDb => gainDb;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            vad = new VoiceActivityDetector(format.SampleRate);
            rmsCoefficient = 1f - MathF.Exp(-1f / (RmsWindowMs * 0.001f * format.SampleRate));
            meanSquare = 0f;
            gainDb = 0f;
            RecomputeTimes();
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var mono = 0f;
                var sumSquares = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var s = buffer[i + ch];
                    mono += s;
                    sumSquares += s * s;
                }
                meanSquare += rmsCoefficient * (sumSquares / channels - meanSquare);

                var voiced = vad.Process(mono);
                if (!UseVoiceDetection || voiced)
                {
                    var rmsDb = 10f * MathF.Log10(meanSquare < 1e-12f ? 1e-12f : meanSquare);
                    var desiredGainDb = Math.Clamp(TargetDb - rmsDb, MinGainDb, MaxGainDb);
                    // Reducing gain (signal too loud) uses the faster attack coefficient.
                    var coeff = desiredGainDb < gainDb ? attackCoefficient : releaseCoefficient;
                    gainDb = desiredGainDb + coeff * (gainDb - desiredGainDb);
                }

                var gain = MathF.Pow(10f, gainDb * (1f / 20f));
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] *= gain;
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            vad?.Reset();
            meanSquare = 0f;
            gainDb = 0f;
        }

        private void RecomputeTimes()
        {
            if (WaveFormat == null)
                return;
            attackCoefficient = CoefficientFor(attackMs);
            releaseCoefficient = CoefficientFor(releaseMs);
        }

        private float CoefficientFor(float milliseconds)
        {
            if (milliseconds <= 0f)
                return 0f;
            return MathF.Exp(-1f / (milliseconds * 0.001f * SampleRate));
        }
    }
}
