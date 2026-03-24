using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Adapts an <see cref="IAudioSource"/> to the <see cref="IWaveProvider"/> interface,
    /// allowing span-based audio sources to be used with legacy byte[]-based consumers.
    /// </summary>
    public class AudioSourceWaveProvider : IWaveProvider
    {
        private readonly IAudioSource source;

        /// <summary>
        /// Creates a new AudioSourceWaveProvider wrapping an existing IAudioSource.
        /// </summary>
        public AudioSourceWaveProvider(IAudioSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <inheritdoc/>
        public int Read(byte[] buffer, int offset, int count)
        {
            return source.Read(buffer.AsSpan(offset, count));
        }
    }
}
