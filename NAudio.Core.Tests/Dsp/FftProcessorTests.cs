using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    /// <summary>
    /// Verifies that <see cref="FftProcessor"/> produces correct and consistent output against
    /// both known analytical signals and the existing static <see cref="FastFourierTransform"/>
    /// implementation.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class FftProcessorTests
    {
        private const double Tolerance = 1e-5;

        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(64)]
        [TestCase(1024)]
        public void RealForwardMatchesFullComplexFftOnRealInput(int n)
        {
            // Pack real samples (Y=0) into a full Complex[] and run the static full-size FFT;
            // compare the first N/2+1 bins to RealForward's output — they should match exactly
            // (both use the same 1/N forward scaling convention).
            var rng = new Random(1337);
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = (float)(rng.NextDouble() * 2 - 1);

            var fullBuffer = new Complex[n];
            for (int i = 0; i < n; i++) { fullBuffer[i].X = samples[i]; fullBuffer[i].Y = 0f; }
            int m = Log2(n);
            FastFourierTransform.FFT(true, m, fullBuffer);

            var processor = new FftProcessor(n);
            var half = new Complex[n / 2 + 1];
            processor.RealForward(samples, half);

            // Accumulated float32 rounding differs slightly between the full-complex FFT path
            // (N-point with twiddle recurrence) and the real-FFT path (N/2-point + unpack). Allow
            // a proportional tolerance that scales with FFT size.
            double tol = Tolerance * Math.Max(1, n / 64);
            for (int k = 0; k <= n / 2; k++)
            {
                Assert.That(half[k].X, Is.EqualTo(fullBuffer[k].X).Within(tol),
                    $"bin {k} real part diverges from the full complex FFT");
                Assert.That(half[k].Y, Is.EqualTo(fullBuffer[k].Y).Within(tol),
                    $"bin {k} imaginary part diverges from the full complex FFT");
            }
        }

        [Test]
        public void RealForwardOnConstantSignalProducesDcOnly()
        {
            const int n = 16;
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = 1.0f;

            var processor = new FftProcessor(n);
            var spectrum = new Complex[n / 2 + 1];
            processor.RealForward(samples, spectrum);

            Assert.That(spectrum[0].Real, Is.EqualTo(1.0f).Within(Tolerance),
                "DC bin of a constant-1 signal should read 1.0 under 1/N scaling");
            for (int k = 1; k <= n / 2; k++)
            {
                Assert.That(spectrum[k].Real, Is.EqualTo(0.0f).Within(Tolerance));
                Assert.That(spectrum[k].Imaginary, Is.EqualTo(0.0f).Within(Tolerance));
            }
        }

        [Test]
        public void RealForwardOnImpulseProducesFlatSpectrum()
        {
            const int n = 16;
            var samples = new float[n];
            samples[0] = 1.0f;

            var processor = new FftProcessor(n);
            var spectrum = new Complex[n / 2 + 1];
            processor.RealForward(samples, spectrum);

            float expected = 1.0f / n;
            for (int k = 0; k <= n / 2; k++)
            {
                Assert.That(spectrum[k].Real, Is.EqualTo(expected).Within(Tolerance),
                    $"impulse FFT should be flat at 1/N; bin {k} differs");
                Assert.That(spectrum[k].Imaginary, Is.EqualTo(0.0f).Within(Tolerance));
            }
        }

        [TestCase(3, 16)]
        [TestCase(5, 32)]
        [TestCase(7, 64)]
        public void RealForwardOnCosineHasEnergyAtBin(int bin, int n)
        {
            var samples = new float[n];
            for (int i = 0; i < n; i++)
                samples[i] = (float)Math.Cos(2 * Math.PI * bin * i / n);

            var processor = new FftProcessor(n);
            var spectrum = new Complex[n / 2 + 1];
            processor.RealForward(samples, spectrum);

            // Real cosine has energy split evenly between +bin and -bin; the real-half-spectrum
            // sums both halves (conjugate symmetry) into magnitude 0.5 at the target bin.
            for (int k = 0; k <= n / 2; k++)
            {
                float magnitude = (float)Math.Sqrt(spectrum[k].Real * spectrum[k].Real + spectrum[k].Imaginary * spectrum[k].Imaginary);
                if (k == bin)
                    Assert.That(magnitude, Is.EqualTo(0.5f).Within(Tolerance), $"bin {k} should have magnitude 0.5");
                else
                    Assert.That(magnitude, Is.EqualTo(0.0f).Within(Tolerance), $"bin {k} should be near zero");
            }
        }

        [TestCase(8)]
        [TestCase(64)]
        [TestCase(1024)]
        public void RealForwardInverseRoundTrips(int n)
        {
            var rng = new Random(7);
            var input = new float[n];
            for (int i = 0; i < n; i++) input[i] = (float)(rng.NextDouble() * 2 - 1);

            var processor = new FftProcessor(n);
            var spectrum = new Complex[n / 2 + 1];
            processor.RealForward(input, spectrum);

            var recovered = new float[n];
            processor.RealInverse(spectrum, recovered);

            for (int i = 0; i < n; i++)
                Assert.That(recovered[i], Is.EqualTo(input[i]).Within(1e-4f),
                    $"sample {i} did not round-trip");
        }

        [Test]
        public void ComplexForwardMatchesStaticFftAtSameSize()
        {
            const int n = 64;
            int m = Log2(n);
            var rng = new Random(42);
            var a = new Complex[n];
            var b = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                a[i].X = (float)rng.NextDouble();
                a[i].Y = (float)rng.NextDouble();
                b[i] = a[i];
            }

            FastFourierTransform.FFT(true, m, a);
            new FftProcessor(n).ComplexForward(b);

            for (int i = 0; i < n; i++)
            {
                Assert.That(b[i].X, Is.EqualTo(a[i].X).Within(Tolerance));
                Assert.That(b[i].Y, Is.EqualTo(a[i].Y).Within(Tolerance));
            }
        }

        [Test]
        public void HammingWindowTableMatchesStaticFunction()
        {
            // When a window is configured, RealForward should apply it identically to how a caller
            // would using the static window function on each sample. Verified indirectly: compare
            // the windowed output to a manually-windowed unwindowed call.
            const int n = 64;
            var rng = new Random(99);
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = (float)(rng.NextDouble() * 2 - 1);

            var windowedSamples = new float[n];
            for (int i = 0; i < n; i++)
                windowedSamples[i] = samples[i] * (float)FastFourierTransform.HammingWindow(i, n);

            var noWindow = new FftProcessor(n);
            var withWindow = new FftProcessor(n, FftWindowType.Hamming);

            var expectedSpectrum = new Complex[n / 2 + 1];
            noWindow.RealForward(windowedSamples, expectedSpectrum);

            var actualSpectrum = new Complex[n / 2 + 1];
            withWindow.RealForward(samples, actualSpectrum);

            for (int k = 0; k <= n / 2; k++)
            {
                Assert.That(actualSpectrum[k].X, Is.EqualTo(expectedSpectrum[k].X).Within(Tolerance),
                    $"bin {k} real part differs between built-in window and manual windowing");
                Assert.That(actualSpectrum[k].Y, Is.EqualTo(expectedSpectrum[k].Y).Within(Tolerance));
            }
        }

        [Test]
        public void NonPowerOfTwoSizeThrows()
        {
            Assert.Throws<ArgumentException>(() => new FftProcessor(7));
            Assert.Throws<ArgumentException>(() => new FftProcessor(1000));
        }

        [Test]
        public void SizeLessThanTwoThrows()
        {
            Assert.Throws<ArgumentException>(() => new FftProcessor(1));
            Assert.Throws<ArgumentException>(() => new FftProcessor(0));
        }

        [Test]
        public void WrongSizedSpansThrow()
        {
            var processor = new FftProcessor(16);
            var shortSamples = new float[8];
            var rightSamples = new float[16];
            var shortSpectrum = new Complex[5];
            var rightSpectrum = new Complex[9];

            Assert.Throws<ArgumentException>(() => processor.RealForward(shortSamples, rightSpectrum));
            Assert.Throws<ArgumentException>(() => processor.RealForward(rightSamples, shortSpectrum));
            Assert.Throws<ArgumentException>(() => processor.RealInverse(shortSpectrum, rightSamples));
            Assert.Throws<ArgumentException>(() => processor.RealInverse(rightSpectrum, shortSamples));
        }

        [Test]
        public void RealForwardDoesNotAllocateInSteadyState()
        {
            const int n = 1024;
            var processor = new FftProcessor(n, FftWindowType.Hamming);
            var samples = new float[n];
            var spectrum = new Complex[n / 2 + 1];
            var rng = new Random(0);
            for (int i = 0; i < n; i++) samples[i] = (float)rng.NextDouble();

            // Warm up
            processor.RealForward(samples, spectrum);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) processor.RealForward(samples, spectrum);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0),
                "RealForward must not allocate on the steady-state path — window and twiddle tables are precomputed in the ctor");
        }

        private static int Log2(int value)
        {
            int log = 0;
            while (value > 1) { value >>= 1; log++; }
            return log;
        }
    }
}
