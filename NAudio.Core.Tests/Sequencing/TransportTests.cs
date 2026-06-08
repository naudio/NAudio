using NAudio.Sequencing;
using NUnit.Framework;

namespace NAudio.Core.Tests.Sequencing
{
    [TestFixture]
    [Category("UnitTest")]
    public class TransportTests
    {
        [Test]
        public void Starts_NotPlaying_AtZero()
        {
            var transport = new Transport(new StaticTempoMap(120), 44100);
            Assert.That(transport.IsPlaying, Is.False);
            Assert.That(transport.CurrentFrames, Is.EqualTo(0));
            Assert.That(transport.CurrentTicks, Is.EqualTo(0));
        }

        [Test]
        public void Play_Flips_IsPlaying()
        {
            var transport = new Transport(new StaticTempoMap(120), 44100);
            transport.Play();
            Assert.That(transport.IsPlaying, Is.True);
            transport.Stop();
            Assert.That(transport.IsPlaying, Is.False);
        }

        [Test]
        public void Advance_Updates_Both_Frames_And_Ticks()
        {
            // 120 BPM → 1 quarter (PPQ ticks) = 0.5 sec = 22050 frames at 44100.
            var transport = new Transport(new StaticTempoMap(120), 44100);
            transport.AdvanceByFrames(22050);
            Assert.That(transport.CurrentFrames, Is.EqualTo(22050));
            Assert.That(transport.CurrentTicks, Is.EqualTo(MusicalTime.CanonicalPpq));
        }

        [Test]
        public void Seek_Frames_Updates_Ticks()
        {
            var transport = new Transport(new StaticTempoMap(120), 44100);
            transport.SeekFrames(22050);
            Assert.That(transport.CurrentFrames, Is.EqualTo(22050));
            Assert.That(transport.CurrentTicks, Is.EqualTo(MusicalTime.CanonicalPpq));
        }

        [Test]
        public void Seek_Ticks_Updates_Frames()
        {
            var transport = new Transport(new StaticTempoMap(120), 44100);
            transport.SeekTicks(MusicalTime.CanonicalPpq);
            Assert.That(transport.CurrentTicks, Is.EqualTo(MusicalTime.CanonicalPpq));
            Assert.That(transport.CurrentFrames, Is.EqualTo(22050));
        }

        [Test]
        public void Advance_Across_Tempo_Change_Lands_On_Correct_Tick()
        {
            // 1 second at 120 BPM = 2 quarters = 2 * PPQ ticks.
            // Then 1 second at 60 BPM = 1 quarter = PPQ ticks. Total: 3 * PPQ.
            var oneBar = MusicalTime.CanonicalPpq * 4L;
            var transport = new Transport(new StaticTempoMap(new[] { (0L, 120.0), (oneBar, 60.0) }), 44100);
            // Advance to the boundary first.
            transport.AdvanceByFrames(44100 * 2); // 2 seconds → 1 bar (oneBar ticks)
            Assert.That(transport.CurrentTicks, Is.EqualTo(oneBar));
            // Then 1 more second at 60 BPM → +1 quarter.
            transport.AdvanceByFrames(44100);
            Assert.That(transport.CurrentTicks, Is.EqualTo(oneBar + MusicalTime.CanonicalPpq));
        }
    }
}
