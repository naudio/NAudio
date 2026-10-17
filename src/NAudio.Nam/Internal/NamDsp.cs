using System;

namespace NAudio.Nam.Internal;

/// <summary>
/// A mono neural network model that turns input samples into output samples.
/// Implementations must not allocate in <see cref="Process"/> or <see cref="Reset"/>.
/// </summary>
internal abstract class NamDsp
{
    /// <summary>Largest number of frames <see cref="Process"/> accepts in one call.</summary>
    public const int MaxBlockFrames = 128;

    /// <summary>
    /// Processes up to <see cref="MaxBlockFrames"/> samples. <paramref name="input"/> and
    /// <paramref name="output"/> have the same length and may be the same memory.
    /// </summary>
    public abstract void Process(ReadOnlySpan<float> input, Span<float> output);

    /// <summary>
    /// Puts the model into the state it settles to after a long stretch of silence
    /// (NAM calls this "prewarming"), so the next sample is processed as if the input
    /// had always been silent.
    /// </summary>
    public abstract void Reset();
}
