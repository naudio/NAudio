using System;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NUnit.Framework;

namespace NAudioTests.WaveStreams
{
    /// <summary>
    /// Lightweight in-test throughput check for <see cref="MixingSampleProvider"/>. Not a precise
    /// benchmark (use <c>NAudio.Benchmarks</c> for that) — just a floor the CI can assert against
    /// so someone regressing the mix-add kernel back to scalar would have to justify it.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class MixingSampleProviderThroughputTests
    {
        /// <summary>Endless sample source for throughput tests — no file I/O, no allocation per Read.</summary>
        private sealed class CyclingSource : ISampleProvider
        {
            private readonly float[] data;
            private int position;

            public CyclingSource(float[] data, WaveFormat format)
            {
                this.data = data;
                WaveFormat = format;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(Span<float> buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = data[position];
                    position = (position + 1) % data.Length;
                }
                return buffer.Length;
            }
        }

        [Test]
        public void MixingSampleProvider_EightSources_ExceedsRealtimeFloor()
        {
            const int sampleRate = 48000;
            const int channels = 2;
            const int bufferFrames = 4800; // 100 ms stereo @ 48k
            const int iterations = 500;

            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            var sources = new ISampleProvider[8];
            var rng = new Random(1337);
            for (int i = 0; i < sources.Length; i++)
            {
                var data = new float[4096];
                for (int k = 0; k < data.Length; k++) data[k] = (float)(rng.NextDouble() * 2 - 1);
                sources[i] = new CyclingSource(data, format);
            }
            var mixer = new MixingSampleProvider(sources);
            var buffer = new float[bufferFrames * channels];

            // warm up JIT
            for (int i = 0; i < 10; i++) mixer.Read(buffer);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) mixer.Read(buffer);
            sw.Stop();

            double audioSeconds = (double)iterations * bufferFrames / sampleRate;
            double realtimeFactor = audioSeconds / sw.Elapsed.TotalSeconds;
            TestContext.WriteLine($"Mixed 8 sources × 100ms @ 48kHz stereo: {realtimeFactor:F0}× realtime ({sw.Elapsed.TotalMilliseconds:F1}ms for {iterations} buffers = {audioSeconds:F1}s of audio)");

            // Generous floor — tests run against Debug NAudio on CI machines of unknown speed,
            // so we set this well below the ~300× seen locally on the scalar baseline. The goal
            // is to catch egregious regressions (e.g. reintroducing a per-Read allocation or
            // an O(n²) bug), not to assert a precise SIMD number — that's the benchmark project's job.
            Assert.That(realtimeFactor, Is.GreaterThan(50),
                "MixingSampleProvider throughput regressed below 50× realtime — check the mix-add kernel and per-Read allocations");
        }
    }
}
