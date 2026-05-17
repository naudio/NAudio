using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Phaser: a cascade of first-order all-pass stages whose corner frequency is swept
    /// by an LFO, with feedback. Mixing the phase-shifted signal with the dry signal
    /// creates moving notches in the spectrum.
    /// </summary>
    public sealed class PhaserEffect : AudioEffect
    {
        private Lfo lfo;
        private float[,] apX;   // per-channel, per-stage previous input
        private float[,] apY;   // per-channel, per-stage previous output
        private float[] lastOut = Array.Empty<float>();
        private int stages = 4;

        /// <summary>Number of all-pass stages (more = deeper effect). Default 4.</summary>
        public int Stages
        {
            get => stages;
            set
            {
                if (value < 1 || value > 24)
                    throw new ArgumentOutOfRangeException(nameof(value), "Stages must be 1–24");
                stages = value;
                if (WaveFormat != null)
                    AllocateState();
            }
        }

        /// <summary>Low end of the sweep in Hz. Default 300 Hz.</summary>
        public float MinFrequency { get; set; } = 300f;

        /// <summary>High end of the sweep in Hz. Default 1500 Hz.</summary>
        public float MaxFrequency { get; set; } = 1500f;

        /// <summary>Sweep rate in Hz. Default 0.5 Hz.</summary>
        public float RateHz { get; set; } = 0.5f;

        /// <summary>Feedback amount, -0.99 to 0.99. Default 0.3.</summary>
        public float Feedback { get; set; } = 0.3f;

        /// <summary>
        /// Creates a phaser with a 50/50 default mix.
        /// </summary>
        public PhaserEffect()
        {
            Mix = 0.5f;
        }

        /// <summary>
        /// Locks <see cref="RateHz"/> to a tempo and note division.
        /// </summary>
        public void SyncRateToTempo(double bpm, NoteDivision division)
            => RateHz = (float)TempoTime.Hertz(bpm, division);

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            lfo = new Lfo(format.SampleRate);
            AllocateState();
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            lfo.FrequencyHz = RateHz <= 0f ? 0.01f : RateHz;
            var feedback = Math.Clamp(Feedback, -0.99f, 0.99f);
            var nyquist = SampleRate * 0.5f;
            var lo = Math.Clamp(MathF.Min(MinFrequency, MaxFrequency), 20f, nyquist - 1f);
            var hi = Math.Clamp(MathF.Max(MinFrequency, MaxFrequency), 20f, nyquist - 1f);

            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var mod = 0.5f * (1f + lfo.Process());
                var fc = lo + (hi - lo) * mod;
                // First-order all-pass coefficient for this corner frequency.
                var t = MathF.Tan(MathF.PI * fc / SampleRate);
                var a1 = (t - 1f) / (t + 1f);

                for (var ch = 0; ch < channels; ch++)
                {
                    var x = buffer[i + ch] + feedback * lastOut[ch];
                    for (var s = 0; s < stages; s++)
                    {
                        var y = a1 * x + apX[ch, s] - a1 * apY[ch, s];
                        apX[ch, s] = x;
                        apY[ch, s] = y;
                        x = y;
                    }
                    lastOut[ch] = x;
                    buffer[i + ch] = x;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            lfo?.Reset();
            if (apX != null)
                Array.Clear(apX);
            if (apY != null)
                Array.Clear(apY);
            Array.Clear(lastOut);
        }

        private void AllocateState()
        {
            apX = new float[Channels, stages];
            apY = new float[Channels, stages];
            lastOut = new float[Channels];
        }
    }
}
