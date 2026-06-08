using System;

namespace NAudio.Sequencing
{
    /// <summary>
    /// A tempo map with a single mutable "current" tempo, intended for live consumers (the drum-machine
    /// tempo knob, a real-time-input keyboard performance). Past tempo segments are frozen as they are
    /// observed; <see cref="SetTempo"/> only changes the future. Writers serialise on an internal lock;
    /// readers are lock-free (segment array is swapped atomically).
    /// </summary>
    public sealed class LiveTempoMap : ITempoMap
    {
        private readonly object writerLock = new();
        private volatile Segment[] segments;

        /// <summary>Creates a tempo map starting at the given BPM from tick 0.</summary>
        public LiveTempoMap(double initialBpm)
        {
            segments = new[] { Segment.FromBpm(0, 0.0, initialBpm) };
        }

        /// <summary>The current (most recently set) tempo in BPM.</summary>
        public double CurrentBpm => segments[^1].Bpm;

        /// <summary>
        /// Sets the tempo from <paramref name="effectiveAtTicks"/> onward. The tick must be at or
        /// after the last segment's start. If it equals the last segment's start (no progress observed yet),
        /// the last segment is replaced rather than appended.
        /// </summary>
        public void SetTempo(double bpm, long effectiveAtTicks)
        {
            if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be positive.");
            if (effectiveAtTicks < 0) throw new ArgumentOutOfRangeException(nameof(effectiveAtTicks), "Ticks must not be negative.");

            lock (writerLock)
            {
                var current = segments;
                var last = current[^1];
                if (effectiveAtTicks < last.StartTick)
                    throw new InvalidOperationException(
                        $"Cannot rewind the live tempo map: requested change at tick {effectiveAtTicks}, last segment starts at {last.StartTick}.");

                var elapsedSecs = (effectiveAtTicks - last.StartTick) * last.SecondsPerTick;
                var startSecs = last.StartSeconds + elapsedSecs;
                var newSeg = Segment.FromBpm(effectiveAtTicks, startSecs, bpm);

                Segment[] next;
                if (effectiveAtTicks == last.StartTick)
                {
                    next = (Segment[])current.Clone();
                    next[^1] = newSeg;
                }
                else
                {
                    next = new Segment[current.Length + 1];
                    Array.Copy(current, next, current.Length);
                    next[^1] = newSeg;
                }
                segments = next;
            }
        }

        /// <inheritdoc/>
        public double SecondsFromTicks(long ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks must not be negative.");
            var segs = segments;
            var seg = FindByTicks(segs, ticks);
            return seg.StartSeconds + (ticks - seg.StartTick) * seg.SecondsPerTick;
        }

        /// <inheritdoc/>
        public long TicksFromSeconds(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must not be negative.");
            var segs = segments;
            var seg = FindBySeconds(segs, seconds);
            return seg.StartTick + (long)((seconds - seg.StartSeconds) / seg.SecondsPerTick);
        }

        /// <inheritdoc/>
        public double BpmAtTicks(long ticks)
        {
            var segs = segments;
            return FindByTicks(segs, ticks).Bpm;
        }

        /// <inheritdoc/>
        public long? NextChangeAfter(long tick)
        {
            var segs = segments;
            for (int i = 0; i < segs.Length; i++)
            {
                if (segs[i].StartTick > tick) return segs[i].StartTick;
            }
            return null;
        }

        private static Segment FindByTicks(Segment[] segs, long ticks)
        {
            for (int i = segs.Length - 1; i >= 0; i--)
            {
                if (segs[i].StartTick <= ticks) return segs[i];
            }
            return segs[0];
        }

        private static Segment FindBySeconds(Segment[] segs, double seconds)
        {
            for (int i = segs.Length - 1; i >= 0; i--)
            {
                if (segs[i].StartSeconds <= seconds) return segs[i];
            }
            return segs[0];
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
                var secondsPerQuarter = 60.0 / bpm;
                var secondsPerTick = secondsPerQuarter / MusicalTime.CanonicalPpq;
                return new Segment(startTick, startSeconds, secondsPerTick, bpm);
            }
        }
    }
}
