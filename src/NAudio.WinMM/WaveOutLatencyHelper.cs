using System;

namespace NAudio.Wave;

/// <summary>
/// Shared implementation of <see cref="IWaveLatency.CurrentLatency"/> for the winmm
/// buffer-array-driven players (<see cref="WaveOut"/> and <c>WaveOutWindow</c>).
/// Extracted so the timestamp-selection logic can be unit-tested independently of the
/// driver-owned <see cref="WaveOutBuffer"/> (whose constructor requires a live wave-out
/// handle, and whose <c>InQueue</c> flag is set from unmanaged code).
/// </summary>
internal static class WaveOutLatencyHelper
{
    /// <summary>
    /// Returns the elapsed time since the oldest buffer in <paramref name="queuedFilledTimestamps"/>
    /// was filled — i.e. the wall-clock age of the audio at the play head. Timestamps of
    /// <see cref="long.MinValue"/> are treated as "never written" and ignored. Returns
    /// <paramref name="averageLatency"/> when the array is empty or every entry is
    /// <see cref="long.MinValue"/>.
    /// </summary>
    /// <param name="queuedFilledTimestamps">
    /// Stopwatch ticks captured when each currently-in-queue buffer was last filled. Callers
    /// should include only buffers whose <c>InQueue</c> flag is set.
    /// </param>
    /// <param name="averageLatency">The fallback value when no live timestamp is available.</param>
    /// <param name="nowTicks">The current <see cref="System.Diagnostics.Stopwatch"/> timestamp.</param>
    /// <param name="stopwatchFrequency">The <see cref="System.Diagnostics.Stopwatch.Frequency"/> value.</param>
    public static TimeSpan CurrentLatencyFromOldestFilledTimestamp(
        ReadOnlySpan<long> queuedFilledTimestamps,
        TimeSpan averageLatency,
        long nowTicks,
        long stopwatchFrequency)
    {
        long oldest = long.MaxValue;
        foreach (var ts in queuedFilledTimestamps)
        {
            if (ts != long.MinValue && ts < oldest) oldest = ts;
        }
        if (oldest == long.MaxValue) return averageLatency;
        return TimeSpan.FromSeconds((nowTicks - oldest) / (double)stopwatchFrequency);
    }
}
