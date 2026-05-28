using System;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class StaticTempoMapTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void SingleTempo_Convenience_Ctor()
        {
            var map = new StaticTempoMap(120);
            Assert.That(map.SecondsFromTicks(MusicalTime.CanonicalPpq), Is.EqualTo(0.5).Within(Tolerance));
        }

        [Test]
        public void PiecewiseTempo_RealTimes_Are_Cumulative()
        {
            // Bar 1 at 120 BPM (one bar = 2 sec at 4/4), bar 2 at 60 BPM (one bar = 4 sec at 4/4).
            var oneBar = MusicalTime.CanonicalPpq * 4L;
            var map = new StaticTempoMap(new[] { (0L, 120.0), (oneBar, 60.0) });
            Assert.That(map.SecondsFromTicks(0), Is.EqualTo(0.0).Within(Tolerance));
            Assert.That(map.SecondsFromTicks(oneBar), Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(map.SecondsFromTicks(oneBar * 2L), Is.EqualTo(6.0).Within(Tolerance));
        }

        [Test]
        public void TicksFromSeconds_FindsCorrectSegment()
        {
            var oneBar = MusicalTime.CanonicalPpq * 4L;
            var map = new StaticTempoMap(new[] { (0L, 120.0), (oneBar, 60.0) });
            // 3 seconds is 1 bar in (2 sec) + 1 second into bar 2 at 60 BPM (1 sec = 1 quarter).
            Assert.That(map.TicksFromSeconds(3.0), Is.EqualTo(oneBar + MusicalTime.CanonicalPpq));
        }

        [Test]
        public void BpmAtTicks_ReportsActiveSegment()
        {
            var oneBar = MusicalTime.CanonicalPpq * 4L;
            var map = new StaticTempoMap(new[] { (0L, 120.0), (oneBar, 60.0) });
            Assert.That(map.BpmAtTicks(0), Is.EqualTo(120));
            Assert.That(map.BpmAtTicks(oneBar - 1), Is.EqualTo(120));
            Assert.That(map.BpmAtTicks(oneBar), Is.EqualTo(60));
        }

        [Test]
        public void Throws_If_First_Entry_NotAtZero()
        {
            Assert.Throws<ArgumentException>(() => new StaticTempoMap(new[] { (10L, 120.0) }));
        }

        [Test]
        public void Throws_If_Entries_NotMonotonic()
        {
            Assert.Throws<ArgumentException>(() => new StaticTempoMap(new[] { (0L, 120.0), (100L, 90.0), (100L, 60.0) }));
        }
    }
}
