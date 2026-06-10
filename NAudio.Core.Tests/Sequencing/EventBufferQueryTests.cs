using System;
using System.Collections.Generic;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class EventBufferQueryTests
    {
        // Same clean-math setup as SequencedSampleProviderTests:
        //   48 kHz / 120 BPM ⇒ 1 quarter = 24000 frames = PPQ ticks; 1 16th = 6000 frames.
        private const int SampleRate = 48000;
        private static readonly long Sixteenth = MusicalTime.TicksPerDivision(16);
        private static readonly IPositionTransform Identity = IdentityPositionTransform.Instance;

        private static (List<(int payload, int offset)> log, Action<SequencerEvent<int>, int> dispatcher) NewLog()
        {
            var log = new List<(int, int)>();
            Action<SequencerEvent<int>, int> d = (ev, off) => log.Add((ev.Payload, off));
            return (log, d);
        }

        [Test]
        public void Empty_Timeline_Dispatches_Nothing()
        {
            var (log, dispatch) = NewLog();
            EventBufferQuery.Query(new EventTimeline<int>(), new StaticTempoMap(120), Identity,
                                   null, 0, 24000, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Empty_Range_Dispatches_Nothing()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 1);
            var (log, dispatch) = NewLog();
            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, null, 100, 100, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Event_At_Range_Start_Fires_At_Offset_Zero()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(Sixteenth, 42); // frame 6000
            var (log, dispatch) = NewLog();
            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, null, 6000, 12000, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].offset, Is.EqualTo(0));
        }

        [Test]
        public void Split_Buffer_Across_Two_Calls_Fires_Event_Exactly_Once()
        {
            // The VST3 use case: a 24000-frame host pull split into two 12000-frame plugin process() calls.
            // The event at frame 6000 fires in the first half; the event at frame 18000 in the second half.
            var timeline = new EventTimeline<int>();
            timeline.Add(Sixteenth, 1);          // frame 6000
            timeline.Add(Sixteenth * 3, 2);      // frame 18000
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, null, 0, 12000, SampleRate, dispatch);
            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, null, 12000, 24000, SampleRate, dispatch);

            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log[0], Is.EqualTo((1, 6000)));
            Assert.That(log[1], Is.EqualTo((2, 6000)));     // offset is relative to the second range's start
        }

        [Test]
        public void Same_Query_Called_Twice_Is_Idempotent()
        {
            // Stateless: the call has no internal cursor. Calling with the same range twice yields the
            // same dispatch both times.
            var timeline = new EventTimeline<int>();
            timeline.Add(Sixteenth, 42);
            var tempoMap = new StaticTempoMap(120);
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, tempoMap, Identity, null, 0, 24000, SampleRate, dispatch);
            EventBufferQuery.Query(timeline, tempoMap, Identity, null, 0, 24000, SampleRate, dispatch);

            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log[0], Is.EqualTo(log[1]));
        }

        [Test]
        public void Looped_Query_Fires_Event_Once_Per_Iteration_Inside_Range()
        {
            // Loop length 4 * PPQ ticks = 2 sec = 96000 frames. Event at tick 0 fires at the start
            // of each iteration. A query covering [0, 192001) should see two firings (iter 0 + iter 1)
            // plus the start of iter 2 if it lands exactly at frame 192000 — verify the half-open
            // boundary excludes it.
            var loopLen = MusicalTime.CanonicalPpq * 4L;
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 99);
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, new LoopRegion(0, loopLen),
                                   0, 192000, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log[0].offset, Is.EqualTo(0));
            Assert.That(log[1].offset, Is.EqualTo(96000));
        }

        [Test]
        public void Looped_Query_Excludes_Event_At_Exact_LoopEnd_Tick()
        {
            // LoopRegion is documented half-open: events at tick == loop.EndTick belong to the next
            // iteration's StartTick, not the current iteration's end. Placing an event there must not
            // fire on every iteration.
            var loopLen = MusicalTime.CanonicalPpq * 4L;
            var timeline = new EventTimeline<int>();
            timeline.Add(loopLen, 99); // event at exactly loop.EndTick
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, new LoopRegion(0, loopLen),
                                   0, 192000, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Looped_Query_Excludes_Event_Past_LoopEnd_Tick()
        {
            // Events placed nominally past loop.EndTick are unreachable loop content; they must not
            // fire each iteration even though they sit within the timeline-query over-scan window of
            // a prior implementation.
            var loopLen = MusicalTime.CanonicalPpq * 4L;
            var timeline = new EventTimeline<int>();
            timeline.Add(loopLen + 5, 99);
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, new LoopRegion(0, loopLen),
                                   0, 192000, SampleRate, dispatch);
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Looped_Query_Covers_PreRoll_Before_The_Loop_Region()
        {
            // Loop over bar 2 only (ticks [bar, 2*bar)). An event in bar 1 is pre-roll: it fires
            // exactly once on the way in; the event at the loop start then fires every iteration.
            long bar = MusicalTime.CanonicalPpq * 4L; // 1 bar = 2 s = 96000 frames at 120 bpm / 48 kHz
            var timeline = new EventTimeline<int>();
            timeline.Add(bar / 2, 1);                 // pre-roll event -> frame 48000
            timeline.Add(bar, 2);                     // loop-start event -> frames 96000, 192000, ...
            var (log, dispatch) = NewLog();

            EventBufferQuery.Query(timeline, new StaticTempoMap(120), Identity, new LoopRegion(bar, bar * 2),
                                   0, 288000, SampleRate, dispatch);

            Assert.That(log, Is.EqualTo(new[] { (1, 48000), (2, 96000), (2, 192000) }));
        }

        [Test]
        public void Query_Does_Not_Allocate_In_Steady_State()
        {
            // The audio-thread contract: per-buffer queries (looped and unlooped) run over the
            // timeline's immutable snapshot with zero heap allocations once warmed up.
            var timeline = new EventTimeline<int>();
            for (int i = 0; i < 64; i++) timeline.Add(i * 60, i); // ticks 0..3780, inside the loop below
            var tempoMap = new StaticTempoMap(120);
            var loop = new LoopRegion(0, MusicalTime.CanonicalPpq * 4L);
            long dispatched = 0;
            Action<SequencerEvent<int>, int> dispatch = (_, _) => dispatched++;

            void RunBlocks(int blocks)
            {
                for (int b = 0; b < blocks; b++)
                {
                    long start = b * 1024L;
                    EventBufferQuery.Query(timeline, tempoMap, Identity, null, start, start + 1024, SampleRate, dispatch);
                    EventBufferQuery.Query(timeline, tempoMap, Identity, loop, start, start + 1024, SampleRate, dispatch);
                }
            }

            RunBlocks(100); // warm-up (JIT)
            long before = GC.GetAllocatedBytesForCurrentThread();
            RunBlocks(200); // ~4.3 s of audio: crosses the loop seam twice on the looped query
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.Zero, "EventBufferQuery.Query must not allocate in steady state");
            Assert.That(dispatched, Is.GreaterThan(0), "the measured queries really dispatched events");
        }

        [Test]
        public void Throws_On_Null_Args()
        {
            var t = new EventTimeline<int>();
            var m = new StaticTempoMap(120);
            Action<SequencerEvent<int>, int> d = (_, _) => { };
            Assert.Throws<ArgumentNullException>(() => EventBufferQuery.Query<int>(null!, m, Identity, null, 0, 100, SampleRate, d));
            Assert.Throws<ArgumentNullException>(() => EventBufferQuery.Query(t, null!, Identity, null, 0, 100, SampleRate, d));
            Assert.Throws<ArgumentNullException>(() => EventBufferQuery.Query(t, m, null!, null, 0, 100, SampleRate, d));
            Assert.Throws<ArgumentNullException>(() => EventBufferQuery.Query(t, m, Identity, null, 0, 100, SampleRate, null!));
        }

        [Test]
        public void Throws_On_Bad_Range_Args()
        {
            var t = new EventTimeline<int>();
            var m = new StaticTempoMap(120);
            Action<SequencerEvent<int>, int> d = (_, _) => { };
            Assert.Throws<ArgumentOutOfRangeException>(() => EventBufferQuery.Query(t, m, Identity, null, -1, 100, SampleRate, d));
            Assert.Throws<ArgumentOutOfRangeException>(() => EventBufferQuery.Query(t, m, Identity, null, 0, 100, 0, d));
        }
    }
}
