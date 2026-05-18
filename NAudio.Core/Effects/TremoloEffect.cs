using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Tremolo (LFO amplitude modulation). With <see cref="AutoPan"/> enabled on a
    /// stereo signal it instead sweeps the signal between the speakers (constant-power
    /// auto-pan).
    /// </summary>
    public sealed class TremoloEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Depth", "", 0f, 1f, () => Depth, v => Depth = v),
            EffectParameter.Continuous("Rate", "Hz", 0.1f, 20f, () => RateHz, v => RateHz = v),
            EffectParameter.Choice("Waveform", new[] { "Sine", "Triangle", "Sawtooth", "Square", "Sample & Hold" },
                () => (int)Waveform, i => Waveform = (LfoWaveform)i),
            EffectParameter.Toggle("Auto-Pan", () => AutoPan, v => AutoPan = v)
        };

        private Lfo lfo;
        // Rounds the step edges of the Square / Sample-and-Hold LFO so amplitude
        // modulation doesn't click; ~3 ms barely touches the smooth waveforms at
        // LFO rates (corner ≈ 50 Hz, well above the 20 Hz max rate).
        private readonly ParameterSmoother modSmoother = new ParameterSmoother();

        /// <summary>Modulation depth, 0 (no effect) to 1 (full). Default 0.5.</summary>
        public float Depth { get; set; } = 0.5f;

        /// <summary>Modulation rate in Hz. Default 5 Hz.</summary>
        public float RateHz { get; set; } = 5f;

        /// <summary>LFO waveform. Default <see cref="LfoWaveform.Sine"/>.</summary>
        public LfoWaveform Waveform { get; set; } = LfoWaveform.Sine;

        /// <summary>When true (and stereo), auto-pan instead of amplitude tremolo.</summary>
        public bool AutoPan { get; set; }

        /// <summary>
        /// Locks <see cref="RateHz"/> to a tempo and note division.
        /// </summary>
        public void SyncRateToTempo(double bpm, NoteDivision division)
            => RateHz = (float)TempoTime.Hertz(bpm, division);

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            lfo = new Lfo(format.SampleRate);
            modSmoother.Configure(format.SampleRate, 3f);
            modSmoother.Reset(0f);
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            lfo.FrequencyHz = RateHz <= 0f ? 0.01f : RateHz;
            lfo.Waveform = Waveform;
            var depth = Math.Clamp(Depth, 0f, 1f);
            var autoPan = AutoPan && channels == 2;

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                modSmoother.SetTarget(lfo.Process());
                var osc = modSmoother.Process();
                if (autoPan)
                {
                    var theta = (osc * 0.5f + 0.5f) * (MathF.PI / 2f);
                    buffer[i] *= MathF.Cos(theta);
                    buffer[i + 1] *= MathF.Sin(theta);
                }
                else
                {
                    // Unipolar gain in [1 - depth, 1].
                    var gain = 1f - depth * (0.5f * (1f - osc));
                    for (var ch = 0; ch < channels; ch++)
                        buffer[i + ch] *= gain;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            lfo?.Reset();
            modSmoother.Reset(0f);
        }
    }
}
