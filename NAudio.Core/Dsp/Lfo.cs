using System;

namespace NAudio.Dsp
{
    /// <summary>
    /// Low-frequency oscillator waveform.
    /// </summary>
    public enum LfoWaveform
    {
        /// <summary>Sine.</summary>
        Sine,
        /// <summary>Triangle.</summary>
        Triangle,
        /// <summary>Rising sawtooth.</summary>
        Sawtooth,
        /// <summary>Square.</summary>
        Square,
        /// <summary>Sample-and-hold (stepped random).</summary>
        SampleAndHold
    }

    /// <summary>
    /// A bipolar (-1..+1) low-frequency oscillator for modulation (chorus, flanger,
    /// phaser, tremolo, auto-pan). Allocation-free; the sample-and-hold waveform uses a
    /// deterministic generator so output is reproducible.
    /// </summary>
    public sealed class Lfo
    {
        private uint rngState = 0x9E3779B9;
        private float phase;
        private float increment;
        private float sampleHoldValue;
        private int sampleRate;
        private float frequencyHz = 1f;

        /// <summary>
        /// Creates an LFO.
        /// </summary>
        /// <param name="sampleRate">Sample rate in Hz. Must be positive.</param>
        public Lfo(int sampleRate)
        {
            SampleRate = sampleRate;
            RecomputeIncrement();
            sampleHoldValue = NextRandom();
        }

        /// <summary>Oscillator waveform. Default <see cref="LfoWaveform.Sine"/>.</summary>
        public LfoWaveform Waveform { get; set; } = LfoWaveform.Sine;

        /// <summary>Oscillation frequency in Hz. Must be positive.</summary>
        public float FrequencyHz
        {
            get => frequencyHz;
            set
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Frequency must be positive");
                frequencyHz = value;
                RecomputeIncrement();
            }
        }

        /// <summary>Sample rate in Hz. Must be positive.</summary>
        public int SampleRate
        {
            get => sampleRate;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Sample rate must be positive");
                sampleRate = value;
                RecomputeIncrement();
            }
        }

        /// <summary>
        /// Sets <see cref="FrequencyHz"/> from a tempo and note division.
        /// </summary>
        public void SyncToTempo(double bpm, NoteDivision division)
            => FrequencyHz = (float)TempoTime.Hertz(bpm, division);

        /// <summary>
        /// Advances one sample and returns the next oscillator value in [-1, 1].
        /// </summary>
        public float Process()
        {
            float value;
            switch (Waveform)
            {
                case LfoWaveform.Triangle:
                    value = 4f * MathF.Abs(phase - 0.5f) - 1f;
                    break;
                case LfoWaveform.Sawtooth:
                    value = 2f * phase - 1f;
                    break;
                case LfoWaveform.Square:
                    value = phase < 0.5f ? 1f : -1f;
                    break;
                case LfoWaveform.SampleAndHold:
                    value = sampleHoldValue;
                    break;
                default:
                    value = MathF.Sin(2f * MathF.PI * phase);
                    break;
            }

            phase += increment;
            if (phase >= 1f)
            {
                phase -= 1f;
                sampleHoldValue = NextRandom();
            }
            return value;
        }

        /// <summary>
        /// Resets the phase (and re-seeds the sample-and-hold generator).
        /// </summary>
        public void Reset()
        {
            phase = 0f;
            rngState = 0x9E3779B9;
            sampleHoldValue = NextRandom();
        }

        private void RecomputeIncrement() => increment = frequencyHz / sampleRate;

        private float NextRandom()
        {
            // Numerical Recipes LCG; take the top bits for a value in [-1, 1).
            rngState = rngState * 1664525u + 1013904223u;
            return (rngState >> 8) / 8388608f - 1f;
        }
    }
}
