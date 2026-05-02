using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    /// <summary>
    /// Objective sample-rate-converter quality measurements: passband flatness,
    /// stop-band attenuation, single-tone THD. The ARDFTSRC tests assert loose
    /// "this isn't broken" thresholds; the parallel Wdl tests measure the same
    /// metrics on NAudio's existing managed SRC purely for side-by-side data
    /// (no asserts, just <see cref="TestContext.Progress"/> output).
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class SrcQualityTests
    {
        // Frequencies chosen to span the audible band; stay clear of new Nyquist (22.05 kHz)
        // so the passband test isn't accidentally sampling the taper transition region.
        private static readonly double[] PassbandFrequencies = { 100, 1000, 5000, 10000, 15000, 19000 };

        // ----------------- ARDFTSRC -----------------

        [Test]
        public void ArDft_Passband_FlatToWithinTolerance_96000_to_44100()
        {
            var report = MeasurePassband(ResampleArDft, srcRate: 96000, dstRate: 44100);
            TestContext.Progress.WriteLine(report.Format("ARDFTSRC", "96000 -> 44100"));

            // Allow up to 1 dB of passband loss across all measured frequencies.
            // Top-tier SRCs achieve < 0.1 dB; we'll see the actual numbers in the log.
            foreach (var (freq, peakDb) in report.PeakDbByFreq)
                Assert.That(peakDb, Is.GreaterThan(-1.0).And.LessThan(1.0),
                    $"passband loss at {freq} Hz outside ±1 dB");
        }

        [Test]
        public void ArDft_Stopband_AttenuatesAboveNyquist_96000_to_44100()
        {
            // 30 kHz tone in the source must not survive resampling to 44.1 kHz.
            double aliasDb = MeasureSteadyStatePeakDb(ResampleArDft, srcRate: 96000, dstRate: 44100, freq: 30000);
            TestContext.Progress.WriteLine($"ARDFTSRC 30 kHz alias floor (96000->44100): {aliasDb:F1} dB");

            // Top SRCs achieve < -150 dB. We're cautious here; "obviously bandlimited" is < -60 dB.
            Assert.That(aliasDb, Is.LessThan(-60.0));
        }

        [Test]
        public void ArDft_SingleToneThd_LowAt1kHz_96000_to_44100()
        {
            double thdDb = MeasureSingleToneThdDb(ResampleArDft, srcRate: 96000, dstRate: 44100, freq: 1000);
            TestContext.Progress.WriteLine($"ARDFTSRC 1 kHz THD (96000->44100): {thdDb:F1} dB");

            Assert.That(thdDb, Is.LessThan(-60.0));
        }

        // -------------- Wdl (log-only baseline) --------------

        [Test]
        public void Wdl_Passband_BaselineMeasurement_96000_to_44100()
        {
            var defaultReport = MeasurePassband(ResampleWdlDefault, srcRate: 96000, dstRate: 44100);
            TestContext.Progress.WriteLine(defaultReport.Format("Wdl(default linear+IIR)", "96000 -> 44100"));

            var sincReport = MeasurePassband(ResampleWdlSinc512, srcRate: 96000, dstRate: 44100);
            TestContext.Progress.WriteLine(sincReport.Format("Wdl(sinc-512)", "96000 -> 44100"));
        }

        [Test]
        public void Wdl_Stopband_BaselineMeasurement_96000_to_44100()
        {
            double dflt = MeasureSteadyStatePeakDb(ResampleWdlDefault, 96000, 44100, 30000);
            double sinc = MeasureSteadyStatePeakDb(ResampleWdlSinc512, 96000, 44100, 30000);
            TestContext.Progress.WriteLine($"Wdl(default) 30 kHz alias floor (96000->44100): {dflt:F1} dB");
            TestContext.Progress.WriteLine($"Wdl(sinc-512) 30 kHz alias floor (96000->44100): {sinc:F1} dB");
        }

        [Test]
        public void Wdl_SingleToneThd_BaselineMeasurement_96000_to_44100()
        {
            double dflt = MeasureSingleToneThdDb(ResampleWdlDefault, 96000, 44100, 1000);
            double sinc = MeasureSingleToneThdDb(ResampleWdlSinc512, 96000, 44100, 1000);
            TestContext.Progress.WriteLine($"Wdl(default) 1 kHz THD (96000->44100): {dflt:F1} dB");
            TestContext.Progress.WriteLine($"Wdl(sinc-512) 1 kHz THD (96000->44100): {sinc:F1} dB");
        }

        // -------------- core measurements --------------

        private sealed class PassbandReport
        {
            public List<(double freq, double peakDb)> PeakDbByFreq { get; } = new();

            public string Format(string label, string ratio)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{label} passband flatness ({ratio}):");
                foreach (var (f, db) in PeakDbByFreq)
                    sb.Append("  ").Append(f.ToString("F0", CultureInfo.InvariantCulture).PadLeft(6))
                      .Append(" Hz: ").Append(db.ToString("F2", CultureInfo.InvariantCulture).PadLeft(7))
                      .AppendLine(" dB");
                return sb.ToString();
            }
        }

        private static PassbandReport MeasurePassband(
            Func<float[], int, int, float[]> resample, int srcRate, int dstRate)
        {
            var report = new PassbandReport();
            foreach (double f in PassbandFrequencies)
            {
                if (f >= dstRate / 2.0 * 0.95) continue;     // skip frequencies inside the taper
                double linear = MeasureSteadyStatePeak(resample, srcRate, dstRate, f);
                double db = 20.0 * Math.Log10(Math.Max(linear, 1e-12));
                report.PeakDbByFreq.Add((f, db));
            }
            return report;
        }

        // Generate a long sine, resample, take peak amplitude in the steady-state region.
        // For an amplitude-1 sine the peak should be ~1.0 if the SRC preserves the band.
        private static double MeasureSteadyStatePeak(
            Func<float[], int, int, float[]> resample, int srcRate, int dstRate, double freq)
        {
            const double durationSeconds = 0.5;
            var input = MakeSine(srcRate, freq, durationSeconds, amplitude: 1.0);
            var output = resample(input, srcRate, dstRate);
            // Skip 10% from each end to avoid OLA/transient effects.
            int start = output.Length / 10;
            int end = output.Length - output.Length / 10;
            double peak = 0;
            for (int i = start; i < end; i++)
            {
                double a = Math.Abs(output[i]);
                if (a > peak) peak = a;
            }
            return peak;
        }

        private static double MeasureSteadyStatePeakDb(
            Func<float[], int, int, float[]> resample, int srcRate, int dstRate, double freq)
        {
            double linear = MeasureSteadyStatePeak(resample, srcRate, dstRate, freq);
            return 20.0 * Math.Log10(Math.Max(linear, 1e-12));
        }

        // Drive the resampler with a single-frequency sine, then FFT a bin-aligned analysis
        // window from the steady-state region. THD = 20*log10(sqrt(sum(harmonic^2)) / fundamental).
        private static double MeasureSingleToneThdDb(
            Func<float[], int, int, float[]> resample, int srcRate, int dstRate, double freq)
        {
            const double amplitude = 0.5;       // headroom against any residual DC / overshoot
            const double durationSeconds = 0.5;
            var input = MakeSine(srcRate, freq, durationSeconds, amplitude);
            var output = resample(input, srcRate, dstRate);

            // Pick an analysis FFT length such that freq lands exactly on a bin: N = dstRate / gcd(freq,dstRate).
            // For 1 kHz @ 44100: gcd = 100 -> N = 441 (smallest); use 4410 for finer resolution.
            int gcd = Gcd((int)freq, dstRate);
            int fftLen = dstRate / gcd * 10;
            int analysisStart = output.Length / 4;
            if (output.Length - analysisStart < fftLen) analysisStart = 0;
            if (output.Length < fftLen) return double.NaN;

            var dft = new BluesteinDft(fftLen);
            var buf = new Complex[fftLen];
            for (int i = 0; i < fftLen; i++) buf[i].X = output[analysisStart + i];
            dft.Forward(buf);

            int fundamentalBin = (int)Math.Round(freq * fftLen / dstRate);
            double fundMag = Magnitude(buf[fundamentalBin]);

            double harmonicEnergy = 0;
            for (int h = 2; h * fundamentalBin < fftLen / 2; h++)
            {
                double m = Magnitude(buf[h * fundamentalBin]);
                harmonicEnergy += m * m;
            }
            double thd = Math.Sqrt(harmonicEnergy) / Math.Max(fundMag, 1e-30);
            return 20.0 * Math.Log10(Math.Max(thd, 1e-12));
        }

        // -------------- resampler adapters --------------

        private static float[] ResampleArDft(float[] input, int srcRate, int dstRate)
            => new ArDftResampler(srcRate, dstRate).Process(input);

        private static float[] ResampleWdlDefault(float[] input, int srcRate, int dstRate)
            => DriveWdlSampleProvider(input, srcRate, dstRate, configure: null);

        // High-quality WDL config: sinc interpolation with a 512-tap kernel. Closer match to
        // ARDFTSRC's quality target than the default linear+IIR.
        private static float[] ResampleWdlSinc512(float[] input, int srcRate, int dstRate)
            => DriveWdlSampleProvider(input, srcRate, dstRate, r =>
            {
                r.SetMode(true, 0, true, sinc_size: 512);
                r.SetFilterParms();
                r.SetFeedMode(false);
                r.SetRates(srcRate, dstRate);
            });

        private static float[] DriveWdlSampleProvider(
            float[] input, int srcRate, int dstRate, Action<WdlResampler> configure)
        {
            ISampleProvider source = new ArraySampleProvider(input, srcRate);
            ISampleProvider resampled;

            if (configure == null)
            {
                resampled = new WdlResamplingSampleProvider(source, dstRate);
            }
            else
            {
                // Reuse WdlResamplingSampleProvider's plumbing but override its config via reflection
                // would be ugly; build the same shape ourselves with the custom resampler config.
                resampled = new ConfiguredWdlSampleProvider(source, dstRate, configure);
            }

            long expected = (long)input.Length * dstRate / srcRate;
            var output = new float[expected];
            int written = 0;
            const int block = 4096;
            while (written < output.Length)
            {
                int n = resampled.Read(output.AsSpan(written, Math.Min(block, output.Length - written)));
                if (n == 0) break;
                written += n;
            }
            return output.AsSpan(0, written).ToArray();
        }

        // -------------- helpers --------------

        private static float[] MakeSine(int sampleRate, double freq, double durationSeconds, double amplitude)
        {
            int n = (int)(sampleRate * durationSeconds);
            var x = new float[n];
            double w = 2.0 * Math.PI * freq / sampleRate;
            for (int i = 0; i < n; i++) x[i] = (float)(amplitude * Math.Sin(w * i));
            return x;
        }

        private static double Magnitude(Complex c) => Math.Sqrt((double)c.X * c.X + (double)c.Y * c.Y);

        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { var t = b; b = a % b; a = t; }
            return a == 0 ? 1 : a;
        }

        // Minimal mono ISampleProvider that reads from a float[].
        private sealed class ArraySampleProvider : ISampleProvider
        {
            private readonly float[] data;
            private int position;
            public WaveFormat WaveFormat { get; }
            public ArraySampleProvider(float[] data, int sampleRate)
            {
                this.data = data;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            }
            public int Read(Span<float> buffer)
            {
                int avail = Math.Min(buffer.Length, data.Length - position);
                if (avail <= 0) return 0;
                data.AsSpan(position, avail).CopyTo(buffer);
                position += avail;
                return avail;
            }
        }

        // Mirrors WdlResamplingSampleProvider but lets the caller choose the WdlResampler config.
        private sealed class ConfiguredWdlSampleProvider : ISampleProvider
        {
            private readonly WdlResampler resampler;
            private readonly WaveFormat outFormat;
            private readonly ISampleProvider source;

            public ConfiguredWdlSampleProvider(ISampleProvider source, int newSampleRate, Action<WdlResampler> configure)
            {
                this.source = source;
                outFormat = WaveFormat.CreateIeeeFloatWaveFormat(newSampleRate, source.WaveFormat.Channels);
                resampler = new WdlResampler();
                configure(resampler);
            }

            public WaveFormat WaveFormat => outFormat;

            public int Read(Span<float> buffer)
            {
                int channels = outFormat.Channels;
                int framesRequested = buffer.Length / channels;
                int inNeeded = resampler.ResamplePrepare(framesRequested, channels, out Span<float> inBuffer);
                int inAvail = source.Read(inBuffer[..(inNeeded * channels)]) / channels;
                int outAvail = resampler.ResampleOut(buffer, inAvail, framesRequested, channels);
                return outAvail * channels;
            }
        }
    }
}
</content>
</invoke>