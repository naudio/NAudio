using System.Threading;

namespace NAudio.Extras;

/// <summary>
/// A shared time origin for a set of <see cref="CaptureMixerInput"/> instances. The first
/// timestamped packet seen across any input fixes the origin; every input then aligns its
/// audio relative to that same origin so independently-clocked capture devices line up on a
/// common timeline. Pass one instance to every input you want mutually aligned.
/// </summary>
/// <remarks>
/// Timestamps are QPC (QueryPerformanceCounter) values in 100-nanosecond units, exactly as
/// delivered by <c>WasapiRecorder.DataAvailable</c>. QPC is a system-wide clock, so the same
/// value is comparable across devices.
/// </remarks>
public sealed class CaptureTimeline
{
    private long origin;
    private int hasOrigin;

    /// <summary>True once an origin has been established.</summary>
    public bool HasOrigin => Volatile.Read(ref hasOrigin) != 0;

    /// <summary>The shared origin in QPC 100-nanosecond units (0 until established).</summary>
    public long Origin => Volatile.Read(ref origin);

    /// <summary>
    /// Returns the shared origin, atomically setting it to <paramref name="qpcPosition"/> the
    /// first time it is called. Safe to call from multiple capture threads concurrently.
    /// </summary>
    public long GetOrSetOrigin(long qpcPosition)
    {
        if (Interlocked.CompareExchange(ref hasOrigin, 1, 0) == 0)
        {
            Volatile.Write(ref origin, qpcPosition);
            return qpcPosition;
        }
        return Volatile.Read(ref origin);
    }
}
