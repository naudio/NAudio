using System;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class PitchShiftEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        private static float[] Sine(int n, float hz)
        {
            var b = new float[n];
            for (var i = 0; i < n; i++)
                b[i] = 0.5f * MathF.Sin(i * (2f * MathF.PI * hz / 48000f));
            return b;
        }

        private static float Rms(float[] b, int start, int end)
        {
            double s = 0;
            for (var i = start; i < end; i++)
                s += b[i] * (double)b[i];
            return MathF.Sqrt((float)(s / (end - start)));
        }

        private static int ZeroCrossings(float[] b, int start, int end)
        {
            var z = 0;
            for (var i = start + 1; i < end; i++)
                if ((b[i - 1] < 0f && b[i] >= 0f) || (b[i - 1] >= 0f && b[i] < 0f))
                    z++;
            return z;
        }

        [Test]
        public void UnityShiftRoughlyPreservesEnergy()
        {
            var fx = new PitchShiftEffect { PitchSemitones = 0f };
            fx.Configure(Mono);

            var input = Sine(48000, 440f);
            var inRms = Rms(input, 16000, 44000);
            var buffer = (float[])input.Clone();
            fx.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
            Assert.That(Rms(buffer, 16000, 44000), Is.EqualTo(inRms).Within(inRms * 0.6f));
        }

        [Test]
        public void OctaveUpRoughlyDoublesZeroCrossingRate()
        {
            var input = Sine(48000, 400f);
            var inZc = ZeroCrossings(input, 16000, 44000);

            var fx = new PitchShiftEffect { PitchSemitones = 12f };
            fx.Configure(Mono);
            var buffer = (float[])input.Clone();
            fx.Process(buffer);

            var outZc = ZeroCrossings(buffer, 16000, 44000);
            Assert.That(outZc, Is.GreaterThan(inZc * 1.4));
        }

        [Test]
        public void DownShiftStaysFinite()
        {
            var fx = new PitchShiftEffect { PitchSemitones = -7f };
            fx.Configure(Mono);
            var buffer = Sine(24000, 600f);
            fx.Process(buffer);
            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }

        [Test]
        public void ValidatesParametersAndClampsRange()
        {
            var fx = new PitchShiftEffect();
            Assert.Throws<ArgumentException>(() => fx.FftSize = 1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => fx.Oversampling = 0);
            fx.PitchSemitones = 24f;
            Assert.That(fx.PitchSemitones, Is.EqualTo(12f));
        }
    }
}
