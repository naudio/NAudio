using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Pitch shifter (no tempo change) wrapping the Bernsee STFT phase-vocoder
    /// (<see cref="SmbPitchShifter"/>) in the effect framework, one shifter per channel.
    /// This is the dependable default; a higher-quality Signalsmith-based tier remains a
    /// separate future evaluation. Introduces FFT-frame latency
    /// (<see cref="AudioEffect.LatencySamples"/>).
    /// </summary>
    public sealed class PitchShiftEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Pitch", "st", -12f, 12f, () => PitchSemitones, v => PitchSemitones = v)
        };

        private SmbPitchShifter[] shifters = Array.Empty<SmbPitchShifter>();
        private float[][] scratch = Array.Empty<float[]>();
        private float semitones;
        private int fftSize = 4096;
        private long oversampling = 4;

        /// <summary>
        /// Pitch shift in semitones (±, 0 = no change). Clamped to roughly ±1 octave,
        /// the algorithm's usable range.
        /// </summary>
        public float PitchSemitones
        {
            get => semitones;
            set => semitones = Math.Clamp(value, -12f, 12f);
        }

        /// <summary>FFT size; a power of two ≤ 4096. Default 4096.</summary>
        public int FftSize
        {
            get => fftSize;
            set
            {
                if (value < 64 || value > 4096 || (value & (value - 1)) != 0)
                    throw new ArgumentException("FFT size must be a power of two in 64–4096.", nameof(value));
                fftSize = value;
            }
        }

        /// <summary>STFT oversampling (overlapping windows). Higher is smoother/heavier. Default 4.</summary>
        public long Oversampling
        {
            get => oversampling;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Oversampling must be at least 1");
                oversampling = value;
            }
        }

        /// <inheritdoc />
        public override int LatencySamples => shifters.Length > 0 ? fftSize : 0;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format)
        {
            shifters = new SmbPitchShifter[format.Channels];
            scratch = new float[format.Channels][];
            for (var ch = 0; ch < format.Channels; ch++)
            {
                shifters[ch] = new SmbPitchShifter();
                scratch[ch] = Array.Empty<float>();
            }
        }

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channels = Channels;
            var frames = buffer.Length / channels;
            if (frames == 0)
                return;
            var pitch = MathF.Pow(2f, semitones / 12f);
            var sampleRate = SampleRate;

            if (channels == 1)
            {
                shifters[0].PitchShift(pitch, frames, fftSize, oversampling, sampleRate, buffer);
                return;
            }

            for (var ch = 0; ch < channels; ch++)
            {
                if (scratch[ch].Length < frames)
                    scratch[ch] = new float[frames];
                var mono = scratch[ch];
                for (var f = 0; f < frames; f++)
                    mono[f] = buffer[f * channels + ch];

                shifters[ch].PitchShift(pitch, frames, fftSize, oversampling, sampleRate, mono.AsSpan(0, frames));

                for (var f = 0; f < frames; f++)
                    buffer[f * channels + ch] = mono[f];
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            for (var ch = 0; ch < shifters.Length; ch++)
                shifters[ch] = new SmbPitchShifter();
        }
    }
}
