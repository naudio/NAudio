using System;
using NAudio.Wave;

namespace NAudio.Wasapi
{
    /// <summary>
    /// Span-based audio source interface for zero-copy audio rendering.
    /// This is the modern equivalent of IWaveProvider that avoids buffer copies
    /// by writing directly into the WASAPI render buffer.
    /// </summary>
    public interface IAudioSource
    {
        /// <summary>
        /// Reads audio data into the provided span.
        /// </summary>
        /// <param name="buffer">The buffer to fill with audio data.</param>
        /// <returns>The number of bytes written. Return 0 to signal end of stream.</returns>
        int Read(Span<byte> buffer);

        /// <summary>
        /// The WaveFormat of the audio this source produces.
        /// </summary>
        WaveFormat WaveFormat { get; }
    }

    /// <summary>
    /// Adapts an existing <see cref="IWaveProvider"/> to the <see cref="IAudioSource"/> interface
    /// using a pooled buffer to bridge between the byte[] and Span APIs.
    /// </summary>
    public class WaveProviderAudioSource : IAudioSource
    {
        private readonly IWaveProvider provider;
        private byte[] bridgeBuffer;

        /// <summary>
        /// Creates a new WaveProviderAudioSource wrapping an existing IWaveProvider.
        /// </summary>
        public WaveProviderAudioSource(IWaveProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => provider.WaveFormat;

        /// <inheritdoc/>
        public int Read(Span<byte> buffer)
        {
            // Rent from pool or reuse if already large enough
            if (bridgeBuffer == null || bridgeBuffer.Length < buffer.Length)
            {
                bridgeBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
            }

            int bytesRead = provider.Read(bridgeBuffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                bridgeBuffer.AsSpan(0, bytesRead).CopyTo(buffer);
            }
            return bytesRead;
        }
    }
}
