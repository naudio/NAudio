using System;
using NAudio.Wave;

namespace NAudio.Effects
{
    /// <summary>
    /// An auxiliary effect send/return bus: a shared interleaved buffer that
    /// sources mix a portion of their signal into, run through one
    /// <see cref="IAudioEffect"/> (typically a reverb or chorus), with the wet
    /// result returned (added) into a destination mix.
    ///
    /// This is the generic plumbing a mixer or sampler uses to share one reverb
    /// or chorus instance across many voices, rather than instantiating the
    /// effect per voice. The effect runs every block so its tail keeps sounding
    /// after the sends stop. Not thread-safe; drive it from the audio thread.
    /// </summary>
    public sealed class SendBus
    {
        private readonly IAudioEffect effect;
        private float[] buffer;
        private int channels;

        /// <summary>
        /// Creates a send bus around an effect. The effect should be fully wet
        /// (return only its processed signal); <see cref="AudioEffect.Mix"/>
        /// defaults to 1, which is correct here.
        /// </summary>
        public SendBus(IAudioEffect effect)
        {
            this.effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        /// <summary>The hosted effect, exposed so callers can tweak or bypass it.</summary>
        public IAudioEffect Effect => effect;

        /// <summary>
        /// Configures the bus for a format and a maximum block size (frames per
        /// <see cref="ProcessReturn"/> call). Allocates the send buffer.
        /// </summary>
        public void Configure(WaveFormat format, int maxFrames)
        {
            if (format == null) throw new ArgumentNullException(nameof(format));
            if (maxFrames < 1) throw new ArgumentOutOfRangeException(nameof(maxFrames));
            channels = format.Channels;
            buffer = new float[maxFrames * channels];
            effect.Configure(format);
        }

        /// <summary>
        /// The send buffer for the next <paramref name="frames"/> frames, cleared
        /// and ready for sources to accumulate into. Valid until the next
        /// <see cref="ProcessReturn"/>.
        /// </summary>
        public Span<float> PrepareSend(int frames)
        {
            var span = buffer.AsSpan(0, frames * channels);
            span.Clear();
            return span;
        }

        /// <summary>
        /// Runs the effect over the accumulated send signal and adds the wet
        /// result into <paramref name="destination"/> (the dry mix). Call once per
        /// block after sources have written their sends via <see cref="PrepareSend"/>.
        /// </summary>
        public void ProcessReturn(Span<float> destination, int frames)
        {
            var span = buffer.AsSpan(0, frames * channels);
            effect.Process(span);
            for (int i = 0; i < span.Length; i++) destination[i] += span[i];
        }

        /// <summary>Clears the hosted effect's internal state (delay lines, etc.).</summary>
        public void Reset() => effect.Reset();
    }
}
