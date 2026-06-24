using System;

namespace NAudio.Sequencing;

/// <summary>
/// A half-open musical loop region: playback wraps from <see cref="EndTick"/> back to <see cref="StartTick"/>.
/// </summary>
public readonly record struct LoopRegion
{
    /// <summary>The (inclusive) start of the loop in canonical ticks.</summary>
    public long StartTick { get; }

    /// <summary>The (exclusive) end of the loop in canonical ticks.</summary>
    public long EndTick { get; }

    /// <summary>The length of the loop in canonical ticks.</summary>
    public long LengthTicks => EndTick - StartTick;

    /// <summary>Creates a loop region. <paramref name="endTick"/> must be greater than <paramref name="startTick"/>.</summary>
    public LoopRegion(long startTick, long endTick)
    {
        if (endTick <= startTick)
            throw new ArgumentException("Loop end must be greater than loop start.", nameof(endTick));
        if (startTick < 0)
            throw new ArgumentException("Loop start must not be negative.", nameof(startTick));
        StartTick = startTick;
        EndTick = endTick;
    }
}
