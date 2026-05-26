using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class VoiceActivityDetectorTests
    {
        [Test]
        public void DetectsABurstAfterSilenceAndReleasesAfterHangover()
        {
            const int sr = 48000;
            var vad = new VoiceActivityDetector(sr);

            bool duringSilence = true, duringBurst = false, afterBurst = true;

            // 0.5 s silence, 0.3 s loud tone, 0.6 s silence.
            for (var i = 0; i < sr / 2; i++)
                vad.Process(0f);
            duringSilence = vad.IsVoiceActive;

            for (var i = 0; i < (int)(0.3 * sr); i++)
                vad.Process(0.5f * MathF.Sin(i * 0.2f));
            duringBurst = vad.IsVoiceActive;

            var release = (int)(0.6 * sr);
            for (var i = 0; i < release; i++)
            {
                vad.Process(0f);
                if (i == release - 1)
                    afterBurst = vad.IsVoiceActive;
            }

            Assert.That(duringSilence, Is.False);
            Assert.That(duringBurst, Is.True);
            Assert.That(afterBurst, Is.False);
        }

        [Test]
        public void ConstructorValidatesArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceActivityDetector(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceActivityDetector(48000, 0f));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class AutomaticGainControlEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        private static float Rms(float[] b, int start, int end)
        {
            double s = 0;
            for (var i = start; i < end; i++)
                s += b[i] * (double)b[i];
            return MathF.Sqrt((float)(s / (end - start)));
        }

        [Test]
        public void BoostsAQuietSignal()
        {
            var agc = new AutomaticGainControlEffect { TargetDb = -18f, UseVoiceDetection = false };
            agc.Configure(Mono);

            var buffer = new float[96000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = 0.02f * MathF.Sin(i * 0.1f);
            agc.Process(buffer);

            Assert.That(agc.GainDb, Is.GreaterThan(3f));
            Assert.That(Rms(buffer, 80000, 96000), Is.GreaterThan(0.02f));
            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
        }

        [Test]
        public void AttenuatesALoudSignal()
        {
            var agc = new AutomaticGainControlEffect { TargetDb = -18f, UseVoiceDetection = false };
            agc.Configure(Mono);

            var buffer = new float[96000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * 0.1f);
            agc.Process(buffer);

            Assert.That(agc.GainDb, Is.LessThan(0f));
            Assert.That(Rms(buffer, 80000, 96000), Is.LessThan(0.8f));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class NoiseSuppressionEffectTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        private static float Rms(float[] b, int start, int end)
        {
            double s = 0;
            for (var i = start; i < end; i++)
                s += b[i] * (double)b[i];
            return MathF.Sqrt((float)(s / (end - start)));
        }

        [Test]
        public void SuppressesStationaryNoise()
        {
            var ns = new NoiseSuppressionEffect { Mix = 1f };
            ns.Configure(Mono);

            var rng = new Random(7);
            var buffer = new float[96000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
            var inputRms = Rms(buffer, 60000, 90000);

            ns.Process(buffer);

            foreach (var s in buffer)
                Assert.That(float.IsFinite(s), Is.True);
            Assert.That(Rms(buffer, 60000, 90000), Is.LessThan(inputRms * 0.6f));
        }

        [Test]
        public void PreservesAToneOverLearnedNoise()
        {
            var ns = new NoiseSuppressionEffect { Mix = 1f };
            ns.Configure(Mono);

            var rng = new Random(11);
            var buffer = new float[144000]; // 1 s noise, then 2 s noise + tone
            for (var i = 0; i < buffer.Length; i++)
            {
                var n = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.15f;
                if (i >= 48000)
                    n += 0.8f * MathF.Sin(i * (2f * MathF.PI * 700f / 48000f));
                buffer[i] = n;
            }
            ns.Process(buffer);

            var noiseOnly = Rms(buffer, 30000, 46000);
            var withTone = Rms(buffer, 120000, 140000);
            Assert.That(withTone, Is.GreaterThan(noiseOnly * 2f));
        }

        [Test]
        public void FrameSizeMustBePowerOfTwo()
        {
            var ns = new NoiseSuppressionEffect();
            Assert.Throws<ArgumentException>(() => ns.FrameSize = 300);
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class ComfortNoiseEffectTests
    {
        [Test]
        public void AddsLowLevelNoiseToSilence()
        {
            var cn = new ComfortNoiseEffect { LevelDb = -40f };
            cn.Configure(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));

            var buffer = new float[48000];
            cn.Process(buffer);

            double s = 0;
            foreach (var v in buffer)
            {
                Assert.That(float.IsFinite(v), Is.True);
                s += v * (double)v;
            }
            var rms = MathF.Sqrt((float)(s / buffer.Length));
            Assert.That(rms, Is.GreaterThan(1e-4f));
            Assert.That(rms, Is.LessThan(0.2f));
        }
    }
}
