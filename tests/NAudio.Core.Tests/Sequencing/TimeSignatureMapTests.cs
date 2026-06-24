using System;
using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class TimeSignatureMapTests
    {
        [Test]
        public void FromTicks_OriginIsBar1Beat1Tick0_In4_4()
        {
            var map = new TimeSignatureMap(TimeSignature.FourFour);
            Assert.That(map.FromTicks(0), Is.EqualTo(BarBeatTick.Start));
        }

        [Test]
        public void FromTicks_OneQuarterIs_Bar1Beat2()
        {
            var map = new TimeSignatureMap(TimeSignature.FourFour);
            var pos = map.FromTicks(MusicalTime.CanonicalPpq);
            Assert.That(pos, Is.EqualTo(new BarBeatTick(1, 2, 0)));
        }

        [Test]
        public void FromTicks_OneBarIs_Bar2Beat1()
        {
            var map = new TimeSignatureMap(TimeSignature.FourFour);
            var pos = map.FromTicks(MusicalTime.CanonicalPpq * 4L);
            Assert.That(pos, Is.EqualTo(new BarBeatTick(2, 1, 0)));
        }

        [Test]
        public void FromTicks_SubBeat_TickInBeatIsCorrect()
        {
            var map = new TimeSignatureMap(TimeSignature.FourFour);
            // Halfway through beat 1: PPQ/2 ticks past the start of beat 1.
            var pos = map.FromTicks(MusicalTime.CanonicalPpq / 2);
            Assert.That(pos, Is.EqualTo(new BarBeatTick(1, 1, MusicalTime.CanonicalPpq / 2)));
        }

        [Test]
        public void RoundTrip_ManyPositions()
        {
            var map = new TimeSignatureMap(TimeSignature.FourFour);
            for (long tick = 0; tick < MusicalTime.CanonicalPpq * 32L; tick += 37)
            {
                var pos = map.FromTicks(tick);
                Assert.That(map.ToTicks(pos), Is.EqualTo(tick), $"Failed at tick {tick}");
            }
        }

        [Test]
        public void SixEight_TicksPerBeatIsHalfPpq()
        {
            var sig = new TimeSignature(6, 8);
            Assert.That(sig.TicksPerBeat, Is.EqualTo(MusicalTime.CanonicalPpq / 2));
            Assert.That(sig.TicksPerBar, Is.EqualTo(MusicalTime.CanonicalPpq * 3));
        }

        [Test]
        public void SixEight_OneBarIs_Bar2Beat1()
        {
            var map = new TimeSignatureMap(new TimeSignature(6, 8));
            var pos = map.FromTicks(MusicalTime.CanonicalPpq * 3L);
            Assert.That(pos, Is.EqualTo(new BarBeatTick(2, 1, 0)));
        }

        [Test]
        public void Multiple_TimeSignatures_BarCountingContinues()
        {
            // Two bars of 4/4 then 3/4 from there.
            var map = new TimeSignatureMap(new[]
            {
                (0L, TimeSignature.FourFour),
                (MusicalTime.CanonicalPpq * 8L, new TimeSignature(3, 4)),
            });

            // Start of the 3/4 section should be bar 3 (after two 4/4 bars).
            var pos = map.FromTicks(MusicalTime.CanonicalPpq * 8L);
            Assert.That(pos.Bar, Is.EqualTo(3));
            Assert.That(pos.Beat, Is.EqualTo(1));
            Assert.That(pos.TickInBeat, Is.EqualTo(0));

            // One bar of 3/4 later → bar 4.
            var pos2 = map.FromTicks(MusicalTime.CanonicalPpq * 11L);
            Assert.That(pos2.Bar, Is.EqualTo(4));
            Assert.That(pos2.Beat, Is.EqualTo(1));
        }

        [Test]
        public void Throws_If_TimeSignatureChange_Not_On_BarBoundary()
        {
            // 4/4 bar is 4 PPQ ticks; mid-bar at PPQ*2 is not a bar boundary.
            Assert.Throws<ArgumentException>(() => new TimeSignatureMap(new[]
            {
                (0L, TimeSignature.FourFour),
                (MusicalTime.CanonicalPpq * 2L, new TimeSignature(3, 4)),
            }));
        }

        [Test]
        public void Throws_If_First_Entry_Not_At_Zero()
        {
            Assert.Throws<ArgumentException>(() => new TimeSignatureMap(new[]
            {
                (10L, TimeSignature.FourFour),
            }));
        }
    }
}
