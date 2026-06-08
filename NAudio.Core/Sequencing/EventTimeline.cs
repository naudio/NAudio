using System;
using System.Collections.Generic;

namespace NAudio.Sequencing
{
    /// <summary>
    /// An ordered set of timed events with a consumer-defined payload. Events are sorted by tick;
    /// events with the same tick preserve insertion order. Mutators and readers serialise on an
    /// internal lock — matching the policy used by <see cref="NAudio.Wave.SampleProviders.MixingSampleProvider"/>.
    /// </summary>
    /// <typeparam name="T">Payload type. See <see cref="SequencerEvent{T}"/>.</typeparam>
    public sealed class EventTimeline<T>
    {
        private readonly object syncRoot = new();
        private readonly List<SequencerEvent<T>> events = new();

        /// <summary>The number of events currently in the timeline.</summary>
        public int Count
        {
            get { lock (syncRoot) return events.Count; }
        }

        /// <summary>Adds an event at the given tick.</summary>
        public void Add(long tick, T payload)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), "Tick must not be negative.");
            lock (syncRoot)
            {
                // Linear search from the end — the common case is appending in tick order.
                int i = events.Count;
                while (i > 0 && events[i - 1].Tick > tick) i--;
                events.Insert(i, new SequencerEvent<T>(tick, payload));
            }
        }

        /// <summary>Adds a pre-built event.</summary>
        public void Add(SequencerEvent<T> ev) => Add(ev.Tick, ev.Payload);

        /// <summary>Removes all events.</summary>
        public void Clear()
        {
            lock (syncRoot) events.Clear();
        }

        /// <summary>
        /// Returns the events whose tick lies in the half-open range [<paramref name="startTickInclusive"/>,
        /// <paramref name="endTickExclusive"/>). Returns a snapshot so the caller can iterate without
        /// holding the lock.
        /// </summary>
        public IReadOnlyList<SequencerEvent<T>> EventsInRange(long startTickInclusive, long endTickExclusive)
        {
            if (endTickExclusive <= startTickInclusive)
                return Array.Empty<SequencerEvent<T>>();

            lock (syncRoot)
            {
                if (events.Count == 0) return Array.Empty<SequencerEvent<T>>();

                int lo = LowerBound(startTickInclusive);
                int hi = LowerBound(endTickExclusive);
                if (hi <= lo) return Array.Empty<SequencerEvent<T>>();

                var result = new SequencerEvent<T>[hi - lo];
                events.CopyTo(lo, result, 0, hi - lo);
                return result;
            }
        }

        /// <summary>The smallest tick at which an event lies, or null if empty.</summary>
        public long? FirstTick
        {
            get
            {
                lock (syncRoot) return events.Count == 0 ? null : events[0].Tick;
            }
        }

        /// <summary>The largest tick at which an event lies, or null if empty.</summary>
        public long? LastTick
        {
            get
            {
                lock (syncRoot) return events.Count == 0 ? null : events[^1].Tick;
            }
        }

        private int LowerBound(long tick)
        {
            int lo = 0, hi = events.Count;
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
