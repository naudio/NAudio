using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class OversamplerTests
    {
        [Test]
        public void FactorOneIsTransparent()
        {
            var os = new Oversampler(1, 48000);
            Span<float> work = stackalloc float[1];
            os.Upsample(0.42f, work);
            Assert.That(work[0], Is.EqualTo(0.42f));
            Assert.That(os.Downsample(work), Is.EqualTo(0.42f));
        }

        [Test]
        public void PreservesDcThroughUpAndDown()
        {
            var os = new Oversampler(2, 48000);
            Span<float> work = stackalloc float[2];
            float last = 0f;
            for (var i = 0; i < 4000; i++)
            {
                os.Upsample(1f, work);
                last = os.Downsample(work);
            }
            Assert.That(last, Is.EqualTo(1f).Within(0.05f));
        }

        [Test]
        public void ConstructorValidatesFactor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Oversampler(3, 48000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Oversampler(2, 0));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class SaturationEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        [Test]
        public void HardClipBoundsOutput()
        {
            var sat = new SaturationEffect
            {
                Curve = SaturationCurve.HardClip,
                DriveDb = 20f,
                OutputGainDb = 0f,
                OversampleFactor = 1
            };
            sat.Configure(Mono);

            var buffer = new float[512];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * 0.2f);
            sat.Process(buffer);

            foreach (var s in buffer)
                Assert.That(MathF.Abs(s), Is.LessThanOrEqualTo(1f + 1e-6f));
        }

        [Test]
        public void LowLevelInputIsNearLinearWithNoDrive()
        {
            var sat = new SaturationEffect
            {
                Curve = SaturationCurve.Tanh,
                DriveDb = 0f,
                OutputGainDb = 0f,
                OversampleFactor = 1
            };
            sat.Configure(Mono);

            var buffer = new[] { 0.001f, -0.002f, 0.0015f };
            var expected = (float[])buffer.Clone();
            sat.Process(buffer);

            for (var i = 0; i < buffer.Length; i++)
                Assert.That(buffer[i], Is.EqualTo(expected[i]).Within(1e-5f));
        }

        [Test]
        public void OversampledRunsCleanly()
        {
            var sat = new SaturationEffect { OversampleFactor = 4, DriveDb = 12f };
            sat.Configure(Mono);

            var buffer = new float[1024];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * 0.3f);
            sat.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class BitCrusherEffectTests
    {
        private static WaveFormat Stereo => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        [Test]
        public void SampleRateReductionHoldsSamples()
        {
            // 48 kHz / 12 kHz target ⇒ hold factor 4.
            var crusher = new BitCrusherEffect { BitDepth = 16, TargetSampleRate = 12000 };
            crusher.Configure(Stereo);

            var buffer = new float[2 * 8];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = i * 0.01f;
            crusher.Process(buffer);

            // Each channel's value is held for 4 frames.
            for (var frame = 1; frame < 4; frame++)
            {
                Assert.That(buffer[frame * 2], Is.EqualTo(buffer[0]));
                Assert.That(buffer[frame * 2 + 1], Is.EqualTo(buffer[1]));
            }
        }

        [Test]
        public void OneBitProducesCoarseQuantisation()
        {
            var crusher = new BitCrusherEffect { BitDepth = 1 };
            crusher.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

            var buffer = new float[256];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * 0.1f);
            crusher.Process(buffer);

            foreach (var s in buffer)
                Assert.That(s == -1f || s == 0f || s == 1f, Is.True, $"unexpected level {s}");
        }

        [Test]
        public void ValidatesParameters()
        {
            var crusher = new BitCrusherEffect();
            Assert.Throws<ArgumentOutOfRangeException>(() => crusher.BitDepth = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => crusher.BitDepth = 33);
            // A target rate of 0 (or negative) is valid and means "no reduction".
            crusher.TargetSampleRate = 0;
            Assert.That(crusher.TargetSampleRate, Is.EqualTo(0));
            crusher.TargetSampleRate = -5;
            Assert.That(crusher.TargetSampleRate, Is.EqualTo(0));
        }

        [Test]
        public void TargetRateOffPassesSamplesThrough()
        {
            var crusher = new BitCrusherEffect { BitDepth = 32, TargetSampleRate = 0 };
            crusher.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

            var input = new float[64];
            for (var i = 0; i < input.Length; i++)
                input[i] = MathF.Sin(i * 0.3f);
            var buffer = (float[])input.Clone();
            crusher.Process(buffer);

            for (var i = 0; i < buffer.Length; i++)
                Assert.That(buffer[i], Is.EqualTo(input[i]).Within(1e-3f));
        }

        [Test]
        public void SmoothingReducesHarshnessOfDecimation()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            var input = new float[4096];
            for (var i = 0; i < input.Length; i++)
                input[i] = MathF.Sin(i * 0.07f);

            var raw = new BitCrusherEffect { BitDepth = 16, TargetSampleRate = 6000, Smoothing = false };
            raw.Configure(format);
            var a = (float[])input.Clone();
            raw.Process(a);

            var smooth = new BitCrusherEffect { BitDepth = 16, TargetSampleRate = 6000, Smoothing = true };
            smooth.Configure(format);
            var b = (float[])input.Clone();
            smooth.Process(b);

            // The smoothed path must differ from the raw sample-and-hold and stay finite.
            var different = false;
            for (var i = 1024; i < input.Length; i++)
            {
                Assert.That(float.IsFinite(b[i]), Is.True);
                if (MathF.Abs(a[i] - b[i]) > 1e-3f)
                    different = true;
            }
            Assert.That(different, Is.True);
        }
    }
}
