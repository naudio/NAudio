using System;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// Tests validating correctness of the Span-first IWaveProvider / ISampleProvider transition.
    /// Covers PCM round-trips, cross-boundary reads, buffer-size variation, and the SampleChannel pipeline.
    /// </summary>
    [TestFixture]
    public class SpanTransitionTests
    {
        #region PCM format conversion round-trips

        [Test]
        public void RoundTrip16Bit_PreservesSignal()
        {
            // float -> 16-bit PCM -> float should preserve signal within quantization error
            var signal = CreateKnownSignal(1000, 44100, 1);
            var sampleToWave = new SampleToWaveProvider16(signal);
            var waveToSample = new Pcm16BitToSampleProvider(sampleToWave);

            var output = new float[1000];
            int read = waveToSample.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(1000));
            for (int i = 0; i < read; i++)
            {
                // 16-bit uses asymmetric scale (encode *32767, decode /32768) so tolerance ~2/32768
                Assert.That(output[i], Is.EqualTo(signal.Samples[i]).Within(2.0f / 32768f),
                    $"Sample {i} round-trip mismatch");
            }
        }

        [Test]
        public void RoundTrip24Bit_PreservesSignal()
        {
            var signal = CreateKnownSignal(1000, 44100, 1);
            var sampleToWave = new SampleToWaveProvider24(signal);
            var waveToSample = new Pcm24BitToSampleProvider(sampleToWave);

            var output = new float[1000];
            int read = waveToSample.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(1000));
            for (int i = 0; i < read; i++)
            {
                // 24-bit uses asymmetric scale (encode *8388607, decode /8388608) so tolerance ~2/8388608
                Assert.That(output[i], Is.EqualTo(signal.Samples[i]).Within(2.0f / 8388608f),
                    $"Sample {i} round-trip mismatch");
            }
        }

        [Test]
        public void RoundTrip16BitStereo_PreservesChannelLayout()
        {
            // Stereo signal: L=0.5, R=-0.5
            var signal = new ConstChannelSampleProvider(44100, 2, 500,
                new[] { 0.5f, -0.5f });
            var sampleToWave = new SampleToWaveProvider16(signal);
            var waveToSample = new Pcm16BitToSampleProvider(sampleToWave);

            var output = new float[1000]; // 500 stereo sample pairs
            int read = waveToSample.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(1000));
            for (int i = 0; i < read; i += 2)
            {
                Assert.That(output[i], Is.EqualTo(0.5f).Within(1.0f / 32768f), $"Left sample at {i}");
                Assert.That(output[i + 1], Is.EqualTo(-0.5f).Within(1.0f / 32768f), $"Right sample at {i + 1}");
            }
        }

        [Test]
        public void RoundTrip16Bit_ClipsAboveOne()
        {
            var signal = new ConstChannelSampleProvider(44100, 1, 10,
                new[] { 1.5f });
            var sampleToWave = new SampleToWaveProvider16(signal);
            var waveToSample = new Pcm16BitToSampleProvider(sampleToWave);

            var output = new float[10];
            waveToSample.Read(output.AsSpan());

            for (int i = 0; i < 10; i++)
            {
                // Clipped to 1.0 then quantized
                Assert.That(output[i], Is.EqualTo(1.0f).Within(1.0f / 32768f));
            }
        }

        #endregion

        #region ConcatenatingSampleProvider boundary crossing

        [Test]
        public void Concatenation_SingleReadSpansBoundary()
        {
            // Two providers of 30 samples each, read with buffer of 60
            // This forces the Span slicing logic to read from both in one call
            var p1 = new TestSampleProvider(44100, 1, 30);
            p1.UseConstValue = true; p1.ConstValue = 1;
            var p2 = new TestSampleProvider(44100, 1, 30);
            p2.UseConstValue = true; p2.ConstValue = 2;

            var concat = new ConcatenatingSampleProvider(new[] { p1, p2 });
            var buffer = new float[60];
            int read = concat.Read(buffer.AsSpan());

            Assert.That(read, Is.EqualTo(60));
            // First 30 from p1
            for (int i = 0; i < 30; i++)
                Assert.That(buffer[i], Is.EqualTo(1f), $"Index {i} should be from provider 1");
            // Next 30 from p2
            for (int i = 30; i < 60; i++)
                Assert.That(buffer[i], Is.EqualTo(2f), $"Index {i} should be from provider 2");
        }

        [Test]
        public void Concatenation_SmallReadsAcrossBoundary()
        {
            var p1 = new TestSampleProvider(44100, 1, 10);
            p1.UseConstValue = true; p1.ConstValue = 1;
            var p2 = new TestSampleProvider(44100, 1, 10);
            p2.UseConstValue = true; p2.ConstValue = 2;

            var concat = new ConcatenatingSampleProvider(new[] { p1, p2 });

            // Read 7 at a time across the boundary
            var buffer = new float[7];
            int r1 = concat.Read(buffer.AsSpan()); // reads 7 from p1
            Assert.That(r1, Is.EqualTo(7));
            Assert.That(buffer.Take(7).All(v => v == 1f));

            int r2 = concat.Read(buffer.AsSpan()); // reads 3 from p1, 4 from p2
            Assert.That(r2, Is.EqualTo(7));
            Assert.That(buffer[0], Is.EqualTo(1f)); // last 3 from p1
            Assert.That(buffer[1], Is.EqualTo(1f));
            Assert.That(buffer[2], Is.EqualTo(1f));
            Assert.That(buffer[3], Is.EqualTo(2f)); // first 4 from p2
            Assert.That(buffer[6], Is.EqualTo(2f));

            int r3 = concat.Read(buffer.AsSpan()); // reads remaining 6 from p2
            Assert.That(r3, Is.EqualTo(6));
        }

        #endregion

        #region MixingSampleProvider with varying buffer sizes

        [Test]
        public void Mixing_VaryingBufferSizes_ProducesConsistentOutput()
        {
            // Mix two constant sources and read with different buffer sizes
            var s1 = new TestSampleProvider(44100, 1, 1000);
            s1.UseConstValue = true; s1.ConstValue = 3;
            var s2 = new TestSampleProvider(44100, 1, 1000);
            s2.UseConstValue = true; s2.ConstValue = 5;

            var mixer = new MixingSampleProvider(new[] { s1, s2 });

            // Read with varying sizes in a cycle until done
            int[] readSizes = { 7, 13, 100, 1, 50, 29 };
            int totalRead = 0;
            int sizeIndex = 0;
            while (totalRead < 1000)
            {
                int size = readSizes[sizeIndex % readSizes.Length];
                sizeIndex++;
                var buffer = new float[size];
                int read = mixer.Read(buffer.AsSpan());
                if (read == 0) break;
                totalRead += read;
                for (int i = 0; i < read; i++)
                {
                    Assert.That(buffer[i], Is.EqualTo(8f), // 3 + 5
                        $"Mixed value at offset {totalRead - read + i} (read size {size})");
                }
            }
            Assert.That(totalRead, Is.EqualTo(1000));
        }

        [Test]
        public void Mixing_ThreeSources_DifferentLengths()
        {
            var s1 = new TestSampleProvider(44100, 1, 100);
            s1.UseConstValue = true; s1.ConstValue = 1;
            var s2 = new TestSampleProvider(44100, 1, 50);
            s2.UseConstValue = true; s2.ConstValue = 2;
            var s3 = new TestSampleProvider(44100, 1, 75);
            s3.UseConstValue = true; s3.ConstValue = 4;

            var mixer = new MixingSampleProvider(new[] { s1, s2, s3 });

            // First 50 samples: all three contribute = 1+2+4 = 7
            var buf = new float[50];
            Assert.That(mixer.Read(buf.AsSpan()), Is.EqualTo(50));
            Assert.That(buf[0], Is.EqualTo(7f));
            Assert.That(buf[49], Is.EqualTo(7f));

            // Next 25: s1 + s3 = 1+4 = 5
            buf = new float[25];
            Assert.That(mixer.Read(buf.AsSpan()), Is.EqualTo(25));
            Assert.That(buf[0], Is.EqualTo(5f));

            // Next 25: s1 only = 1
            buf = new float[25];
            Assert.That(mixer.Read(buf.AsSpan()), Is.EqualTo(25));
            Assert.That(buf[0], Is.EqualTo(1f));

            // Done
            buf = new float[100];
            Assert.That(mixer.Read(buf.AsSpan()), Is.EqualTo(0));
        }

        #endregion

        #region SampleChannel pipeline

        [Test]
        public void SampleChannel_16BitMono_ProducesSamples()
        {
            // Create a 16-bit PCM wave provider with known data
            var format = new WaveFormat(44100, 16, 1);
            var pcmData = new byte[200]; // 100 16-bit samples
            // Write a known 16-bit sample value (16384 = 0.5 * 32768)
            for (int i = 0; i < pcmData.Length; i += 2)
            {
                short val = 16384;
                pcmData[i] = (byte)(val & 0xFF);
                pcmData[i + 1] = (byte)(val >> 8);
            }
            var waveProvider = new BufferedWaveProvider(format);
            waveProvider.ReadFully = false;
            waveProvider.AddSamples(pcmData, 0, pcmData.Length);

            var channel = new SampleChannel(waveProvider);

            var output = new float[100];
            int read = channel.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(100));
            for (int i = 0; i < read; i++)
            {
                // 16384 / 32768 = 0.5
                Assert.That(output[i], Is.EqualTo(0.5f).Within(0.001f),
                    $"Sample {i}");
            }
        }

        [Test]
        public void SampleChannel_VolumeAffectsOutput()
        {
            var format = new WaveFormat(44100, 16, 1);
            var pcmData = new byte[200];
            for (int i = 0; i < pcmData.Length; i += 2)
            {
                short val = 16384; // 0.5
                pcmData[i] = (byte)(val & 0xFF);
                pcmData[i + 1] = (byte)(val >> 8);
            }
            var waveProvider = new BufferedWaveProvider(format);
            waveProvider.ReadFully = false;
            waveProvider.AddSamples(pcmData, 0, pcmData.Length);

            var channel = new SampleChannel(waveProvider);
            channel.Volume = 0.5f; // half volume

            var output = new float[100];
            int read = channel.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(100));
            for (int i = 0; i < read; i++)
            {
                // 0.5 * 0.5 = 0.25
                Assert.That(output[i], Is.EqualTo(0.25f).Within(0.001f),
                    $"Sample {i}");
            }
        }

        [Test]
        public void SampleChannel_MonoForcedToStereo()
        {
            var format = new WaveFormat(44100, 16, 1);
            var pcmData = new byte[20]; // 10 mono samples
            for (int i = 0; i < pcmData.Length; i += 2)
            {
                short val = 16384;
                pcmData[i] = (byte)(val & 0xFF);
                pcmData[i + 1] = (byte)(val >> 8);
            }
            var waveProvider = new BufferedWaveProvider(format);
            waveProvider.ReadFully = false;
            waveProvider.AddSamples(pcmData, 0, pcmData.Length);

            var channel = new SampleChannel(waveProvider, forceStereo: true);

            Assert.That(channel.WaveFormat.Channels, Is.EqualTo(2));

            var output = new float[20]; // 10 stereo pairs
            int read = channel.Read(output.AsSpan());

            Assert.That(read, Is.EqualTo(20));
            for (int i = 0; i < read; i++)
            {
                Assert.That(output[i], Is.EqualTo(0.5f).Within(0.001f),
                    $"Stereo sample {i}");
            }
        }

        #endregion

        #region BufferedWaveProvider circular buffer wrap-around

        [Test]
        public void BufferedWaveProvider_WrapAround_DataIntegrity()
        {
            // Use a small buffer that will wrap
            var format = new WaveFormat(44100, 16, 2);
            var bwp = new BufferedWaveProvider(format, TimeSpan.FromMilliseconds(10));
            bwp.ReadFully = false;
            int bufferSize = bwp.BufferLength;

            // Fill most of the buffer
            int firstWrite = bufferSize - 100;
            var data1 = Enumerable.Range(0, firstWrite).Select(n => (byte)(n % 256)).ToArray();
            bwp.AddSamples(data1, 0, data1.Length);

            // Read it all out (advances the read pointer near the end)
            var readBuf = new byte[firstWrite];
            int read1 = bwp.Read(readBuf.AsSpan());
            Assert.That(read1, Is.EqualTo(firstWrite));
            Assert.That(readBuf, Is.EqualTo(data1));

            // Now write data that wraps around the circular buffer
            int wrapWrite = 200; // crosses the end of the internal buffer
            var data2 = Enumerable.Range(0, wrapWrite).Select(n => (byte)((n + 50) % 256)).ToArray();
            bwp.AddSamples(data2, 0, data2.Length);

            // Read it back - this exercises the wrap-around read path
            readBuf = new byte[wrapWrite];
            int read2 = bwp.Read(readBuf.AsSpan());
            Assert.That(read2, Is.EqualTo(wrapWrite));
            Assert.That(readBuf, Is.EqualTo(data2), "Data corrupted across circular buffer wrap");
        }

        #endregion

        #region Span edge cases

        [Test]
        public void ZeroLengthSpanRead_ReturnsZero()
        {
            var provider = new TestSampleProvider(44100, 1, 100);
            var buffer = new float[0];
            Assert.That(provider.Read(buffer.AsSpan()), Is.EqualTo(0));
        }

        [Test]
        public void SampleToWaveProvider_ZeroLengthSpan()
        {
            var signal = new TestSampleProvider(44100, 1, 100);
            signal.UseConstValue = true; signal.ConstValue = 1;
            var stwp = new SampleToWaveProvider(signal);
            var buffer = new byte[0];
            Assert.That(stwp.Read(buffer.AsSpan()), Is.EqualTo(0));
        }

        [Test]
        public void PartialSpanSlice_ReadsCorrectSubset()
        {
            var signal = new TestSampleProvider(44100, 1, 100);
            signal.UseConstValue = true; signal.ConstValue = 42;

            // Allocate larger buffer, pass a slice
            var buffer = new float[100];
            int read = signal.Read(buffer.AsSpan(10, 20));
            Assert.That(read, Is.EqualTo(20));
            // The slice should be filled
            Assert.That(buffer[10], Is.EqualTo(42f));
            Assert.That(buffer[29], Is.EqualTo(42f));
            // Outside the slice should be untouched
            Assert.That(buffer[0], Is.EqualTo(0f));
            Assert.That(buffer[30], Is.EqualTo(0f));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Creates an ISampleProvider that produces a known sine-like signal with values in [-1, 1]
        /// </summary>
        private static KnownSignalSampleProvider CreateKnownSignal(int sampleCount, int sampleRate, int channels)
        {
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                // Use a sine wave that stays well within [-1, 1] to avoid clipping
                samples[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / sampleRate) * 0.9f;
            }
            return new KnownSignalSampleProvider(sampleRate, channels, samples);
        }

        /// <summary>
        /// Sample provider backed by a known array of samples
        /// </summary>
        private class KnownSignalSampleProvider : ISampleProvider
        {
            public float[] Samples { get; }
            private int position;

            public KnownSignalSampleProvider(int sampleRate, int channels, float[] samples)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                Samples = samples;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(Span<float> buffer)
            {
                int toCopy = Math.Min(buffer.Length, Samples.Length - position);
                Samples.AsSpan(position, toCopy).CopyTo(buffer);
                position += toCopy;
                return toCopy;
            }
        }

        /// <summary>
        /// Sample provider that repeats a per-channel constant pattern
        /// </summary>
        private class ConstChannelSampleProvider : ISampleProvider
        {
            private readonly float[] channelValues;
            private readonly int totalSamples;
            private int position;

            public ConstChannelSampleProvider(int sampleRate, int channels, int framesCount, float[] channelValues)
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
                this.channelValues = channelValues;
                this.totalSamples = framesCount * channels;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(Span<float> buffer)
            {
                int toCopy = Math.Min(buffer.Length, totalSamples - position);
                for (int i = 0; i < toCopy; i++)
                {
                    buffer[i] = channelValues[(position + i) % channelValues.Length];
                }
                position += toCopy;
                return toCopy;
            }
        }

        #endregion
    }
}
