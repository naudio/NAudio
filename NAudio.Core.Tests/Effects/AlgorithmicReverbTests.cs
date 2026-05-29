using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class ReverbEffectTests
    {
        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        private static float[] ImpulseStereo(int frames)
        {
            var b = new float[frames * 2];
            b[0] = 1f;
            b[1] = 1f;
            return b;
        }

        private static double Energy(float[] b, int startFrame, int endFrame)
        {
            double e = 0;
            for (var i = startFrame * 2; i < endFrame * 2; i++)
                e += b[i] * (double)b[i];
            return e;
        }

        [Test]
        public void ProducesADecayingTail()
        {
            var fx = new ReverbEffect { RoomSize = 0.7f, Mix = 1f };
            fx.Configure(Stereo);
            var buffer = ImpulseStereo(96000);
            fx.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);

            var early = Energy(buffer, 2000, 12000);
            var late = Energy(buffer, 70000, 80000);
            Assert.That(early, Is.GreaterThan(0.0));
            Assert.That(late, Is.LessThan(early));
        }

        [Test]
        public void LargerRoomDecaysMoreSlowly()
        {
            var small = new ReverbEffect { RoomSize = 0.2f, Mix = 1f };
            small.Configure(Stereo);
            var bSmall = ImpulseStereo(96000);
            small.Process(bSmall);

            var large = new ReverbEffect { RoomSize = 0.95f, Mix = 1f };
            large.Configure(Stereo);
            var bLarge = ImpulseStereo(96000);
            large.Process(bLarge);

            Assert.That(Energy(bLarge, 60000, 80000), Is.GreaterThan(Energy(bSmall, 60000, 80000)));
        }

        [Test]
        public void ZeroMixIsDry()
        {
            var fx = new ReverbEffect { Mix = 0f };
            fx.Configure(Stereo);
            var buffer = new[] { 0.3f, -0.4f, 0.5f, -0.6f };
            var expected = (float[])buffer.Clone();
            fx.Process(buffer);
            Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class FdnReverbEffectTests
    {
        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        private static float[] ImpulseStereo(int frames)
        {
            var b = new float[frames * 2];
            b[0] = 1f;
            b[1] = 1f;
            return b;
        }

        private static double Energy(float[] b, int startFrame, int endFrame)
        {
            double e = 0;
            for (var i = startFrame * 2; i < endFrame * 2; i++)
                e += b[i] * (double)b[i];
            return e;
        }

        [Test]
        public void ProducesAFiniteDecayingTail()
        {
            var fx = new FdnReverbEffect { DecaySeconds = 2f, Mix = 1f };
            fx.Configure(Stereo);
            var buffer = ImpulseStereo(96000);
            fx.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);

            var early = Energy(buffer, 2000, 12000);
            var late = Energy(buffer, 70000, 80000);
            Assert.That(early, Is.GreaterThan(0.0));
            Assert.That(late, Is.LessThan(early));
        }

        [Test]
        public void LongerDecayLeavesMoreLateEnergy()
        {
            var shortRt = new FdnReverbEffect { DecaySeconds = 0.4f, Mix = 1f };
            shortRt.Configure(Stereo);
            var bShort = ImpulseStereo(96000);
            shortRt.Process(bShort);

            var longRt = new FdnReverbEffect { DecaySeconds = 5f, Mix = 1f };
            longRt.Configure(Stereo);
            var bLong = ImpulseStereo(96000);
            longRt.Process(bLong);

            Assert.That(Energy(bLong, 60000, 80000), Is.GreaterThan(Energy(bShort, 60000, 80000)));
        }

        [Test]
        public void ZeroMixIsDry()
        {
            var fx = new FdnReverbEffect { Mix = 0f };
            fx.Configure(Stereo);
            var buffer = new[] { 0.2f, -0.3f, 0.4f, -0.5f };
            var expected = (float[])buffer.Clone();
            fx.Process(buffer);
            Assert.That(buffer, Is.EqualTo(expected).Within(1e-6f));
        }
    }
}
