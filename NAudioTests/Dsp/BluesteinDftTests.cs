using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    [TestFixture]
    [Category("UnitTest")]
    public class BluesteinDftTests
    {
        // Tolerance picked for float32 chirp + radix-2 FFT round-tripping at sizes up to a few
        // thousand. Tighter than 1e-4 starts catching legitimate float rounding.
        private const float Tolerance = 1e-4f;

        // Cover edge cases (1, 2), small primes (3, 5, 7, 13), an even non-power-of-two (10),
        // and the canonical SRC sizes (147, 160, 320 for 44.1<->48 / 96->44.1).
        private static readonly int[] TestLengths = { 1, 2, 3, 4, 5, 7, 8, 10, 13, 16, 147, 160, 320 };

        [Test, TestCaseSource(nameof(TestLengths))]
        public void RoundTrip_RandomSignal_RecoversInput(int length)
        {
            var dft = new BluesteinDft(length);
            var original = MakeRandom(length, seed: 1234 ^ length);
            var data = (Complex[])original.Clone();

            dft.Forward(data);
            dft.Inverse(data);

            AssertClose(original, data, length * Tolerance);
        }

        [Test, TestCaseSource(nameof(TestLengths))]
        public void Forward_MatchesNaiveDft(int length)
        {
            var dft = new BluesteinDft(length);
            var input = MakeRandom(length, seed: 5678 ^ length);
            var data = (Complex[])input.Clone();
            var expected = NaiveForwardDft(input);

            dft.Forward(data);

            AssertClose(expected, data, length * Tolerance);
        }

        [Test, TestCaseSource(nameof(TestLengths))]
        public void Inverse_MatchesNaiveDft(int length)
        {
            var dft = new BluesteinDft(length);
            var input = MakeRandom(length, seed: 9012 ^ length);
            var data = (Complex[])input.Clone();
            var expected = NaiveInverseDft(input);

            dft.Inverse(data);

            AssertClose(expected, data, length * Tolerance);
        }

        // For power-of-two sizes Bluestein and the existing radix-2 FFT must agree exactly
        // (modulo float rounding). Anchors the convention bridge between the two engines.
        [TestCase(2, 1)]
        [TestCase(4, 2)]
        [TestCase(8, 3)]
        [TestCase(16, 4)]
        public void Forward_AgreesWithRadix2Fft_OnPowersOfTwo(int n, int log2)
        {
            var input = MakeRandom(n, seed: 4242);
            var bluestein = (Complex[])input.Clone();
            var radix2 = (Complex[])input.Clone();

            new BluesteinDft(n).Forward(bluestein);
            FastFourierTransform.FFT(true, log2, radix2);

            AssertClose(radix2, bluestein, n * Tolerance);
        }

        [Test]
        public void Forward_ConstantSignal_HasOnlyDcComponent()
        {
            const int n = 147;
            var data = new Complex[n];
            for (int i = 0; i < n; i++) data[i].X = 1.0f;

            new BluesteinDft(n).Forward(data);

            // 1/N scaling (NAudio convention) means DC bin = mean = 1, all others zero.
            Assert.That(data[0].X, Is.EqualTo(1.0f).Within(Tolerance));
            Assert.That(data[0].Y, Is.EqualTo(0.0f).Within(Tolerance));
            for (int i = 1; i < n; i++)
            {
                Assert.That(data[i].X, Is.EqualTo(0.0f).Within(Tolerance), $"bin {i} real");
                Assert.That(data[i].Y, Is.EqualTo(0.0f).Within(Tolerance), $"bin {i} imag");
            }
        }

        [Test]
        public void Forward_Impulse_HasFlatSpectrum()
        {
            const int n = 160;
            var data = new Complex[n];
            data[0].X = 1.0f;

            new BluesteinDft(n).Forward(data);

            float expected = 1.0f / n;
            for (int i = 0; i < n; i++)
            {
                Assert.That(data[i].X, Is.EqualTo(expected).Within(Tolerance), $"bin {i} real");
                Assert.That(data[i].Y, Is.EqualTo(0.0f).Within(Tolerance), $"bin {i} imag");
            }
        }

        [Test]
        public void Forward_IsLinear()
        {
            const int n = 73;
            var dft = new BluesteinDft(n);
            var x = MakeRandom(n, seed: 11);
            var y = MakeRandom(n, seed: 22);
            const float a = 0.7f, b = -1.3f;

            var combined = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                combined[i].X = a * x[i].X + b * y[i].X;
                combined[i].Y = a * x[i].Y + b * y[i].Y;
            }

            var fX = (Complex[])x.Clone(); dft.Forward(fX);
            var fY = (Complex[])y.Clone(); dft.Forward(fY);
            var fCombined = combined; dft.Forward(fCombined);

            for (int i = 0; i < n; i++)
            {
                float expectedX = a * fX[i].X + b * fY[i].X;
                float expectedY = a * fX[i].Y + b * fY[i].Y;
                Assert.That(fCombined[i].X, Is.EqualTo(expectedX).Within(n * Tolerance), $"bin {i} real");
                Assert.That(fCombined[i].Y, Is.EqualTo(expectedY).Within(n * Tolerance), $"bin {i} imag");
            }
        }

        [Test]
        public void Constructor_InvalidLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BluesteinDft(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BluesteinDft(-1));
        }

        [Test]
        public void Forward_WrongSpanLength_Throws()
        {
            var dft = new BluesteinDft(8);
            var data = new Complex[7];
            Assert.Throws<ArgumentException>(() => dft.Forward(data));
        }

        [Test]
        public void Inverse_WrongSpanLength_Throws()
        {
            var dft = new BluesteinDft(8);
            var data = new Complex[9];
            Assert.Throws<ArgumentException>(() => dft.Inverse(data));
        }

        // ------- helpers -------

        private static Complex[] MakeRandom(int length, int seed)
        {
            var rng = new Random(seed);
            var data = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                data[i].X = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i].Y = (float)(rng.NextDouble() * 2.0 - 1.0);
            }
            return data;
        }

        // Naive O(N^2) DFT in NAudio convention (forward scaled by 1/N).
        private static Complex[] NaiveForwardDft(Complex[] x)
        {
            int n = x.Length;
            var result = new Complex[n];
            double inv = 1.0 / n;
            for (int k = 0; k < n; k++)
            {
                double re = 0, im = 0;
                for (int j = 0; j < n; j++)
                {
                    double angle = -2.0 * Math.PI * k * j / n;
                    double c = Math.Cos(angle), s = Math.Sin(angle);
                    re += x[j].X * c - x[j].Y * s;
                    im += x[j].X * s + x[j].Y * c;
                }
                result[k].X = (float)(re * inv);
                result[k].Y = (float)(im * inv);
            }
            return result;
        }

        // Naive inverse DFT in NAudio convention (unscaled).
        private static Complex[] NaiveInverseDft(Complex[] x)
        {
            int n = x.Length;
            var result = new Complex[n];
            for (int k = 0; k < n; k++)
            {
                double re = 0, im = 0;
                for (int j = 0; j < n; j++)
                {
                    double angle = 2.0 * Math.PI * k * j / n;
                    double c = Math.Cos(angle), s = Math.Sin(angle);
                    re += x[j].X * c - x[j].Y * s;
                    im += x[j].X * s + x[j].Y * c;
                }
                result[k].X = (float)re;
                result[k].Y = (float)im;
            }
            return result;
        }

        private static void AssertClose(Complex[] expected, Complex[] actual, float tolerance)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].X, Is.EqualTo(expected[i].X).Within(tolerance), $"index {i} real");
                Assert.That(actual[i].Y, Is.EqualTo(expected[i].Y).Within(tolerance), $"index {i} imag");
            }
        }
    }
}
