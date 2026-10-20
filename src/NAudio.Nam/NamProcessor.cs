using System;
using NAudio.Nam.Internal;

namespace NAudio.Nam;

/// <summary>
/// Runs one NAM network over mono audio. This is the low-level engine behind
/// <see cref="NamEffect"/>; use it directly when you manage buffers yourself, for
/// example in an ASIO callback or an offline render.
/// </summary>
/// <remarks>
/// <para>
/// The network is causal and adds no latency: each output sample depends only on
/// the current and earlier input samples. Processing is allocation-free, so it is
/// safe to call on a real-time audio thread. A processor is not thread-safe; use
/// one per audio stream.
/// </para>
/// <para>
/// A new processor starts in the state the network settles to after silence, so the
/// first samples are clean. Feed it audio at the submodel's
/// <see cref="NamSubmodel.SampleRate"/>; NAM models are normally trained at 48 kHz.
/// Unlike <see cref="NamEffect"/>, a processor does not resample.
/// </para>
/// </remarks>
public sealed class NamProcessor
{
    private readonly NamDsp dsp;

    /// <summary>Creates a processor for a submodel of a loaded <see cref="NamModel"/>.</summary>
    public NamProcessor(NamSubmodel submodel)
    {
        ArgumentNullException.ThrowIfNull(submodel);
        Submodel = submodel;
        dsp = submodel.CreateDsp();
    }

    /// <summary>The network this processor runs.</summary>
    public NamSubmodel Submodel { get; }

    /// <summary>
    /// Processes mono samples. <paramref name="input"/> and <paramref name="output"/>
    /// must be the same length; they may be the same buffer to process in place. Any
    /// length is accepted.
    /// </summary>
    public void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (output.Length != input.Length)
            throw new ArgumentException("Output must be the same length as input.", nameof(output));
        for (var offset = 0; offset < input.Length; offset += NamDsp.MaxBlockFrames)
        {
            var n = Math.Min(NamDsp.MaxBlockFrames, input.Length - offset);
            dsp.Process(input.Slice(offset, n), output.Slice(offset, n));
        }
    }

    /// <summary>
    /// Clears the network's history, returning it to the state it settles to after
    /// silence. Call this when playback jumps (e.g. a seek) to avoid the previous
    /// audio bleeding into the new position. Allocation-free.
    /// </summary>
    public void Reset() => dsp.Reset();
}
