using System;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// A processing kernel that transforms interleaved 32-bit float audio in place.
/// An effect is configured with a <see cref="Wave.WaveFormat"/> (sample rate and
/// channel count) and then processes blocks — pulled from a source through an
/// <see cref="EffectSampleProvider"/>/<see cref="EffectChain"/>, or pushed directly
/// by a synth/sampler voice. Implementations are channel-agnostic and must not
/// allocate on the steady-state processing path.
/// </summary>
public interface IAudioEffect
{
    /// <summary>
    /// Supplies the sample rate and channel count the effect will process. Called
    /// before the first <see cref="Process"/>, and again if the format changes.
    /// </summary>
    void Configure(WaveFormat format);

    /// <summary>
    /// Processes one block of interleaved samples in place.
    /// </summary>
    /// <param name="buffer">Interleaved samples to transform in place.</param>
    void Process(Span<float> buffer);

    /// <summary>
    /// Clears internal state (delay lines, envelopes, smoothers) so the next block
    /// starts as if from silence.
    /// </summary>
    void Reset();

    /// <summary>
    /// Processing latency the effect introduces, in samples per channel. Zero for
    /// effects with no inherent delay; non-zero for look-ahead or
    /// block/partitioned designs that need delay compensation in a chain.
    /// </summary>
    int LatencySamples { get; }
}
