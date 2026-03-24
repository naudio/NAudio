using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Span-based sample source interface for zero-copy audio processing with IEEE float samples.
    /// This is the modern equivalent of <see cref="ISampleProvider"/> that avoids buffer copies
    /// by reading directly into the caller's buffer span.
    /// </summary>
    public interface ISampleSource
    {
        /// <summary>
        /// Reads 32-bit IEEE float samples into the provided span.
        /// </summary>
        /// <param name="buffer">The buffer to fill with samples.</param>
        /// <returns>The number of samples written. Return 0 to signal end of stream.</returns>
        int Read(Span<float> buffer);

        /// <summary>
        /// The WaveFormat of the audio this source produces.
        /// Must be IEEE float (32-bit).
        /// </summary>
        WaveFormat WaveFormat { get; }
    }
}
