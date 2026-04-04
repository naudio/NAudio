using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioTests.Utils;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudioTests.MediaFoundation
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class MediaFoundationResamplerTests
    {
        [SetUp]
        public void SetUp()
        {
            OSUtils.RequireVista();
            RequireMediaFoundationResampler();
        }

        [Test]
        public void ConstructorRejectsUnsupportedInputFormat()
        {
            var unsupportedInput = WaveFormat.CreateALawFormat(8000, 1);
            var source = new InMemoryWaveProvider(unsupportedInput, new byte[unsupportedInput.AverageBytesPerSecond / 10]);
            Assert.That(() => new MediaFoundationResampler(source, new WaveFormat(16000, 16, 1)),
                Throws.ArgumentException.With.Message.Contains("Input must be PCM or IEEE float"));
        }

        [Test]
        public void ConstructorRejectsUnsupportedOutputFormat()
        {
            var source = new InMemoryWaveProvider(new WaveFormat(16000, 16, 1), new byte[1600]);
            var unsupportedOutput = WaveFormat.CreateMuLawFormat(8000, 1);
            Assert.That(() => new MediaFoundationResampler(source, unsupportedOutput),
                Throws.ArgumentException.With.Message.Contains("Output must be PCM or IEEE float"));
        }

        [Test]
        public void ResamplerQualityHasValidRange()
        {
            using (var source = CreateSineWaveSource(44100, 1, 0.25, 400))
            using (var resampler = new MediaFoundationResampler(source, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)))
            {
                Assert.That(() => resampler.ResamplerQuality = 0, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => resampler.ResamplerQuality = 61, Throws.TypeOf<ArgumentOutOfRangeException>());

                resampler.ResamplerQuality = 1;
                Assert.That(resampler.ResamplerQuality, Is.EqualTo(1));
                resampler.ResamplerQuality = 60;
                Assert.That(resampler.ResamplerQuality, Is.EqualTo(60));
            }
        }

        [TestCase(44100, 48000)]
        [TestCase(48000, 44100)]
        [TestCase(16000, 44100)]
        [TestCase(44100, 16000)]
        [TestCase(96000, 22050)]
        [TestCase(22050, 96000)]
        public void ReadResamplesAndPreservesFrequency(int inputRate, int outputRate)
        {
            const double frequency = 1000;
            const double durationSeconds = 1.0;

            using (var source = CreateSineWaveSource(inputRate, 1, durationSeconds, frequency))
            using (var resampler = new MediaFoundationResampler(source, WaveFormat.CreateIeeeFloatWaveFormat(outputRate, 1)))
            {
                var outputBytes = ReadAllBytes(resampler, resampler.WaveFormat.AverageBytesPerSecond / 100);
                var outputSamples = BytesToFloatSamples(outputBytes);

                Assert.That(outputSamples.Length, Is.GreaterThan(outputRate / 2), "Expected substantial output samples");

                var estimatedFrequency = EstimateFrequencyByPositiveZeroCrossings(outputSamples, outputRate);
                Assert.That(estimatedFrequency, Is.InRange(frequency - 30, frequency + 30),
                    "Estimated frequency should remain near source frequency after resampling");
            }
        }

        [Test]
        public void ReadInSmallChunksEventuallyEndsWithZero()
        {
            var inputFormat = new WaveFormat(44100, 16, 1);
            var oneSecond = new byte[inputFormat.AverageBytesPerSecond];
            for (int n = 0; n < oneSecond.Length; n++)
            {
                oneSecond[n] = (byte)(n % 251);
            }

            using (var source = new InMemoryWaveProvider(inputFormat, oneSecond))
            using (var resampler = new MediaFoundationResampler(source, new WaveFormat(22050, 16, 1)))
            {
                var buffer = new byte[257]; // intentionally awkward size to exercise output buffering
                int totalRead = 0;
                int reads = 0;
                int bytesRead;
                do
                {
                    bytesRead = resampler.Read(buffer.AsSpan());
                    totalRead += bytesRead;
                    reads++;
                } while (bytesRead > 0 && reads < 20000);

                Assert.That(reads, Is.LessThan(20000), "Read loop should terminate");
                Assert.That(totalRead, Is.GreaterThan(0), "Should produce output before ending");
                Assert.That(resampler.Read(buffer.AsSpan()), Is.EqualTo(0), "Subsequent reads after EOF should return zero");
            }
        }

        [Test]
        public void RepositionAfterRewindingSourceRepeatsOutput()
        {
            using (var source = CreateSineWaveSource(44100, 1, 1.0, 700))
            using (var resampler = new MediaFoundationResampler(source, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)))
            {
                var first = new byte[4096];
                var second = new byte[4096];

                var firstRead = resampler.Read(first.AsSpan());
                Assert.That(firstRead, Is.GreaterThan(0));

                source.Position = 0;
                resampler.Reposition();

                var secondRead = resampler.Read(second.AsSpan());
                Assert.That(secondRead, Is.EqualTo(firstRead));
                Assert.That(second, Is.EqualTo(first), "After source rewind + reposition, initial output should repeat");
            }
        }

        [Test]
        public void ReadAfterDisposeThrows()
        {
            using (var source = CreateSineWaveSource(44100, 1, 0.25, 500))
            {
                var resampler = new MediaFoundationResampler(source, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
                resampler.Dispose();

                var buffer = new byte[1024];
                Assert.That(() => resampler.Read(buffer.AsSpan()), Throws.TypeOf<ObjectDisposedException>());
            }
        }

        private static RawSourceWaveStream CreateSineWaveSource(int sampleRate, int channels, double durationSeconds, double frequency)
        {
            var signal = new SignalGenerator(sampleRate, channels)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = frequency,
                Gain = 0.8
            };

            var sampleCount = (int)(sampleRate * channels * durationSeconds);
            var sampleBuffer = new float[sampleCount];
            var read = signal.Read(sampleBuffer.AsSpan());
            Assert.That(read, Is.EqualTo(sampleBuffer.Length));

            var bytes = new byte[sampleBuffer.Length * sizeof(float)];
            Buffer.BlockCopy(sampleBuffer, 0, bytes, 0, bytes.Length);
            return new RawSourceWaveStream(new MemoryStream(bytes), WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
        }

        private static byte[] ReadAllBytes(IWaveProvider source, int chunkSize)
        {
            var readBuffer = new byte[chunkSize];
            using (var output = new MemoryStream())
            {
                int bytesRead;
                while ((bytesRead = source.Read(readBuffer.AsSpan())) > 0)
                {
                    output.Write(readBuffer, 0, bytesRead);
                }
                return output.ToArray();
            }
        }

        private static float[] BytesToFloatSamples(byte[] bytes)
        {
            var samples = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, samples, 0, samples.Length * sizeof(float));
            return samples;
        }

        private static double EstimateFrequencyByPositiveZeroCrossings(float[] samples, int sampleRate)
        {
            if (samples.Length < 3)
            {
                return 0;
            }

            int start = samples.Length / 10;
            int end = samples.Length - start;
            int positiveCrossings = 0;
            for (int n = Math.Max(start + 1, 1); n < end; n++)
            {
                if (samples[n - 1] <= 0 && samples[n] > 0)
                {
                    positiveCrossings++;
                }
            }

            double analyzedSeconds = (end - start) / (double)sampleRate;
            if (analyzedSeconds <= 0)
            {
                return 0;
            }
            return positiveCrossings / analyzedSeconds;
        }

        private static void RequireMediaFoundationResampler()
        {
            var probeFormat = WaveFormat.CreateIeeeFloatWaveFormat(8000, 1);
            var probeBytes = new byte[probeFormat.AverageBytesPerSecond / 50];
            using (var source = new InMemoryWaveProvider(probeFormat, probeBytes))
            {
                try
                {
                    using (var resampler = new MediaFoundationResampler(source, WaveFormat.CreateIeeeFloatWaveFormat(8000, 1)))
                    {
                    }
                }
                catch (COMException ex)
                {
                    Assert.Ignore("Media Foundation resampler unavailable: " + ex.Message);
                }
                catch (TypeInitializationException ex)
                {
                    Assert.Ignore("Media Foundation initialization failed: " + ex.Message);
                }
                catch (DllNotFoundException ex)
                {
                    Assert.Ignore("Media Foundation runtime missing: " + ex.Message);
                }
            }
        }

        private class InMemoryWaveProvider : IWaveProvider, IDisposable
        {
            private readonly RawSourceWaveStream stream;

            public InMemoryWaveProvider(WaveFormat waveFormat, byte[] bytes)
            {
                stream = new RawSourceWaveStream(new MemoryStream(bytes), waveFormat);
            }

            public WaveFormat WaveFormat => stream.WaveFormat;

            public long Position
            {
                get { return stream.Position; }
                set { stream.Position = value; }
            }

            public int Read(Span<byte> buffer)
            {
                return stream.Read(buffer);
            }

            public void Dispose()
            {
                stream.Dispose();
            }
        }
    }
}
