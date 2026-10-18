
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
    /// <remarks>
    /// <para>
    /// A well-behaved provider fills the whole buffer on every call until the end of the stream.
    /// It then returns at most one short read (fewer bytes than <c>buffer.Length</c>), and 0 on every
    /// call after that. The count returned is always a whole number of sample frames, that is a
    /// multiple of <see cref="WaveFormat.BlockAlign"/> bytes.
    /// </para>
    /// <para>
    /// A provider that wraps another one, such as an effect or a resampler, may return more on that last
    /// read than it received from its source, in order to flush a tail (for example a reverb decay or
    /// samples held in a filter). Once it has done so, it must return 0.
    /// </para>
    /// <para>
    /// Playback devices stop when <see cref="Read"/> returns 0. Consumers that keep reading after a
    /// short read until they get 0 are being defensive against badly behaved sources; that is best
    /// effort, not something a provider should rely on.
    /// </para>
    /// <para>
    /// A real-time source that cannot always keep up should be put behind a
    /// <see cref="BufferedWaveProvider"/> with <see cref="BufferedWaveProvider.ReadFully"/> set to
    /// <see langword="true"/>, which pads with silence rather than returning a short read. A source that
    /// can block, such as a network stream, belongs behind a <see cref="BufferedWaveProvider"/> that is
    /// filled from another thread, not on the audio thread.
    /// </para>
    /// </remarks>
    int Read(Span<byte> buffer);
}
