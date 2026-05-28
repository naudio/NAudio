using System;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class LiveTempoMapTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void At120Bpm_OneQuarterIsHalfSecond()
        {
            var map = new LiveTempoMap(120);
            Assert.That(map.SecondsFromTicks(MusicalTime.CanonicalPpq), Is.EqualTo(0.5).Within(Tolerance));
        }

        [Test]
        public void At60Bpm_OneQuarterIsOneSecond()
        {
            var map = new LiveTempoMap(60);
            Assert.That(map.SecondsFromTicks(MusicalTime.CanonicalPpq), Is.EqualTo(1.0).Within(Tolerance));
        }

        [Test]
        public void TicksFromSeconds_RoundTrips_AtConstantTempo_Within_One_Tick()
        {
            // Double-precision arithmetic loses sub-tick precision, so the round trip is allowed
            // to land one tick short. The wider system filters dispatch on frames not ticks, so
            // a 1-tick wobble at the boundary has no functional effect.
            var map = new LiveTempoMap(140);
            for (long tick = 0; tick < MusicalTime.CanonicalPpq * 16L; tick += 13)
            {
                var sec = map.SecondsFromTicks(tick);
                var roundTripped = map.TicksFromSeconds(sec);
                Assert.That(roundTripped, Is.InRange(tick - 1, tick), $"Failed at tick {tick}");
            }
        }

        [Test]
        public void SetTempo_AffectsFutureOnly()
        {
            var map = new LiveTempoMap(120);                  // 0.5 sec/quarter
            // Run for two quarters at 120 → 1 second.
            var twoQuarters = MusicalTime.CanonicalPpq * 2L;
            var secsAfterTwoQuarters = map.SecondsFromTicks(twoQuarters);
            Assert.That(secsAfterTwoQuarters, Is.EqualTo(1.0).Within(Tolerance));

            map.SetTempo(60, twoQuarters);                    // halve tempo from there

            // Pre-change ticks still resolve to the same seconds.
            Assert.That(map.SecondsFromTicks(MusicalTime.CanonicalPpq), Is.EqualTo(0.5).Within(Tolerance));
            // Two more quarters at 60 BPM → 2 seconds, total 3.
            var fourQuarters = MusicalTime.CanonicalPpq * 4L;
            Assert.That(map.SecondsFromTicks(fourQuarters), Is.EqualTo(3.0).Within(Tolerance));
        }

        [Test]
        public void SetTempo_At_Same_Tick_Replaces_LastSegment()
        {
            var map = new LiveTempoMap(120);
            map.SetTempo(60, 0);
            map.SetTempo(240, 0);
            Assert.That(map.CurrentBpm, Is.EqualTo(240));
            // At 240 BPM (0.25 sec/quarter), one quarter is 0.25 sec.
            Assert.That(map.SecondsFromTicks(MusicalTime.CanonicalPpq), Is.EqualTo(0.25).Within(Tolerance));
        }

        [Test]
        public void SetTempo_Backwards_Throws()
        {
            var map = new LiveTempoMap(120);
            map.SetTempo(60, MusicalTime.CanonicalPpq * 4L);
            Assert.Throws<InvalidOperationException>(() => map.SetTempo(90, MusicalTime.CanonicalPpq));
        }

        [Test]
        public void BpmAtTicks_ReportsActiveSegment()
        {
            var map = new LiveTempoMap(120);
            map.SetTempo(90, MusicalTime.CanonicalPpq * 4L);
            Assert.That(map.BpmAtTicks(0), Is.EqualTo(120));
            Assert.That(map.BpmAtTicks(MusicalTime.CanonicalPpq * 2L), Is.EqualTo(120));
            Assert.That(map.BpmAtTicks(MusicalTime.CanonicalPpq * 4L), Is.EqualTo(90));
            Assert.That(map.BpmAtTicks(MusicalTime.CanonicalPpq * 8L), Is.EqualTo(90));
        }
    }
}
