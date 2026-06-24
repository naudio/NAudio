using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Sequencing
{
    /// <summary>
    /// An ordered set of time signature segments, used to convert canonical ticks to and from
    /// the human-facing bar/beat/tick view. The first segment must begin at tick 0; subsequent
    /// segment boundaries must fall on a bar line of the preceding signature.
    /// </summary>
    /// <remarks>
    /// Mid-bar time signature changes are not supported — they are exceedingly rare in real scores
    /// and the bar-counting bookkeeping required to handle them is not worth the complexity in v1.
    /// </remarks>
    public sealed class TimeSignatureMap
    {
        private readonly struct Segment
        {
            public Segment(long startTick, int startBar, TimeSignature signature)
            {
                StartTick = startTick;
                StartBar = startBar;
                Signature = signature;
            }

            public long StartTick { get; }
            public int StartBar { get; }
            public TimeSignature Signature { get; }
        }

        private readonly Segment[] segments;

        /// <summary>Creates a map containing a single time signature in force from tick 0.</summary>
        public TimeSignatureMap(TimeSignature signature) : this(new[] { (0L, signature) })
        {
        }

        /// <summary>Creates a map from an ordered set of (tick, signature) entries. The first must be at tick 0.</summary>
        public TimeSignatureMap(IEnumerable<(long StartTick, TimeSignature Signature)> entries)
        {
            if (entries is null) throw new ArgumentNullException(nameof(entries));
            var list = entries.OrderBy(e => e.StartTick).ToList();
            if (list.Count == 0) throw new ArgumentException("At least one entry is required.", nameof(entries));
            if (list[0].StartTick != 0) throw new ArgumentException("First entry must start at tick 0.", nameof(entries));

            var built = new Segment[list.Count];
            built[0] = new Segment(0, 1, list[0].Signature);
            built[0].Signature.Validate();
            for (int i = 1; i < list.Count; i++)
            {
                list[i].Signature.Validate();
                var prev = built[i - 1];
                var ticksInPrev = list[i].StartTick - prev.StartTick;
                if (ticksInPrev % prev.Signature.TicksPerBar != 0)
                    throw new ArgumentException(
                        $"Time-signature change at tick {list[i].StartTick} does not fall on a bar boundary of the preceding {prev.Signature.Numerator}/{prev.Signature.Denominator} signature.",
                        nameof(entries));
                var barsInPrev = (int)(ticksInPrev / prev.Signature.TicksPerBar);
                built[i] = new Segment(list[i].StartTick, prev.StartBar + barsInPrev, list[i].Signature);
            }
            segments = built;
        }

        /// <summary>The time signature in force at the given tick.</summary>
        public TimeSignature SignatureAt(long ticks) => SegmentAtTicks(ticks).Signature;

        /// <summary>Converts a tick position to a 1-based bar/beat/tick view.</summary>
        public BarBeatTick FromTicks(long ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks), "Ticks must not be negative.");
            var seg = SegmentAtTicks(ticks);
            var rel = ticks - seg.StartTick;
            var ticksPerBar = seg.Signature.TicksPerBar;
            var ticksPerBeat = seg.Signature.TicksPerBeat;
            var barsIn = (int)(rel / ticksPerBar);
            var tickInBar = rel - (long)barsIn * ticksPerBar;
            var beatIn = (int)(tickInBar / ticksPerBeat);
            var tickInBeat = (int)(tickInBar - (long)beatIn * ticksPerBeat);
            return new BarBeatTick(seg.StartBar + barsIn, beatIn + 1, tickInBeat);
        }

        /// <summary>Converts a 1-based bar/beat/tick position back to canonical ticks.</summary>
        public long ToTicks(BarBeatTick position)
        {
            if (position.Bar < 1) throw new ArgumentOutOfRangeException(nameof(position), "Bar must be 1 or greater.");
            if (position.Beat < 1) throw new ArgumentOutOfRangeException(nameof(position), "Beat must be 1 or greater.");
            if (position.TickInBeat < 0) throw new ArgumentOutOfRangeException(nameof(position), "TickInBeat must not be negative.");

            for (int i = segments.Length - 1; i >= 0; i--)
            {
                var seg = segments[i];
                if (position.Bar < seg.StartBar) continue;
                var barOffset = position.Bar - seg.StartBar;
                if (position.Beat > seg.Signature.Numerator)
                    throw new ArgumentOutOfRangeException(nameof(position),
                        $"Beat {position.Beat} is out of range for a {seg.Signature.Numerator}/{seg.Signature.Denominator} bar.");
                if (position.TickInBeat >= seg.Signature.TicksPerBeat)
                    throw new ArgumentOutOfRangeException(nameof(position),
                        $"TickInBeat {position.TickInBeat} is out of range for the {seg.Signature.Numerator}/{seg.Signature.Denominator} signature.");
                return seg.StartTick
                       + (long)barOffset * seg.Signature.TicksPerBar
                       + (long)(position.Beat - 1) * seg.Signature.TicksPerBeat
                       + position.TickInBeat;
            }
            throw new ArgumentOutOfRangeException(nameof(position), "Position falls before the start of the map.");
        }

        private Segment SegmentAtTicks(long ticks)
        {
            for (int i = segments.Length - 1; i >= 0; i--)
            {
                if (segments[i].StartTick <= ticks) return segments[i];
            }
            return segments[0];
        }
    }
}
