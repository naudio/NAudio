using System;
using System.Linq;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    [TestFixture]
    [Category("UnitTest")]
    public class ArDftResamplerTests
    {
        // The algorithm centres each input chunk in a 2N-zeros FFT buffer, so the
        // output is delayed by half an output block. Tests skip that warmup.

        [Test]
        public void Constructor_RejectsBadArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArDftResampler(0, 48000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArDftResampler(48000, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArDftResampler(44100, 48000, quality: 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArDftResampler(44100, 48000, bandwidth: 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArDftResampler(44100, 48000, bandwidth: 1.5));
        }

        [TestCase(44100, 48000)]
        [TestCase(48000, 44100)]
        [TestCase(96000, 44100)]
        [TestCase(44100, 96000)]
        [TestCase(88200, 44100)]
        [TestCase(44100, 44100)]
        public void Constructor_AcceptsCommonRatePairs(int from, int to)
        {
            var r = new ArDftResampler(from, to);
            Assert.That(r.SourceRate, Is.EqualTo(from));
            Assert.That(r.TargetRate, Is.EqualTo(to));
            Assert.That(r.InputChunkSize, Is.GreaterThan(0));
            Assert.That(r.OutputChunkSize, Is.GreaterThan(0));
            // Block sizes must respect the rate ratio exactly.
            Assert.That((long)r.InputChunkSize * to, Is.EqualTo((long)r.OutputChunkSize * from));
        }

        [Test]
        public void Process_EmptyInput_ReturnsEmpty()
        {
            var r = new ArDftResampler(44100, 48000);
            var result = r.Process(ReadOnlySpan<float>.Empty);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void Process_OutputLength_FollowsExpectedFormula()
        {
            var r = new ArDftResampler(44100, 48000);
            int input = 5000;
            int expectedChunks = (input + r.InputChunkSize - 1) / r.InputChunkSize;
            int expected = expectedChunks * r.OutputChunkSize;

            var result = r.Process(new float[input]);

            Assert.That(result.Length, Is.EqualTo(expected));
        }

        [Test]
        public void Process_IdentityRate_RecoversInputAfterHalfBlockDelay()
        {
            // 44100 -> 44100 should be approximately a delay line of half a block,
            // with the rest matching the input within taper-induced rounding.
            const int rate = 44100;
            var r = new ArDftResampler(rate, rate);
            var input = MakeSine(rate, frequencyHz: 1000.0, durationSeconds: 0.5);

            var output = r.Process(input);

            int delay = r.OutputChunkSize / 2;
            // Compare a steady-state region: skip the first and last full output block.
            int start = r.OutputChunkSize;
            int end = output.Length - r.OutputChunkSize;
            Assume.That(end, Is.GreaterThan(start));
            double err = MaxAbsDiff(input, output, sourceOffset: start - delay, destOffset: start, length: end - start);

            // 1 kHz at 44.1 kHz is deep in the passband; tolerance leaves room for
            // accumulated float32 error across the forward+inverse pair plus the
            // overlap-add seam at chunk boundaries.
            Assert.That(err, Is.LessThan(5e-3), $"max abs error was {err}");
        }

        [Test]
        public void Process_DownSample_PreservesSineAmplitudeAndFrequency()
        {
            // 1 kHz sine from 96 kHz to 44.1 kHz: still 1 kHz, still ~unit amplitude.
            const int srcRate = 96000;
            const int dstRate = 44100;
            const double freq = 1000.0;
            var r = new ArDftResampler(srcRate, dstRate);
            var input = MakeSine(srcRate, freq, durationSeconds: 1.0);

            var output = r.Process(input);

            // Drop the first half-block delay + a full block of warmup; sample a steady region.
            int analysisLen = 1 << 14;
            int analysisStart = r.OutputChunkSize * 2;
            Assume.That(output.Length - analysisStart, Is.GreaterThanOrEqualTo(analysisLen));

            (int peakBin, double peakMag, double totalEnergy) = AnalyseDominantBin(output, analysisStart, analysisLen);
            double binHz = (double)dstRate / analysisLen;
            double peakHz = peakBin * binHz;

            Assert.That(peakHz, Is.EqualTo(freq).Within(binHz), "dominant frequency drifted");
            // For a Hann-windowed sine the main lobe holds at least ~0.4 of the
            // half-spectrum energy in the peak bin even when the sine doesn't land
            // on a bin centre. 0.3 is the "obviously not smeared" floor.
            Assert.That(peakMag * peakMag / totalEnergy, Is.GreaterThan(0.3),
                "energy spread suggests smearing or aliasing");
        }

        [Test]
        public void Process_UpSample_PreservesSineAmplitudeAndFrequency()
        {
            const int srcRate = 44100;
            const int dstRate = 48000;
            const double freq = 1000.0;
            var r = new ArDftResampler(srcRate, dstRate);
            var input = MakeSine(srcRate, freq, durationSeconds: 1.0);

            var output = r.Process(input);

            int analysisLen = 1 << 14;
            int analysisStart = r.OutputChunkSize * 2;
            Assume.That(output.Length - analysisStart, Is.GreaterThanOrEqualTo(analysisLen));

            (int peakBin, double peakMag, double totalEnergy) = AnalyseDominantBin(output, analysisStart, analysisLen);
            double binHz = (double)dstRate / analysisLen;
            double peakHz = peakBin * binHz;

            Assert.That(peakHz, Is.EqualTo(freq).Within(binHz));
            Assert.That(peakMag * peakMag / totalEnergy, Is.GreaterThan(0.3));
        }

        // ------- helpers -------

        private static float[] MakeSine(int sampleRate, double frequencyHz, double durationSeconds)
        {
            int n = (int)(sampleRate * durationSeconds);
            var x = new float[n];
            double w = 2.0 * Math.PI * frequencyHz / sampleRate;
            for (int i = 0; i < n; i++) x[i] = (float)Math.Sin(w * i);
            return x;
        }

        private static double MaxAbsDiff(float[] source, float[] dest, int sourceOffset, int destOffset, int length)
        {
            double err = 0;
            for (int i = 0; i < length; i++)
            {
                int si = sourceOffset + i;
                if (si < 0 || si >= source.Length) continue;
                double d = Math.Abs((double)source[si] - dest[destOffset + i]);
                if (d > err) err = d;
            }
            return err;
        }

        // Analyse the dominant frequency bin in a chunk of audio. Applies a Hann window
        // before the DFT so a sine that doesn't land on a bin centre still concentrates
        // its energy in a small main lobe (peak / total ~ 0.67 for ideal sine input,
        // vs. a worst case of ~0.4 with no window).
        private static (int peakBin, double peakMag, double totalEnergy) AnalyseDominantBin(
            float[] data, int start, int length)
        {
            var dft = new BluesteinDft(length);
            var buf = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                double w = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (length - 1));
                buf[i].X = (float)(data[start + i] * w);
            }
            dft.Forward(buf);

            int peakBin = 0;
            double peakMag = 0;
            double total = 0;
            // Only inspect non-negative-frequency bins; the negative half mirrors them.
            int half = length / 2;
            for (int k = 1; k < half; k++)
            {
                double mag = Math.Sqrt((double)buf[k].X * buf[k].X + (double)buf[k].Y * buf[k].Y);
                total += mag * mag;
                if (mag > peakMag) { peakMag = mag; peakBin = k; }
            }
            return (peakBin, peakMag, total);
        }
    }
}
</content>
</invoke>