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
    }
}
