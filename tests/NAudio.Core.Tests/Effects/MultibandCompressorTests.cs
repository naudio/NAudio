using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class MultibandCompressorEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        private static float Rms(float[] b)
        {
            double s = 0;
            foreach (var v in b)
                s += v * (double)v;
            return MathF.Sqrt((float)(s / b.Length));
        }

        [Test]
        public void DefaultsToThreeBands()
        {
            var mb = new MultibandCompressorEffect();
            Assert.That(mb.Bands, Has.Count.EqualTo(3));
        }

        [Test]
        public void CompressesOnlyTheBandContainingEnergy()
        {
            var mb = new MultibandCompressorEffect(120f, 2000f);
            foreach (var band in mb.Bands)
            {
                band.ThresholdDb = -24f;
                band.Ratio = 8f;
            }
            mb.Configure(Mono);

            var buffer = new float[48000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * (2f * MathF.PI * 80f / 48000f)); // low band only
            var inputRms = Rms(buffer);

            mb.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
            Assert.That(mb.Bands[0].GainReductionDb, Is.GreaterThan(0f));
            Assert.That(mb.Bands[2].GainReductionDb, Is.LessThan(1f));
            Assert.That(Rms(buffer), Is.LessThan(inputRms));
        }

        [Test]
        public void QuietSignalPassesThroughNearUnityEnergy()
        {
            var mb = new MultibandCompressorEffect(120f, 2000f);
            foreach (var band in mb.Bands)
                band.ThresholdDb = -6f;
            mb.Configure(Mono);

            var buffer = new float[48000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0.02f * MathF.Sin(i * (2f * MathF.PI * 600f / 48000f));
            var inputRms = Rms(buffer);

            mb.Process(buffer);

            foreach (var band in mb.Bands)
                Assert.That(band.GainReductionDb, Is.LessThan(1f));
            Assert.That(Rms(buffer), Is.EqualTo(inputRms).Within(inputRms * 0.25f));
        }
    }
}
