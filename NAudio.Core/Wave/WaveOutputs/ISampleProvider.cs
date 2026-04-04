using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Like IWaveProvider, but makes it much simpler to put together a 32 bit floating
    /// point mixing engine
    /// </summary>
    public interface ISampleProvider
    {
        /// <summary>
        /// Gets the WaveFormat of this Sample Provider.
        /// </summary>
        /// <value>The wave format.</value>
        WaveFormat WaveFormat { get; }

        /// <summary>
        /// Fill the specified buffer with 32 bit floating point samples
        /// </summary>
        /// <param name="buffer">The buffer to fill with samples.</param>
        /// <returns>The number of samples written. Return 0 to signal end of stream.</returns>
        int Read(Span<float> buffer);
    }
}
