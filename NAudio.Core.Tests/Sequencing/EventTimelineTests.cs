using System;
using System.Linq;
using System.Threading;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class EventTimelineTests
    {
        [Test]
        public void Empty_Range_Returns_Empty()
        {
            var t = new EventTimeline<int>();
            Assert.That(t.EventsInRange(0, 1000).Count, Is.EqualTo(0));
        }

        [Test]
        public void Returns_Events_In_Range()
        {
            var t = new EventTimeline<int>();
            t.Add(0, 1);
            t.Add(100, 2);
            t.Add(200, 3);
            var events = t.EventsInRange(50, 250).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Range_Is_Half_Open()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(200, 2);
            var events = t.EventsInRange(100, 200).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void Out_Of_Order_Inserts_Are_Sorted()
        {
            var t = new EventTimeline<int>();
            t.Add(300, 3);
            t.Add(100, 1);
            t.Add(200, 2);
            var events = t.EventsInRange(0, 1000).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Same_Tick_Preserves_Insertion_Order()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(100, 2);
            t.Add(100, 3);
            var events = t.EventsInRange(0, 1000).Select(e => e.Payload).ToList();
            Assert.That(events, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Clear_Removes_All()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(200, 2);
            t.Clear();
            Assert.That(t.Count, Is.EqualTo(0));
        }

        [Test]
        public void FirstTick_LastTick_Reported()
        {
            var t = new EventTimeline<int>();
            Assert.That(t.FirstTick, Is.Null);
            Assert.That(t.LastTick, Is.Null);
            t.Add(100, 1);
            t.Add(50, 2);
            t.Add(200, 3);
            Assert.That(t.FirstTick, Is.EqualTo(50));
            Assert.That(t.LastTick, Is.EqualTo(200));
        }

        // ---- the allocation-free span query (EventsInRangeSpan) ----

        [Test]
        public void Span_Query_Empty_Timeline_Or_Empty_Range_Is_Empty()
        {
            var t = new EventTimeline<int>();
            Assert.That(t.EventsInRangeSpan(0, 1000).IsEmpty, Is.True);
            t.Add(50, 1);
            Assert.That(t.EventsInRangeSpan(60, 60).IsEmpty, Is.True, "empty range");
            Assert.That(t.EventsInRangeSpan(60, 40).IsEmpty, Is.True, "inverted range");
            Assert.That(t.EventsInRangeSpan(51, 1000).IsEmpty, Is.True, "range past the only event");
        }

        [Test]
        public void Span_Query_Is_Half_Open_And_Preserves_Insertion_Order()
        {
            var t = new EventTimeline<int>();
            t.Add(100, 1);
            t.Add(100, 2);
            t.Add(200, 3);
            var span = t.EventsInRangeSpan(100, 200);
            Assert.That(span.Length, Is.EqualTo(2), "start inclusive, end exclusive");
            Assert.That(span[0].Payload, Is.EqualTo(1));
            Assert.That(span[1].Payload, Is.EqualTo(2));
        }

        [Test]
        public void Span_Query_Matches_EventsInRange_Across_Edge_Cases()
        {
            var t = new EventTimeline<int>();
            long[] ticks = { 0, 10, 10, 10, 25, 100, 100, 200, 5000 };
            for (int i = 0; i < ticks.Length; i++) t.Add(ticks[i], i);

            (long start, long end)[] ranges =
            {
                (0, 1),                       // single event at the origin
                (0, 0), (5, 3),               // empty and inverted
                (0, 10), (10, 11), (9, 10),   // boundaries around a multi-event tick
                (10, 25), (10, 26),           // end exactly on / just past an event
                (0, 5001), (0, 5000),         // full range, and excluding the last event
                (200, 5001), (5001, 9000),    // tail, and beyond the end
                (26, 99), (1, 10)             // gaps with no events
            };
            foreach (var (start, end) in ranges)
            {
                var expected = t.EventsInRange(start, end);
                var actual = t.EventsInRangeSpan(start, end);
                Assert.That(actual.Length, Is.EqualTo(expected.Count), $"count for [{start},{end})");
                for (int i = 0; i < actual.Length; i++)
                    Assert.That(actual[i], Is.EqualTo(expected[i]), $"event {i} of [{start},{end})");
            }
        }

        [Test]
        public void Span_Query_Matches_EventsInRange_Randomised()
        {
            var rng = new Random(42);
            var t = new EventTimeline<int>();
            for (int i = 0; i < 500; i++) t.Add(rng.Next(0, 1000), i); // mostly duplicated, unordered ticks

            for (int q = 0; q < 2000; q++)
            {
                long start = rng.Next(0, 1100), end = rng.Next(0, 1100);
                var expected = t.EventsInRange(start, end);
                var actual = t.EventsInRangeSpan(start, end);
                Assert.That(actual.Length, Is.EqualTo(expected.Count), $"count for [{start},{end})");
                for (int i = 0; i < actual.Length; i++)
                    if (actual[i] != expected[i])
                        Assert.Fail($"event {i} of [{start},{end}): span {actual[i]} != list {expected[i]}");
            }
        }

        [Test]
        public void Span_Query_Does_Not_Allocate()
        {
            var t = new EventTimeline<int>();
            for (int i = 0; i < 256; i++) t.Add(i * 10, i);

            long total = 0;
            void Run(int iterations)
            {
                for (int i = 0; i < iterations; i++)
                {
                    var span = t.EventsInRangeSpan(i % 1000, i % 1000 + 500);
                    for (int j = 0; j < span.Length; j++) total += span[j].Tick;
                }
            }

            Run(100); // warm-up
            long before = GC.GetAllocatedBytesForCurrentThread();
            Run(1000);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "EventsInRangeSpan must not allocate");
            Assert.That(total, Is.GreaterThan(0)); // the queries really visited events
        }

        // ---- concurrency: queries must always see a consistent snapshot ----

        [Test]
        public void Concurrent_Adds_Never_Tear_A_Query()
        {
            // The writer appends (tick i, payload i) in order, so any consistent snapshot is a
            // contiguous prefix 0..k. A torn query would surface as a gap, duplicate, or mismatch.
            const int total = 5000;
            var t = new EventTimeline<int>();
            Exception readerError = null;

            var writer = new Thread(() =>
            {
                for (int i = 0; i < total; i++) t.Add(i, i);
            });
            var reader = new Thread(() =>
            {
                try
                {
                    int lastCount = 0;
                    for (long spin = 0; lastCount < total; spin++)
                    {
                        if (spin > 50_000_000) throw new InvalidOperationException("reader never saw the final snapshot");
                        var span = t.EventsInRangeSpan(0, total);
                        if (span.Length < lastCount)
                            throw new InvalidOperationException("a later query saw fewer events than an earlier one");
                        for (int i = 0; i < span.Length; i++)
                            if (span[i].Tick != i || span[i].Payload != i)
                                throw new InvalidOperationException($"torn snapshot at index {i}: {span[i]}");
                        lastCount = span.Length;
                    }
                }
                catch (Exception ex) { readerError = ex; }
            });

            writer.Start();
            reader.Start();
            writer.Join();
            reader.Join();

            Assert.That(readerError, Is.Null);
            Assert.That(t.Count, Is.EqualTo(total));
        }

        [Test]
        public void Concurrent_Clear_And_Add_Yield_Consistent_Snapshots()
        {
            // The writer builds a small batch (payload mirrors tick) then clears, repeatedly.
            // Whatever instant a query lands on, it must see in-range, ordered events whose
            // payload matches their tick — and must never throw.
            const int rounds = 2000;
            var t = new EventTimeline<long>();
            Exception readerError = null;
            bool done = false;

            var writer = new Thread(() =>
            {
                for (int r = 0; r < rounds; r++)
                {
                    for (long i = 0; i < 8; i++) t.Add(i * 10, i * 10);
                    t.Clear();
                }
                Volatile.Write(ref done, true);
            });
            var reader = new Thread(() =>
            {
                try
                {
                    while (!Volatile.Read(ref done))
                    {
                        var span = t.EventsInRangeSpan(10, 60);
                        long prev = long.MinValue;
                        for (int i = 0; i < span.Length; i++)
                        {
                            var ev = span[i];
                            if (ev.Tick < 10 || ev.Tick >= 60) throw new InvalidOperationException($"out-of-range tick {ev.Tick}");
                            if (ev.Tick < prev) throw new InvalidOperationException("ticks out of order");
                            if (ev.Payload != ev.Tick) throw new InvalidOperationException($"payload {ev.Payload} != tick {ev.Tick}");
                            prev = ev.Tick;
                        }
                        if (span.Length > 5) throw new InvalidOperationException($"{span.Length} events in a range that never holds more than 5");
                    }
                }
                catch (Exception ex) { readerError = ex; }
            });

            writer.Start();
            reader.Start();
            writer.Join();
            reader.Join();

            Assert.That(readerError, Is.Null);
            Assert.That(t.Count, Is.Zero);
        }
    }
}
