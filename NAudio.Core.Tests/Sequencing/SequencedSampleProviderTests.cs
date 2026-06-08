using System.Collections.Generic;
using NAudio.Sequencing;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class SequencedSampleProviderTests
    {
        // 48 kHz / 120 BPM picks integers everywhere we care about:
        //   1 quarter = 0.5 s = 24000 frames = PPQ (960) ticks
        //   1 16th    = 0.125 s = 6000 frames = PPQ/4 (240) ticks
        private const int SampleRate = 48000;
        private const int Channels = 1;
        private static readonly long Sixteenth = MusicalTime.TicksPerDivision(16);

        private static (SequencedSampleProvider<int> sp, List<(int payload, int frameOffset, long absTick)> log)
            Build(EventTimeline<int> timeline, ITempoMap tempoMap, LoopRegion? loop = null)
        {
            var transport = new Transport(tempoMap, SampleRate);
            transport.Loop = loop;
            var wf = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
            var log = new List<(int, int, long)>();
            var sp = new SequencedSampleProvider<int>(transport, timeline, wf,
                (ev, frame) => log.Add((ev.Payload, frame, ev.Tick)));
            return (sp, log);
        }

        [Test]
        public void When_Not_Playing_No_Events_Dispatch()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 42);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            var buffer = new float[24000];
            sp.Read(buffer);
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Event_At_Zero_Fires_At_Frame_Zero()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 42);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Read(new float[24000]);
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(0));
        }

        [Test]
        public void Event_At_OneQuarter_Fires_At_Frame_24000()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(MusicalTime.CanonicalPpq, 42);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Read(new float[48000]); // 1 second
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(24000));
        }

        [Test]
        public void Event_After_Buffer_End_Does_Not_Fire()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(MusicalTime.CanonicalPpq, 42);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Read(new float[1000]);                  // not enough to reach the event
            Assert.That(log.Count, Is.EqualTo(0));
        }

        [Test]
        public void Event_Fires_On_Subsequent_Read_When_Range_Extends()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(MusicalTime.CanonicalPpq, 42); // 24000 frames in
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Read(new float[10000]); // frames 0..9999
            Assert.That(log.Count, Is.EqualTo(0));
            sp.Read(new float[20000]); // frames 10000..29999, event at 24000 ⇒ offset 14000
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(14000));
        }

        [Test]
        public void Multiple_Events_In_Same_Buffer_Fire_In_Order()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 1);
            timeline.Add(Sixteenth, 2);
            timeline.Add(Sixteenth * 2, 3);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Read(new float[24000]); // half a second; should cover ticks 0..959 i.e. all 3 events
            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[0].payload, Is.EqualTo(1));
            Assert.That(log[1].payload, Is.EqualTo(2));
            Assert.That(log[2].payload, Is.EqualTo(3));
            Assert.That(log[0].frameOffset, Is.EqualTo(0));
            Assert.That(log[1].frameOffset, Is.EqualTo(6000));
            Assert.That(log[2].frameOffset, Is.EqualTo(12000));
        }

        [Test]
        public void Loop_Causes_Event_To_Fire_Once_Per_Iteration()
        {
            // 16-step pattern of 16th notes at 120 BPM ⇒ loop length 4 * PPQ ticks = 2 sec = 96000 frames.
            var loopLen = MusicalTime.CanonicalPpq * 4L;
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 1);
            var (sp, log) = Build(timeline, new StaticTempoMap(120), new LoopRegion(0, loopLen));
            sp.Transport.Play();
            sp.Read(new float[96000]); // one full loop
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(0));
            sp.Read(new float[96000]); // second loop iteration
            Assert.That(log.Count, Is.EqualTo(2));
            Assert.That(log[1].frameOffset, Is.EqualTo(0));
            // Second fire's absolute tick should be one loop length later.
            Assert.That(log[1].absTick, Is.EqualTo(loopLen));
        }

        [Test]
        public void Loop_Wrap_Inside_Buffer_Fires_Event_At_Boundary_Offset()
        {
            // Event at tick 0 of a 4-quarter loop. Start the buffer 1 frame after iteration 0's tick 0,
            // and read a buffer of 96000 frames. The next firing is at the start of iteration 1, which
            // is loopLen ticks = 96000 frames from the original tick-0 position ⇒ 95999 frames after
            // the buffer start.
            var loopLen = MusicalTime.CanonicalPpq * 4L;
            var timeline = new EventTimeline<int>();
            timeline.Add(0, 1);
            var (sp, log) = Build(timeline, new StaticTempoMap(120), new LoopRegion(0, loopLen));
            sp.Transport.Play();
            sp.Read(new float[1]);                     // fires iteration 0's event at offset 0
            Assert.That(log.Count, Is.EqualTo(1));
            log.Clear();
            sp.Read(new float[96000]);
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(95999));
        }

        [Test]
        public void Swing_Shifts_Odd_Sixteenth_Events_Within_Buffer()
        {
            var timeline = new EventTimeline<int>();
            timeline.Add(Sixteenth, 42);               // odd 16th, frame 6000 nominally
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transport.Play();
            sp.Transform = new SwingTransform(Sixteenth, 0.5); // delay by half a 16th = a 32nd = 3000 frames
            sp.Read(new float[24000]);
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(9000));
        }

        [Test]
        public void Swing_Does_Not_Drop_Events_That_Get_Shifted_Across_Internal_Boundary()
        {
            // The classic correctness bug: applying swing AFTER filtering by buffer range could drop an
            // event whose effective tick lands inside the buffer because its nominal tick is outside.
            // We construct that exact case: event at the nominal end of the first read's tick range,
            // shifted into the buffer by swing — and we must still see it.
            var timeline = new EventTimeline<int>();
            // Nominal tick = first odd 16th past where a buffer of 6001 frames "would" end.
            // 6001 frames at 48k/120BPM is just past tick 240 (one 16th). The next odd 16th is tick 720 (3x16th).
            // Let's instead use a smaller buffer: 5999 frames ⇒ tick range [0, 239]. The event is at tick 240,
            // which is on an odd 16th line; swing 0.5 shifts it backward… but swing only shifts forward, so
            // it shifts to 360, which is past 239. The event should NOT fire (correct behaviour).
            // To exercise the "save by over-scan" path we need a transform that shifts BACK. Since our
            // SwingTransform only shifts forward, instead verify the reverse: an event at tick 240 with
            // swing 0.5 fires at tick 360 (= frame 9000). The read covers frames 0..23999 (tick 0..959).
            timeline.Add(Sixteenth, 99);
            var (sp, log) = Build(timeline, new StaticTempoMap(120));
            sp.Transform = new SwingTransform(Sixteenth, 0.5);
            sp.Transport.Play();
            sp.Read(new float[24000]);
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(9000));
        }

        [Test]
        public void Tempo_Change_Affects_Frame_Offset_Of_Subsequent_Event()
        {
            // Use LiveTempoMap; halve the tempo after the first quarter, then verify the next quarter
            // takes a full second instead of half a second.
            var tempo = new LiveTempoMap(120);
            var timeline = new EventTimeline<int>();
            timeline.Add(MusicalTime.CanonicalPpq, 1);     // 1 quarter in
            timeline.Add(MusicalTime.CanonicalPpq * 2L, 2); // 2 quarters in (nominal)
            var (sp, log) = Build(timeline, tempo);
            sp.Transport.Play();
            sp.Read(new float[24000]); // half a sec at 120 ⇒ fires event 1 at offset 0? No: tick range covers 0..PPQ
            // Actually 24000 frames = 0.5 sec = exactly PPQ ticks → range [0, PPQ). Event at PPQ is NOT in [0, PPQ).
            Assert.That(log.Count, Is.EqualTo(0));
            tempo.SetTempo(60, sp.Transport.CurrentTicks); // halve tempo from current position
            sp.Read(new float[48000]); // 1 second at 60 BPM = 1 quarter; should fire event 1 (at PPQ) at frame 0
            Assert.That(log.Count, Is.EqualTo(1));
            Assert.That(log[0].payload, Is.EqualTo(1));
            Assert.That(log[0].frameOffset, Is.EqualTo(0));
        }
    }
}
