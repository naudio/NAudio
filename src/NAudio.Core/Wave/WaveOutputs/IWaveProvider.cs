using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Generic interface for all WaveProviders.
    /// </summary>
    public interface IWaveProvider
    {
        /// <summary>
        /// Gets the WaveFormat of this WaveProvider.
        /// </summary>
        /// <value>The wave format.</value>
        WaveFormat WaveFormat { get; }

        /// <summary>
        /// Fill the specified buffer with wave data.
        /// </summary>
        /// <param name="buffer">The buffer to fill with audio data.</param>
        /// <returns>The number of bytes written. Return 0 to signal end of stream.</returns>
        int Read(Span<byte> buffer);
    }
}
