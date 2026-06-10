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
        private float startPhase;
        private float increment;
        private float sampleHoldValue;
        private int sampleRate;
        private float frequencyHz = 1f;
        private float delaySeconds;
        private int delayCounter;

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
        /// The phase, as a fraction of a cycle in [0, 1), that the oscillator
        /// starts from. Setting it moves the current phase there immediately
        /// (like <see cref="Reset"/>, which also re-applies it); values outside
        /// [0, 1) wrap. The default of 0 keeps the historical behaviour (triangle
        /// and square start at +1, sine and sawtooth rising through their zero /
        /// minimum). Because the phase does not advance during
        /// <see cref="DelaySeconds"/>, the first sample after the delay expires
        /// is taken at this phase — a SoundFont LFO (SF2.04 §8.1.2 gens 21/23)
        /// "begins its upward ramp from zero" when its delay elapses, which for
        /// the triangle waveform is a start phase of 0.75.
        /// </summary>
        public float StartPhase
        {
            get => startPhase;
            set
            {
                startPhase = value - MathF.Floor(value); // wrap into [0, 1)
                phase = startPhase;
            }
        }

        /// <summary>
        /// An optional delay in seconds before the LFO starts oscillating. During
        /// the delay <see cref="Process"/> outputs 0 (the bipolar neutral value),
        /// matching the SoundFont/SFZ LFO delay behaviour. The delay is re-armed
        /// by <see cref="Reset"/>.
        /// </summary>
        public float DelaySeconds
        {
            get => delaySeconds;
            set
            {
                delaySeconds = value < 0f ? 0f : value;
                delayCounter = (int)(delaySeconds * sampleRate);
            }
        }

        /// <summary>
        /// Advances one sample and returns the next oscillator value in [-1, 1].
        /// </summary>
        public float Process()
        {
            if (delayCounter > 0)
            {
                delayCounter--;
                return 0f;
            }
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
        /// Resets the phase to <see cref="StartPhase"/> (and re-seeds the
        /// sample-and-hold generator).
        /// </summary>
        public void Reset()
        {
            phase = startPhase;
            rngState = 0x9E3779B9;
            sampleHoldValue = NextRandom();
            delayCounter = (int)(delaySeconds * sampleRate);
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
