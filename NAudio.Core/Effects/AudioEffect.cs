using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// Base class for effects. Provides click-free <see cref="Bypass"/> and dry/wet
    /// <see cref="Mix"/> for free: it captures the dry signal, calls
    /// <see cref="ProcessBlock"/> to produce the wet signal in place, then blends them
    /// with a smoothed mix coefficient so toggling bypass or moving the mix never
    /// clicks. Derived effects implement <see cref="OnConfigure"/> and
    /// <see cref="ProcessBlock"/>.
    /// </summary>
    /// <remarks>
    /// While fully bypassed the effect is still run (its output is discarded) so its
    /// internal state stays warm and un-bypassing does not click. The dry copy and the
    /// per-sample blend are skipped on the common fully-wet, settled path.
    /// </remarks>
    public abstract class AudioEffect : IAudioEffect
    {
        private readonly ParameterSmoother mixSmoother = new ParameterSmoother();
        private WaveFormat waveFormat;
        private float[] dryBuffer = Array.Empty<float>();
        private float mix = 1f;
        private bool bypass;

        /// <summary>
        /// Dry/wet mix: 0 = fully dry (effect inaudible), 1 = fully wet. Changes are
        /// ramped, so moving this while audio flows does not click. Note: for effects that
        /// report a non-zero <see cref="LatencySamples"/> (e.g. pitch shift, convolution),
        /// the captured dry signal is not delay-compensated, so a partial mix blends the
        /// dry against the latency-delayed wet signal.
        /// </summary>
        public float Mix
        {
            get => mix;
            set => mix = value < 0f ? 0f : value > 1f ? 1f : value;
        }

        /// <summary>
        /// When true the effect is bypassed (output equals input). The transition is
        /// ramped, so toggling this while audio flows does not click.
        /// </summary>
        public bool Bypass
        {
            get => bypass;
            set => bypass = value;
        }

        /// <summary>
        /// The format the effect was configured with, or null before
        /// <see cref="Configure"/> has been called.
        /// </summary>
        protected WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Channel count from the configured format.
        /// </summary>
        protected int Channels => waveFormat.Channels;

        /// <summary>
        /// Sample rate from the configured format.
        /// </summary>
        protected int SampleRate => waveFormat.SampleRate;

        /// <inheritdoc />
        public virtual int LatencySamples => 0;

        /// <inheritdoc />
        public void Configure(WaveFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);
            waveFormat = format;
            mixSmoother.Configure(format.SampleRate);
            mixSmoother.Reset(TargetMix);
            OnConfigure(format);
        }

        /// <inheritdoc />
        public void Process(Span<float> buffer)
        {
            if (waveFormat == null)
                throw new InvalidOperationException("Configure must be called before Process.");
            if (buffer.IsEmpty)
                return;

            mixSmoother.SetTarget(TargetMix);

            // Fully wet and settled: no dry copy or blend needed (the common case).
            if (mixSmoother.IsSettled && mixSmoother.Current >= 1f)
            {
                ProcessBlock(buffer);
                return;
            }

            if (dryBuffer.Length < buffer.Length)
                dryBuffer = new float[buffer.Length];
            var dry = dryBuffer.AsSpan(0, buffer.Length);
            buffer.CopyTo(dry);

            ProcessBlock(buffer);

            // Fully dry and settled: keep the effect warm but discard its output.
            if (mixSmoother.IsSettled && mixSmoother.Current <= 0f)
            {
                dry.CopyTo(buffer);
                return;
            }

            // Advance the mix coefficient once per frame so the ramp time is independent
            // of channel count, then apply it to every channel of that frame.
            var channels = Channels;
            for (var i = 0; i + channels <= buffer.Length; i += channels)
            {
                var m = mixSmoother.Process();
                for (var ch = 0; ch < channels; ch++)
                    buffer[i + ch] = dry[i + ch] + (buffer[i + ch] - dry[i + ch]) * m;
            }
        }

        /// <inheritdoc />
        public virtual void Reset()
        {
            mixSmoother.Reset(TargetMix);
        }

        /// <summary>
        /// Called by <see cref="Configure"/> after the format is stored. Derived effects
        /// allocate and size their sample-rate-dependent state here.
        /// </summary>
        protected abstract void OnConfigure(WaveFormat format);

        /// <summary>
        /// Transforms <paramref name="buffer"/> in place into the fully-wet signal. The
        /// base class handles dry/wet mixing and bypass around this call.
        /// </summary>
        protected abstract void ProcessBlock(Span<float> buffer);

        private float TargetMix => bypass ? 0f : mix;
    }
}
