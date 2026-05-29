using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Chorus: a short LFO-modulated delay mixed with the dry signal to thicken and
    /// detune it. Classic single-LFO design applied to every channel.
    /// </summary>
    public sealed class ChorusEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Base Delay", "ms", 1f, 50f, () => BaseDelayMs, v => BaseDelayMs = v),
            EffectParameter.Continuous("Depth", "ms", 0f, 30f, () => DepthMs, v => DepthMs = v),
            EffectParameter.Continuous("Rate", "Hz", 0.05f, 10f, () => RateHz, v => RateHz = v),
            EffectParameter.Continuous("Feedback", "", 0f, 0.95f, () => Feedback, v => Feedback = v)
        };

        private Lfo lfo;
        private DelayLine[] lines = Array.Empty<DelayLine>();

        /// <summary>Centre delay in milliseconds. Default 15 ms.</summary>
        public float BaseDelayMs { get; set; } = 15f;

        /// <summary>Modulation depth in milliseconds. Default 8 ms.</summary>
        public float DepthMs { get; set; } = 8f;

        /// <summary>Modulation rate in Hz. Default 0.8 Hz.</summary>
        public float RateHz { get; set; } = 0.8f;

        /// <summary>Feedback amount, 0 to &lt; 1. Default 0.</summary>
        public float Feedback { get; set; }

        /// <summary>
        /// Creates a chorus with a 50/50 default mix.
        /// </summary>
        public ChorusEffect()
        {
            Mix = 0.5f;
        }

        /// <summary>
        /// Locks <see cref="RateHz"/> to a tempo and note division.
        /// </summary>
        public void SyncToTempo(double bpm, NoteDivision division)
            => RateHz = (float)TempoTime.Hertz(bpm, division);

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            lfo = new Lfo(format.SampleRate);
            var max = (int)((BaseDelayMs + DepthMs + 5f) * 0.001f * format.SampleRate) + 2;
            lines = new DelayLine[format.Channels];
            for (var ch = 0; ch < format.Channels; ch++)
                lines[ch] = new DelayLine(max);
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            lfo.FrequencyHz = RateHz <= 0f ? 0.01f : RateHz;
            var feedback = Math.Clamp(Feedback, 0f, 0.99f);
            var baseSamples = BaseDelayMs * 0.001f * SampleRate;
            var depthSamples = DepthMs * 0.001f * SampleRate;
            var maxSamples = lines[0].MaxDelaySamples;

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var mod = 0.5f * (1f + lfo.Process());
                var d = baseSamples + depthSamples * mod;
                if (d < 1f) d = 1f;
                else if (d > maxSamples - 1) d = maxSamples - 1;

                for (var ch = 0; ch < channels; ch++)
                {
                    var delayed = lines[ch].Read(d);
                    lines[ch].Write(buffer[i + ch] + delayed * feedback);
                    buffer[i + ch] = delayed;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            lfo?.Reset();
            foreach (var line in lines)
                line.Reset();
        }
    }
}
