using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Generic interface for all WaveProviders.
    /// 
    /// NOT YET USED.
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
        /// <param name="buffer">The buffer to fill of wave data.</param>
        /// <returns>the number of bytes written to the buffer.</returns>
        int Read(IWaveBuffer buffer);
    }
}
