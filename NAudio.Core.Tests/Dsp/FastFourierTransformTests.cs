using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    [TestFixture]
    [Category("UnitTest")]
    public class FastFourierTransformTests
    {
        private const double Tolerance = 1e-5;

        [Test]
        public void ForwardFft_ConstantSignal_HasOnlyDcComponent()
        {
            const int m = 3;
            const int n = 1 << m;
            var data = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                data[i].X = 1.0f;
            }

            FastFourierTransform.FFT(true, m, data);

            Assert.Multiple(() =>
            {
                Assert.That(data[0].X, Is.EqualTo(1.0f).Within(Tolerance));
                Assert.That(data[0].Y, Is.EqualTo(0.0f).Within(Tolerance));

                for (int i = 1; i < n; i++)
                {
                    Assert.That(data[i].X, Is.EqualTo(0.0f).Within(Tolerance));
                    Assert.That(data[i].Y, Is.EqualTo(0.0f).Within(Tolerance));
                }
            });
        }

        [Test]
        public void ForwardFft_ImpulseSignal_HasFlatSpectrum()
        {
            const int m = 3;
            const int n = 1 << m;
            var data = new Complex[n];
            data[0].X = 1.0f;

            FastFourierTransform.FFT(true, m, data);

            var expected = 1.0 / n;
            Assert.Multiple(() =>
            {
                for (int i = 0; i < n; i++)
                {
                    Assert.That(data[i].X, Is.EqualTo(expected).Within(Tolerance));
                    Assert.That(data[i].Y, Is.EqualTo(0.0f).Within(Tolerance));
                }
            });
        }

        [Test]
        public void ForwardFft_CosineSignal_HasEnergyAtPositiveAndNegativeBin()
        {
            const int m = 4;
            const int n = 1 << m;
            const int frequencyBin = 3;
            var data = new Complex[n];

            for (int i = 0; i < n; i++)
            {
                data[i].X = (float)Math.Cos((2.0 * Math.PI * frequencyBin * i) / n);
            }

            FastFourierTransform.FFT(true, m, data);

            Assert.Multiple(() =>
            {
                Assert.That(data[frequencyBin].X, Is.EqualTo(0.5f).Within(1e-4));
                Assert.That(data[frequencyBin].Y, Is.EqualTo(0.0f).Within(1e-4));

                int mirroredBin = n - frequencyBin;
                Assert.That(data[mirroredBin].X, Is.EqualTo(0.5f).Within(1e-4));
                Assert.That(data[mirroredBin].Y, Is.EqualTo(0.0f).Within(1e-4));

                for (int i = 0; i < n; i++)
                {
                    if (i == frequencyBin || i == mirroredBin)
                    {
                        continue;
                    }

                    Assert.That(data[i].X, Is.EqualTo(0.0f).Within(1e-4));
                    Assert.That(data[i].Y, Is.EqualTo(0.0f).Within(1e-4));
                }
            });
        }

        [Test]
        public void ForwardThenInverseFft_RoundTripsComplexData()
        {
            const int m = 4;
            const int n = 1 << m;
            var random = new Random(12345);
            var data = new Complex[n];
            var original = new Complex[n];

            for (int i = 0; i < n; i++)
            {
                data[i].X = (float)(random.NextDouble() * 2.0 - 1.0);
                data[i].Y = (float)(random.NextDouble() * 2.0 - 1.0);
                original[i] = data[i];
            }

            FastFourierTransform.FFT(true, m, data);
            FastFourierTransform.FFT(false, m, data);

            Assert.Multiple(() =>
            {
                for (int i = 0; i < n; i++)
                {
                    Assert.That(data[i].X, Is.EqualTo(original[i].X).Within(1e-4));
                    Assert.That(data[i].Y, Is.EqualTo(original[i].Y).Within(1e-4));
                }
            });
        }

        [Test]
        public void Fft_SizeOne_DoesNotChangeValue()
        {
            var data = new[] { new Complex { X = 0.125f, Y = -0.75f } };

            FastFourierTransform.FFT(true, 0, data);
            FastFourierTransform.FFT(false, 0, data);

            Assert.Multiple(() =>
            {
                Assert.That(data[0].X, Is.EqualTo(0.125f).Within(Tolerance));
                Assert.That(data[0].Y, Is.EqualTo(-0.75f).Within(Tolerance));
            });
        }

        [Test]
        public void HannWindow_HasExpectedEndpointsAndSymmetry()
        {
            const int frameSize = 1024;

            Assert.Multiple(() =>
            {
                Assert.That(FastFourierTransform.HannWindow(0, frameSize), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(FastFourierTransform.HannWindow(frameSize - 1, frameSize), Is.EqualTo(0.0).Within(1e-12));

                for (int i = 0; i < 50; i++)
                {
                    Assert.That(
                        FastFourierTransform.HannWindow(i, frameSize),
                        Is.EqualTo(FastFourierTransform.HannWindow(frameSize - 1 - i, frameSize)).Within(1e-12));
                }
            });
        }

        [Test]
        public void HammingWindow_HasExpectedEndpointsAndSymmetry()
        {
            const int frameSize = 1024;

            Assert.Multiple(() =>
            {
                Assert.That(FastFourierTransform.HammingWindow(0, frameSize), Is.EqualTo(0.08).Within(1e-12));
                Assert.That(FastFourierTransform.HammingWindow(frameSize - 1, frameSize), Is.EqualTo(0.08).Within(1e-12));

                for (int i = 0; i < 50; i++)
                {
                    Assert.That(
                        FastFourierTransform.HammingWindow(i, frameSize),
                        Is.EqualTo(FastFourierTransform.HammingWindow(frameSize - 1 - i, frameSize)).Within(1e-12));
                }
            });
        }

        [Test]
        public void BlackmanHarrisWindow_HasExpectedEndpointsAndSymmetry()
        {
            const int frameSize = 1024;

            Assert.Multiple(() =>
            {
                Assert.That(FastFourierTransform.BlackmanHarrisWindow(0, frameSize), Is.EqualTo(0.00006).Within(1e-8));
                Assert.That(FastFourierTransform.BlackmanHarrisWindow(frameSize - 1, frameSize), Is.EqualTo(0.00006).Within(1e-8));

                for (int i = 0; i < 50; i++)
                {
                    Assert.That(
                        FastFourierTransform.BlackmanHarrisWindow(i, frameSize),
                        Is.EqualTo(FastFourierTransform.BlackmanHarrisWindow(frameSize - 1 - i, frameSize)).Within(1e-12));
                }
            });
        }

        [Test]
        public void WindowFunctions_AreOneAtCenterForOddSizedFrame()
        {
            const int frameSize = 5;
            const int center = 2;

            Assert.Multiple(() =>
            {
                Assert.That(FastFourierTransform.HannWindow(center, frameSize), Is.EqualTo(1.0).Within(1e-12));
                Assert.That(FastFourierTransform.HammingWindow(center, frameSize), Is.EqualTo(1.0).Within(1e-12));
                Assert.That(FastFourierTransform.BlackmanHarrisWindow(center, frameSize), Is.EqualTo(1.0).Within(1e-12));
            });
        }
    }
}
