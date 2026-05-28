using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Sequencing
{
    /// <summary>
    /// An immutable piecewise-constant tempo curve, intended for content with a fully-known tempo
    /// map (a MIDI file, an edited DAW project). Lookups are O(log n) over the segment list.
    /// </summary>
    /// <remarks>
    /// Continuous tempo ramps (accelerando / ritardando) are not yet supported. The segment record
    /// reserves a <see cref="TempoChangeKind"/> field so they can be added later without breaking the API.
    /// Real-world MIDI files almost always represent ramps as many small stepped changes anyway.
    /// </remarks>
    public sealed class StaticTempoMap : ITempoMap
    {
        private readonly Segment[] segments;

        /// <summary>
        /// Creates a tempo map from an ordered set of (tick, BPM) entries. The first entry must
        /// start at tick 0. Tick values must be strictly increasing.
        /// </summary>
        public StaticTempoMap(IEnumerable<(long StartTick, double Bpm)> entries)
        {
            if (entries is null) throw new ArgumentNullException(nameof(entries));
            var list = entries.ToList();
            if (list.Count == 0) throw new ArgumentException("At least one entry is required.", nameof(entries));
            if (list[0].StartTick != 0) throw new ArgumentException("First entry must start at tick 0.", nameof(entries));

            var built = new Segment[list.Count];
            built[0] = Segment.FromBpm(0, 0.0, list[0].Bpm);
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].StartTick <= list[i - 1].StartTick)
                    throw new ArgumentException("Tempo entries must be strictly increasing in tick.", nameof(entries));
                var prev = built[i - 1];
                var elapsedSecs = (list[i].StartTick - prev.StartTick) * prev.SecondsPerTick;
                built[i] = Segment.FromBpm(list[i].StartTick, prev.StartSeconds + elapsedSecs, list[i].Bpm);
            }
            segments = built;
        }

        /// <summary>Convenience overload for a single-tempo map.</summary>
        public StaticTempoMap(double bpm) : this(new[] { (0L, bpm) })
        {
        }

        /// <inheritdoc/>
        public double SecondsFromTicks(long ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks must not be negative.");
            var seg = FindByTicks(ticks);
            return seg.StartSeconds + (ticks - seg.StartTick) * seg.SecondsPerTick;
        }

        /// <inheritdoc/>
        public long TicksFromSeconds(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must not be negative.");
            var seg = FindBySeconds(seconds);
            return seg.StartTick + (long)((seconds - seg.StartSeconds) / seg.SecondsPerTick);
        }

        /// <inheritdoc/>
        public double BpmAtTicks(long ticks) => FindByTicks(ticks).Bpm;

        private Segment FindByTicks(long ticks)
        {
            int lo = 0, hi = segments.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >>> 1;
                if (segments[mid].StartTick <= ticks) lo = mid;
                else hi = mid - 1;
            }
            return segments[lo];
        }

        private Segment FindBySeconds(double seconds)
        {
            int lo = 0, hi = segments.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >>> 1;
                if (segments[mid].StartSeconds <= seconds) lo = mid;
                else hi = mid - 1;
            }
            return segments[lo];
        }

        /// <summary>The kind of tempo change at a segment boundary. Only <see cref="Step"/> is implemented today.</summary>
        public enum TempoChangeKind
        {
            /// <summary>An instant tempo change at the segment boundary.</summary>
            Step = 0,
            /// <summary>A linear BPM ramp from this segment's tempo to the next segment's tempo. Not yet implemented.</summary>
            LinearBpm = 1,
        }

        private readonly struct Segment
        {
            public Segment(long startTick, double startSeconds, double secondsPerTick, double bpm)
            {
                StartTick = startTick;
                StartSeconds = startSeconds;
                SecondsPerTick = secondsPerTick;
                Bpm = bpm;
            }

            public long StartTick { get; }
            public double StartSeconds { get; }
            public double SecondsPerTick { get; }
            public double Bpm { get; }

            public static Segment FromBpm(long startTick, double startSeconds, double bpm)
            {
                if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be positive.");
                var secondsPerTick = (60.0 / bpm) / MusicalTime.CanonicalPpq;
                return new Segment(startTick, startSeconds, secondsPerTick, bpm);
            }
        }
    }
}
