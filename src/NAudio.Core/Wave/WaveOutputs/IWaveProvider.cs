
using System;

namespace NAudio.Wave;

/// <summary>
/// The <see cref="IWaveProvider" /> interface is the lowest-level primitive in NAudio that is an audio source -
/// either that comes from a file that contains the samples, or from a signal generator
/// generating audio on the fly, or even by wrapping another provider that manipulates its samples.
/// </summary>
/// <remarks>
/// Providers can be chained into a pipeline: audio is read from a file or generated, optionally processed,
/// and then passed to an output device for playback or written to a file.
/// </remarks>
/// <seealso cref="WaveStream" />
/// <seealso cref="ISampleProvider" />
public interface IWaveProvider
{
    /// <summary>
    /// Gets the wave format of this wave provider.
    /// </summary>
    /// <value>The wave format.</value>
    /// <remarks>
    /// The audio format describes the wave data that are provided by successive <see cref="Read" /> calls. <br />
    /// Without this, it is not possible to know how the audio samples are laid out. <br /> <br />
    /// For most NAudio implementations, this typically returns a PCM or an IEEE floating-point format.
    /// </remarks>
    WaveFormat WaveFormat { get; }

    /// <summary>
    /// Fills the specified buffer with wave data.
    /// </summary>
    /// <param name="buffer">The buffer to fill with audio data.</param>
    /// <returns>The number of bytes written. Return 0 to signal end of stream.</returns>
    int Read(Span<byte> buffer);
}
