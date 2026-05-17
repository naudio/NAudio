using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Feedback delay / echo with optional tempo sync, feedback damping and ping-pong.
    /// The delay time can be set directly in milliseconds or locked to a tempo and note
    /// division. A one-pole damping filter in the feedback path darkens successive
    /// repeats. In ping-pong mode (stereo) the echoes bounce between channels.
    /// </summary>
    public sealed class DelayEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Delay", "ms", 1f, 2000f, () => DelayMilliseconds, v => DelayMilliseconds = v),
            EffectParameter.Continuous("Feedback", "", 0f, 0.99f, () => Feedback, v => Feedback = v),
            EffectParameter.Continuous("Damping", "", 0f, 1f, () => Damping, v => Damping = v),
            EffectParameter.Toggle("Ping-Pong", () => PingPong, v => PingPong = v),
            EffectParameter.Toggle("Tempo Sync", () => TempoSync, v => TempoSync = v)
        };

        private const double MaxDelaySeconds = 5.0;

        private DelayLine[] lines = Array.Empty<DelayLine>();
        private float[] dampState = Array.Empty<float>();

        /// <summary>Delay time in milliseconds (used when <see cref="TempoSync"/> is false). Default 350 ms.</summary>
        public float DelayMilliseconds { get; set; } = 350f;

        /// <summary>Feedback amount, 0 to &lt; 1. Default 0.4.</summary>
        public float Feedback { get; set; } = 0.4f;

        /// <summary>Feedback-path damping, 0 (bright) to 1 (dark). Default 0.</summary>
        public float Damping { get; set; }

        /// <summary>When true the delay time follows <see cref="Bpm"/> and <see cref="Division"/>.</summary>
        public bool TempoSync { get; set; }

        /// <summary>Tempo in BPM, used when <see cref="TempoSync"/> is true. Default 120.</summary>
        public double Bpm { get; set; } = 120.0;

        /// <summary>Note division, used when <see cref="TempoSync"/> is true. Default quarter note.</summary>
        public NoteDivision Division { get; set; } = NoteDivision.Quarter;

        /// <summary>When true (and stereo), echoes alternate between left and right.</summary>
        public bool PingPong { get; set; }

        /// <summary>
        /// Creates a delay with a sensible default wet/dry mix.
        /// </summary>
        public DelayEffect()
        {
            Mix = 0.35f;
        }

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            var max = (int)(MaxDelaySeconds * format.SampleRate);
            lines = new DelayLine[format.Channels];
            for (var ch = 0; ch < format.Channels; ch++)
                lines[ch] = new DelayLine(max);
            dampState = new float[format.Channels];
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            var seconds = TempoSync ? TempoTime.Seconds(Bpm, Division) : DelayMilliseconds * 0.001;
            var maxSamples = lines[0].MaxDelaySamples;
            var delaySamples = (float)Math.Clamp(seconds * SampleRate, 1.0, maxSamples - 1.0);
            var feedback = Math.Clamp(Feedback, 0f, 0.99f);
            var dampCoeff = 1f - Math.Clamp(Damping, 0f, 1f) * 0.95f;

            var pingPong = PingPong && channels == 2;

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                if (pingPong)
                {
                    var dl = lines[0].Read(delaySamples);
                    var dr = lines[1].Read(delaySamples);
                    dampState[0] += dampCoeff * (dl - dampState[0]);
                    dampState[1] += dampCoeff * (dr - dampState[1]);
                    // Cross the feedback so repeats bounce L↔R.
                    lines[0].Write(buffer[i] + dampState[1] * feedback);
                    lines[1].Write(buffer[i + 1] + dampState[0] * feedback);
                    buffer[i] = dl;
                    buffer[i + 1] = dr;
                }
                else
                {
                    for (var ch = 0; ch < channels; ch++)
                    {
                        var delayed = lines[ch].Read(delaySamples);
                        dampState[ch] += dampCoeff * (delayed - dampState[ch]);
                        lines[ch].Write(buffer[i + ch] + dampState[ch] * feedback);
                        buffer[i + ch] = delayed;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            foreach (var line in lines)
                line.Reset();
            Array.Clear(dampState);
        }
    }
}
