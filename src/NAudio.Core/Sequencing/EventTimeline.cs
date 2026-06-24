using System;
using System.Collections.Generic;
using System.Threading;

namespace NAudio.Sequencing
{
    /// <summary>
    /// An ordered set of timed events with a consumer-defined payload. Events are sorted by tick;
    /// events with the same tick preserve insertion order. Internally the timeline is an immutable
    /// snapshot array published copy-on-write: mutators (<see cref="Add(long, T)"/>, <see cref="Clear"/>)
    /// serialise on an internal lock and atomically publish a new array, while queries read the
    /// current snapshot lock-free and (via <see cref="EventsInRangeSpan"/>) allocation-free. This
    /// trade-off favours the audio thread — timelines are queried every buffer but mutated rarely
    /// (user edits) — at the cost of an O(n) copy per mutation, and it means a UI-thread edit can
    /// never block or priority-invert the audio thread's per-buffer query.
    /// </summary>
    /// <typeparam name="T">Payload type. See <see cref="SequencerEvent{T}"/>.</typeparam>
    public sealed class EventTimeline<T>
    {
        private readonly object writeLock = new();
        private SequencerEvent<T>[] snapshot = Array.Empty<SequencerEvent<T>>();

        /// <summary>The number of events currently in the timeline.</summary>
        public int Count => Volatile.Read(ref snapshot).Length;

        /// <summary>Adds an event at the given tick.</summary>
        public void Add(long tick, T payload)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), "Tick must not be negative.");
            lock (writeLock)
            {
                var current = snapshot;
                // Linear search from the end — the common case is appending in tick order.
                int i = current.Length;
                while (i > 0 && current[i - 1].Tick > tick) i--;

                var next = new SequencerEvent<T>[current.Length + 1];
                Array.Copy(current, 0, next, 0, i);
                next[i] = new SequencerEvent<T>(tick, payload);
                Array.Copy(current, i, next, i + 1, current.Length - i);
                Volatile.Write(ref snapshot, next);
            }
        }

        /// <summary>Adds a pre-built event.</summary>
        public void Add(SequencerEvent<T> ev) => Add(ev.Tick, ev.Payload);

        /// <summary>Removes all events.</summary>
        public void Clear()
        {
            lock (writeLock) Volatile.Write(ref snapshot, Array.Empty<SequencerEvent<T>>());
        }

        /// <summary>
        /// Returns the events whose tick lies in the half-open range [<paramref name="startTickInclusive"/>,
        /// <paramref name="endTickExclusive"/>). Returns a snapshot so the caller can iterate while the
        /// timeline is mutated concurrently. Allocates a fresh array per call; per-buffer audio-thread
        /// callers should prefer the allocation-free <see cref="EventsInRangeSpan"/>.
        /// </summary>
        public IReadOnlyList<SequencerEvent<T>> EventsInRange(long startTickInclusive, long endTickExclusive)
        {
            var range = EventsInRangeSpan(startTickInclusive, endTickExclusive);
            return range.IsEmpty ? Array.Empty<SequencerEvent<T>>() : range.ToArray();
        }

        /// <summary>
        /// The allocation-free, lock-free variant of <see cref="EventsInRange"/>: returns a span over
        /// the timeline's current immutable snapshot covering the half-open tick range
        /// [<paramref name="startTickInclusive"/>, <paramref name="endTickExclusive"/>).
        /// </summary>
        /// <remarks>
        /// Safe to call from the audio thread while another thread mutates the timeline: a mutation
        /// publishes a new snapshot array, so the returned span stays valid and internally consistent
        /// (it keeps the snapshot it was created from) but does not observe later edits. Take a fresh
        /// span per buffer to pick up edits.
        /// </remarks>
        public ReadOnlySpan<SequencerEvent<T>> EventsInRangeSpan(long startTickInclusive, long endTickExclusive)
        {
            if (endTickExclusive <= startTickInclusive)
                return ReadOnlySpan<SequencerEvent<T>>.Empty;

            var events = Volatile.Read(ref snapshot);
            if (events.Length == 0) return ReadOnlySpan<SequencerEvent<T>>.Empty;

            int lo = LowerBound(events, startTickInclusive);
            int hi = LowerBound(events, endTickExclusive);
            if (hi <= lo) return ReadOnlySpan<SequencerEvent<T>>.Empty;

            return new ReadOnlySpan<SequencerEvent<T>>(events, lo, hi - lo);
        }

        /// <summary>The smallest tick at which an event lies, or null if empty.</summary>
        public long? FirstTick
        {
            get
            {
                var events = Volatile.Read(ref snapshot);
                return events.Length == 0 ? null : events[0].Tick;
            }
        }

        /// <summary>The largest tick at which an event lies, or null if empty.</summary>
        public long? LastTick
        {
            get
            {
                var events = Volatile.Read(ref snapshot);
                return events.Length == 0 ? null : events[^1].Tick;
            }
        }

        private static int LowerBound(SequencerEvent<T>[] events, long tick)
        {
            int lo = 0, hi = events.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >>> 1;
                if (events[mid].Tick < tick) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }
}
