using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class MusicalTimeTests
    {
        [Test]
        public void TicksPerDivision_QuarterEqualsCanonicalPpq()
        {
            Assert.That(MusicalTime.TicksPerDivision(4), Is.EqualTo(MusicalTime.CanonicalPpq));
        }

        [Test]
        public void TicksPerDivision_EighthIsHalfPpq()
        {
            Assert.That(MusicalTime.TicksPerDivision(8), Is.EqualTo(MusicalTime.CanonicalPpq / 2));
        }

        [Test]
        public void TicksPerDivision_SixteenthIsQuarterPpq()
        {
            Assert.That(MusicalTime.TicksPerDivision(16), Is.EqualTo(MusicalTime.CanonicalPpq / 4));
        }

        [Test]
        public void TicksPerDivision_WholeIsFourPpq()
        {
            Assert.That(MusicalTime.TicksPerDivision(1), Is.EqualTo(MusicalTime.CanonicalPpq * 4));
        }

        [Test]
        public void QuarterTripletTicks_IsTwoThirdsOfQuarter()
        {
            Assert.That(MusicalTime.QuarterTripletTicks, Is.EqualTo(MusicalTime.CanonicalPpq * 2 / 3));
        }

        [Test]
        public void RescaleFromPpq_AtCanonicalPpq_IsIdentity()
        {
            Assert.That(MusicalTime.RescaleFromPpq(0, MusicalTime.CanonicalPpq), Is.EqualTo(0));
            Assert.That(MusicalTime.RescaleFromPpq(1234, MusicalTime.CanonicalPpq), Is.EqualTo(1234));
        }

        [Test]
        public void RescaleFromPpq_FromHalfPpq_DoublesTickValues()
        {
            int halfPpq = MusicalTime.CanonicalPpq / 2;
            Assert.That(MusicalTime.RescaleFromPpq(240, halfPpq), Is.EqualTo(480 * MusicalTime.CanonicalPpq / MusicalTime.CanonicalPpq));
            // 1 quarter in half-PPQ is `halfPpq` ticks; should map to canonical PPQ (= 1 quarter).
            Assert.That(MusicalTime.RescaleFromPpq(halfPpq, halfPpq), Is.EqualTo(MusicalTime.CanonicalPpq));
        }

        [Test]
        public void RescaleFromPpq_FromCommonMidiPpqs_Roundtrips()
        {
            // Common MIDI PPQs: 96, 480. A quarter note at 96 PPQ is 96 ticks; should become CanonicalPpq.
            Assert.That(MusicalTime.RescaleFromPpq(96, 96), Is.EqualTo(MusicalTime.CanonicalPpq));
            Assert.That(MusicalTime.RescaleFromPpq(480, 480), Is.EqualTo(MusicalTime.CanonicalPpq));
        }

        [Test]
        public void RescaleFromPpq_NegativeTick_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MusicalTime.RescaleFromPpq(-1, 480));
        }

        [Test]
        public void RescaleFromPpq_NonPositivePpq_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MusicalTime.RescaleFromPpq(100, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MusicalTime.RescaleFromPpq(100, -1));
        }
    }
}
