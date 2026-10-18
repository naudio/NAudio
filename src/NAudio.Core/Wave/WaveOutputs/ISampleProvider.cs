
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
    /// <remarks>
    /// <para>
    /// A well-behaved provider fills the whole buffer on every call until the end of the stream.
    /// It then returns at most one short read (fewer samples than <c>buffer.Length</c>), and 0 on every
    /// call after that. The count returned is always a whole number of sample frames, that is a
    /// multiple of <see cref="WaveFormat.Channels"/> samples.
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
    int Read(Span<float> buffer);
}
