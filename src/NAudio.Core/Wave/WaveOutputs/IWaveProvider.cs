
using System;

namespace NAudio.Wave;

/// <summary>
/// The <see cref="IWaveProvider" /> interface is the lowest-level primitive in NAudio that is an audio source -
/// either that comes from a file that contains the samples, or from a signal generator
/// generating audio on the fly, or even by wrapping another provider that manipulates it's samples. <br />
/// Instances of this interface can form an audio pipeline that gets samples from a file (or generating them),
/// optionally modifying them in a way, and then are fed to a native player API that can be used to listen to the
/// processed result, or even storing them to a file for long-term access.
/// </summary>
/// <seealso cref="WaveStream" />
/// <seealso cref="ISampleProvider" />
public interface IWaveProvider
{
    /// <summary>
    /// Gets the wave format of this wave provider.
    /// </summary>
    /// <returns>The wave format.</returns>
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
