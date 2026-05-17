using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Brick-wall peak limiter with look-ahead. The signal is delayed by the look-ahead
    /// time while a channel-linked peak detector sees the audio early, so gain is
    /// already reduced by the time a transient reaches the output — no overshoot, no
    /// audible pumping from a hard catch. Gain recovers with a smooth release.
    /// Reports its look-ahead as <see cref="AudioEffect.LatencySamples"/> for delay
    /// compensation.
    /// </summary>
    public sealed class LimiterEffect : AudioEffect
    {
        private DelayLine[] delays = Array.Empty<DelayLine>();
        private float peakEnvelope;
        private float releaseCoefficient;
        private int lookaheadSamples = 1;
        private float ceilingDb = -0.3f;
        private float ceilingLinear = 0.966f;
        private float releaseMs = 50f;
        private float lookaheadMs = 5f;

        /// <summary>Output ceiling in dBFS (peaks are held at or below this). Default -0.3 dB.</summary>
        public float CeilingDb
        {
            get => ceilingDb;
            set
            {
                ceilingDb = value;
                ceilingLinear = MathF.Pow(10f, value * (1f / 20f));
            }
        }

        /// <summary>Release time in milliseconds. Default 50 ms.</summary>
        public float ReleaseMs
        {
            get => releaseMs;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Release time must be positive");
                releaseMs = value;
                RecomputeRelease();
            }
        }

        /// <summary>Look-ahead time in milliseconds. Default 5 ms. Must be positive.</summary>
        public float LookaheadMs
        {
            get => lookaheadMs;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Look-ahead must be positive");
                lookaheadMs = value;
            }
        }

        /// <summary>The most recent gain reduction in dB (≥ 0), for metering.</summary>
        public float GainReductionDb { get; private set; }

        /// <inheritdoc />
        public override int LatencySamples => lookaheadSamples;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            lookaheadSamples = Math.Max(1, (int)MathF.Round(lookaheadMs * 0.001f * format.SampleRate));
            delays = new DelayLine[format.Channels];
            for (var ch = 0; ch < format.Channels; ch++)
                delays[ch] = new DelayLine(lookaheadSamples + 1);
            peakEnvelope = 0f;
            GainReductionDb = 0f;
            RecomputeRelease();
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var peak = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var a = MathF.Abs(buffer[i + ch]);
                    if (a > peak)
                        peak = a;
                }

                // Instant attack, smoothed release: the peak is seen `lookahead`
                // samples before the (delayed) sample it belongs to is output.
                peakEnvelope = MathF.Max(peak, peakEnvelope * releaseCoefficient);
                var gain = peakEnvelope > ceilingLinear ? ceilingLinear / peakEnvelope : 1f;
                GainReductionDb = gain < 1f ? -20f * MathF.Log10(gain) : 0f;

                for (var ch = 0; ch < channels; ch++)
                {
                    delays[ch].Write(buffer[i + ch]);
                    buffer[i + ch] = delays[ch].Read(lookaheadSamples + 1) * gain;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            foreach (var delay in delays)
                delay.Reset();
            peakEnvelope = 0f;
            GainReductionDb = 0f;
        }

        private void RecomputeRelease()
        {
            if (WaveFormat == null)
                return;
            releaseCoefficient = MathF.Exp(-1f / (releaseMs * 0.001f * SampleRate));
        }
    }
}
