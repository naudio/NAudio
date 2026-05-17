using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Single-channel-independent spectral noise suppressor (STFT Wiener / spectral
    /// subtraction). A square-root-Hann weighted overlap-add analysis estimates the
    /// noise spectrum during non-speech frames (gated by a shared
    /// <see cref="VoiceActivityDetector"/>) and applies a smoothed per-bin attenuation
    /// to reduce stationary background noise. This is the build-from-scratch "tier-a"
    /// suppressor; an ML tier (RNNoise) remains a future evaluation. Reports its frame
    /// size as latency.
    /// </summary>
    public sealed class NoiseSuppressionEffect : AudioEffect, IParameterized
    {
        private IReadOnlyList<EffectParameter> parameters;

        /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
        public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
        {
            EffectParameter.Continuous("Aggressiveness", "", 0.5f, 4f, () => Aggressiveness, v => Aggressiveness = v),
            EffectParameter.Continuous("Spectral Floor", "", 0f, 0.5f, () => SpectralFloor, v => SpectralFloor = v),
            EffectParameter.Continuous("Noise Adapt", "", 0.001f, 0.5f, () => NoiseAdaptation, v => NoiseAdaptation = v)
        };

        private FftProcessor fft;
        private VoiceActivityDetector vad;
        private float[] window;        // sqrt-Hann, analysis = synthesis (product = Hann, COLA at 50%)
        private int frameSize = 512;
        private int hop;
        private int spectrumLength;
        private ChannelState[] channels = Array.Empty<ChannelState>();
        private int position;

        /// <summary>Over-subtraction factor; higher removes more noise (and risks artefacts). Default 1.5.</summary>
        public float Aggressiveness { get; set; } = 1.5f;

        /// <summary>Minimum per-bin gain (linear), the residual noise floor. Default 0.08.</summary>
        public float SpectralFloor { get; set; } = 0.08f;

        /// <summary>Noise-estimate adaptation rate per non-speech frame, 0–1. Default 0.05.</summary>
        public float NoiseAdaptation { get; set; } = 0.05f;

        /// <summary>STFT frame size; a power of two (256–2048). Default 512.</summary>
        public int FrameSize
        {
            get => frameSize;
            set
            {
                if (value < 64 || (value & (value - 1)) != 0)
                    throw new ArgumentException("Frame size must be a power of two ≥ 64.", nameof(value));
                frameSize = value;
                if (WaveFormat != null)
                    Build();
            }
        }

        /// <inheritdoc />
        public override int LatencySamples => channels.Length > 0 ? frameSize : 0;

        /// <inheritdoc />
        protected override void OnConfigure(WaveFormat format) => Build();

        /// <inheritdoc />
        protected override void ProcessBlock(Span<float> buffer)
        {
            var channelCount = Channels;
            for (var i = 0; i + channelCount <= buffer.Length; i += channelCount)
            {
                var mono = 0f;
                for (var ch = 0; ch < channelCount; ch++)
                {
                    var state = channels[ch];
                    var outSample = state.Output[position];
                    state.Fill[position] = buffer[i + ch];
                    mono += buffer[i + ch];
                    buffer[i + ch] = outSample;
                }

                var voiced = vad.Process(mono);

                if (++position == hop)
                {
                    // Learn the noise spectrum only during non-speech frames.
                    for (var ch = 0; ch < channelCount; ch++)
                        ProcessHop(channels[ch], !voiced);
                    position = 0;
                }
            }
        }

        /// <inheritdoc />
        public override void Reset()
        {
            base.Reset();
            vad?.Reset();
            position = 0;
            foreach (var state in channels)
                state.Clear();
        }

        private void ProcessHop(ChannelState state, bool updateNoise)
        {
            // Slide the analysis frame: drop the oldest hop, append the new hop.
            Array.Copy(state.Frame, hop, state.Frame, 0, frameSize - hop);
            Array.Copy(state.Fill, 0, state.Frame, frameSize - hop, hop);

            for (var n = 0; n < frameSize; n++)
                state.Windowed[n] = state.Frame[n] * window[n];

            fft.RealForward(state.Windowed, state.Spectrum);

            for (var b = 0; b < spectrumLength; b++)
            {
                var re = state.Spectrum[b].X;
                var im = state.Spectrum[b].Y;
                var power = re * re + im * im;

                if (updateNoise)
                    state.NoisePower[b] += NoiseAdaptation * (power - state.NoisePower[b]);

                var clean = power - Aggressiveness * state.NoisePower[b];
                var gain = clean <= 0f ? 0f : MathF.Sqrt(clean / (power + 1e-12f));
                if (gain < SpectralFloor) gain = SpectralFloor;
                else if (gain > 1f) gain = 1f;

                // Smooth the gain across frames to suppress musical noise.
                state.GainSmooth[b] = 0.5f * state.GainSmooth[b] + 0.5f * gain;
                state.Spectrum[b].X = re * state.GainSmooth[b];
                state.Spectrum[b].Y = im * state.GainSmooth[b];
            }

            fft.RealInverse(state.Spectrum, state.TimeBlock);

            // Synthesis window + overlap-add; emit the first hop, slide the OLA buffer.
            for (var n = 0; n < frameSize; n++)
                state.Ola[n] += state.TimeBlock[n] * window[n];

            Array.Copy(state.Ola, 0, state.Output, 0, hop);
            Array.Copy(state.Ola, hop, state.Ola, 0, frameSize - hop);
            Array.Clear(state.Ola, frameSize - hop, hop);
        }

        private void Build()
        {
            hop = frameSize / 2;
            fft = new FftProcessor(frameSize);
            spectrumLength = fft.SpectrumLength;
            vad = new VoiceActivityDetector(SampleRate);

            window = new float[frameSize];
            for (var n = 0; n < frameSize; n++)
            {
                var hann = 0.5f - 0.5f * MathF.Cos(2f * MathF.PI * n / frameSize);
                window[n] = MathF.Sqrt(hann); // sqrt-Hann: analysis·synthesis = Hann (exact COLA at 50%)
            }

            channels = new ChannelState[Channels];
            for (var ch = 0; ch < Channels; ch++)
            {
                channels[ch] = new ChannelState(frameSize, hop, spectrumLength);
            }
            position = 0;
        }

        private sealed class ChannelState
        {
            public readonly float[] Fill;
            public readonly float[] Frame;
            public readonly float[] Windowed;
            public readonly float[] TimeBlock;
            public readonly float[] Ola;
            public readonly float[] Output;
            public readonly Complex[] Spectrum;
            public readonly float[] NoisePower;
            public readonly float[] GainSmooth;

            public ChannelState(int frameSize, int hop, int spectrumLength)
            {
                Fill = new float[hop];
                Frame = new float[frameSize];
                Windowed = new float[frameSize];
                TimeBlock = new float[frameSize];
                Ola = new float[frameSize];
                Output = new float[hop];
                Spectrum = new Complex[spectrumLength];
                NoisePower = new float[spectrumLength];
                GainSmooth = new float[spectrumLength];
            }

            public void Clear()
            {
                Array.Clear(Fill);
                Array.Clear(Frame);
                Array.Clear(Windowed);
                Array.Clear(TimeBlock);
                Array.Clear(Ola);
                Array.Clear(Output);
                Array.Clear(NoisePower);
                Array.Clear(GainSmooth);
            }
        }
    }
}
