
using System;

namespace NAudio.Wave;

/// <summary>
/// An audio source that provides 32-bit floating-point samples, whatever the format of the underlying audio.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IWaveProvider"/> supplies raw bytes in the source's own format (for example 8, 16, 24 or 32-bit PCM,
/// or floating point), so code that processes samples must handle each format separately.
/// <see cref="ISampleProvider"/> always supplies interleaved 32-bit floating-point samples, nominally in the
/// range -1.0 to 1.0, so effects, mixers and meters only need to be written once. Floating point also
/// gives headroom: intermediate values can exceed full scale without clipping.
/// </para>
/// <para>
/// Use <see cref="WaveExtensionMethods.ToSampleProvider"/> and <see cref="WaveExtensionMethods.ToWaveProvider"/>
/// to convert between the two.
/// </para>
/// </remarks>
public interface ISampleProvider
{
    /// <summary>
    /// Gets the wave format of this sample provider.
    /// </summary>
    /// <value>The wave format.</value>
    /// <remarks>
    /// The audio format describes the wave data that are provided by successive <see cref="Read"/> calls. <br />
    /// This must always be an IEEE 32-bit floating-point format, which can be created with the
    /// <see cref="WaveFormat.CreateIeeeFloatWaveFormat(int, int)"/> helper method.
    /// </remarks>
    WaveFormat WaveFormat { get; }

    /// <summary>
    /// Fills the specified buffer with 32 bit floating point samples
    /// </summary>
    /// <param name="buffer">The buffer to fill with samples.</param>
    /// <returns>The number of samples written. Return 0 to signal end of stream.</returns>
    int Read(Span<float> buffer);
}
