using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Span-based audio source interface for zero-copy audio rendering.
    /// This is the modern equivalent of <see cref="IWaveProvider"/> that avoids buffer copies
    /// by writing directly into the caller's buffer span.
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
}
