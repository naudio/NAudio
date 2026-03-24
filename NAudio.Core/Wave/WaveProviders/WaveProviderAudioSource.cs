using System;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Adapts an existing <see cref="IWaveProvider"/> to the <see cref="IAudioSource"/> interface
    /// using a bridge buffer between the byte[] and Span APIs.
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
            bridgeBuffer = BufferHelpers.Ensure(bridgeBuffer, buffer.Length);
            int bytesRead = provider.Read(bridgeBuffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                bridgeBuffer.AsSpan(0, bytesRead).CopyTo(buffer);
            }
            return bytesRead;
        }
    }
}
