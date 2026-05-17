using System;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Effects
{
    [TestFixture]
    [Category("UnitTest")]
    public class LinkwitzRileyCrossoverTests
    {
        private const int Sr = 48000;

        private static double BandEnergy(LinkwitzRileyCrossover x, float freq, int band, int bands)
        {
            Span<float> outBands = stackalloc float[bands];
            double e = 0;
            for (var i = 0; i < 9600; i++)
            {
                x.Process(MathF.Sin(i * (2f * MathF.PI * freq / Sr)), outBands);
                if (i >= 4800) // skip filter warm-up
                    e += outBands[band] * (double)outBands[band];
            }
            return e;
        }

        [Test]
        public void LowToneGoesToTheLowBand()
        {
            var x = new LinkwitzRileyCrossover(Sr, 1000f);
            Assert.That(x.Bands, Is.EqualTo(2));
            var low = BandEnergy(x, 120f, 0, 2);
            x.Reset();
            var high = BandEnergy(x, 120f, 1, 2);
            Assert.That(low, Is.GreaterThan(high * 50));
        }

        [Test]
        public void HighToneGoesToTheHighBand()
        {
            var x = new LinkwitzRileyCrossover(Sr, 1000f);
            var low = BandEnergy(x, 9000f, 0, 2);
            x.Reset();
            var high = BandEnergy(x, 9000f, 1, 2);
            Assert.That(high, Is.GreaterThan(low * 50));
        }

        [Test]
        public void ThreeBandRoutesMidToMiddleBand()
        {
            var x = new LinkwitzRileyCrossover(Sr, 300f, 3000f);
            Assert.That(x.Bands, Is.EqualTo(3));
            var mid = BandEnergy(x, 1000f, 1, 3);
            x.Reset();
            var low = BandEnergy(x, 1000f, 0, 3);
            x.Reset();
            var high = BandEnergy(x, 1000f, 2, 3);
            Assert.That(mid, Is.GreaterThan(low * 20));
            Assert.That(mid, Is.GreaterThan(high * 20));
        }

        [Test]
        public void ValidatesArguments()
        {
            Assert.Throws<ArgumentException>(() => new LinkwitzRileyCrossover(Sr));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinkwitzRileyCrossover(Sr, 30000f));
            Assert.Throws<ArgumentException>(() => new LinkwitzRileyCrossover(Sr, 2000f, 1000f));
        }
    }

    [TestFixture]
    [Category("UnitTest")]
    public class TruePeakLimiterTests
    {
        private static WaveFormat Mono => WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);

        [Test]
        public void TruePeakIsOnByDefault()
        {
            var limiter = new LimiterEffect();
            Assert.That(limiter.TruePeak, Is.True);
            limiter.Configure(Mono);
            Assert.That(limiter.LatencySamples, Is.GreaterThan(0));
        }

        [Test]
        public void TruePeakCatchesIntersamplePeaksSamplePeakMisses()
        {
            // A 0.25·fs square (+0.8,+0.8,-0.8,-0.8 …): sample peak is exactly the
            // ceiling (no sample-domain limiting), but band-limited reconstruction
            // overshoots ±0.8 between samples (Gibbs ringing).
            var input = new float[16384];
            for (var i = 0; i < input.Length; i++)
                input[i] = (i % 4) < 2 ? 0.8f : -0.8f;

            var samplePeak = new LimiterEffect { CeilingDb = SampleToDb(0.8f), TruePeak = false };
            samplePeak.Configure(Mono);
            var a = (float[])input.Clone();
            samplePeak.Process(a);
            var grSample = samplePeak.GainReductionDb;

            var truePeak = new LimiterEffect { CeilingDb = SampleToDb(0.8f), TruePeak = true, OversampleFactor = 4 };
            truePeak.Configure(Mono);
            var b = (float[])input.Clone();
            truePeak.Process(b);

            foreach (var s in b)
                Assert.That(float.IsFinite(s), Is.True);
            Assert.That(grSample, Is.EqualTo(0f));
            Assert.That(truePeak.GainReductionDb, Is.GreaterThan(0f));
        }

        [Test]
        public void StillLimitsASteadyLoudSignal()
        {
            var limiter = new LimiterEffect { CeilingDb = -6f, TruePeak = true };
            limiter.Configure(Mono);
            var ceiling = MathF.Pow(10f, -6f / 20f);

            var buffer = new float[48000];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = MathF.Sin(i * (2f * MathF.PI * 1000f / 48000f));
            limiter.Process(buffer);

            float peak = 0f;
            for (var i = 4800; i < buffer.Length; i++)
                peak = MathF.Max(peak, MathF.Abs(buffer[i]));
            Assert.That(peak, Is.LessThan(ceiling * 1.05f));
        }

        private static float SampleToDb(float linear) => 20f * MathF.Log10(linear);
    }
}
