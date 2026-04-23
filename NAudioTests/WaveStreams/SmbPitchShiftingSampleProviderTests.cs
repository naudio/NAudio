using System;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudioTests.WaveStreams;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    /// <summary>
    /// Covers the Span-based rewrite of <see cref="SmbPitchShifter"/> and the
    /// per-Read allocation removal in <see cref="SmbPitchShiftingSampleProvider"/>.
    /// </summary>
    [TestFixture]
    public class SmbPitchShiftingSampleProviderTests
    {
        private static float[] MakeSineMono(int n, float freq = 440f, int sampleRate = 44100)
        {
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = (float)Math.Sin(2 * Math.PI * freq * i / sampleRate) * 0.5f;
            return data;
        }

        /// <summary>
        /// Sample provider that replays a fixed float buffer and loops, so a read loop
        /// can call Read many times without running out of source data.
        /// </summary>
        private sealed class LoopingSampleSource : ISampleProvider
        {
            private readonly float[] data;
            private int pos;
            public LoopingSampleSource(float[] data, int sampleRate, int channels)
            {
                this.data = data;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }
            public WaveFormat WaveFormat { get; }
            public int Read(Span<float> buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = data[pos];
                    pos = (pos + 1) % data.Length;
                }
                return buffer.Length;
            }
        }

        [Test]
        public void PitchShift_ArrayAndSpanOverloads_ProduceIdenticalOutput()
        {
            // The float[] overload now forwards to the Span overload; run the same signal
            // through two independently-initialised shifters and verify parity.
            var arrayShifter = new SmbPitchShifter();
            var spanShifter = new SmbPitchShifter();

            var arrayData = MakeSineMono(8192);
            var spanData = (float[])arrayData.Clone();

            arrayShifter.PitchShift(1.25f, arrayData.Length, 2048, 4, 44100f, arrayData);
            spanShifter.PitchShift(1.25f, spanData.Length, 2048, 4, 44100f, spanData.AsSpan());

            Assert.That(spanData, Is.EqualTo(arrayData).AsCollection);
        }

        [Test]
        public void ShortTimeFourierTransform_ArrayAndSpanOverloads_ProduceIdenticalOutput()
        {
            var shifter = new SmbPitchShifter();
            const int fft = 2048;
            var arr = new float[2 * fft];
            for (int i = 0; i < fft; i++)
            {
                arr[2 * i] = (float)Math.Sin(2 * Math.PI * i / 64.0);
                arr[2 * i + 1] = 0;
            }
            var spanCopy = (float[])arr.Clone();

            shifter.ShortTimeFourierTransform(arr, fft, -1);
            shifter.ShortTimeFourierTransform(spanCopy.AsSpan(), fft, -1);

            Assert.That(spanCopy, Is.EqualTo(arr).AsCollection);
        }

        [Test]
        public void SmbProvider_PitchOne_PassesThroughUnchanged()
        {
            // When pitch == 1 the provider returns the source unchanged.
            var input = MakeSineMono(1024);
            var source = new LoopingSampleSource(input, 44100, 1);
            var shifter = new SmbPitchShiftingSampleProvider(source);
            Assert.That(shifter.PitchFactor, Is.EqualTo(1.0f));

            var buffer = new float[1024];
            int read = shifter.Read(buffer);
            Assert.That(read, Is.EqualTo(1024));
            Assert.That(buffer, Is.EqualTo(input).AsCollection);
        }

        [Test]
        public void SmbProvider_Mono_ProducesFiniteNonSilentOutput()
        {
            var source = new LoopingSampleSource(MakeSineMono(16384), 44100, 1);
            var shifter = new SmbPitchShiftingSampleProvider(source) { PitchFactor = 1.5f };

            var buffer = new float[4096];
            // Run several buffers so the pitch shifter's internal FIFO warms up and produces output.
            for (int i = 0; i < 8; i++) shifter.Read(buffer);

            bool anyNonZero = false;
            foreach (var v in buffer)
            {
                Assert.That(float.IsFinite(v), Is.True, "Output must be finite");
                if (Math.Abs(v) > 1e-4f) anyNonZero = true;
            }
            Assert.That(anyNonZero, Is.True, "After warm-up the shifter should produce audible output");
        }

        [Test]
        public void SmbProvider_Stereo_ProducesFiniteNonSilentOutput()
        {
            // stereo interleaved
            var src = MakeSineMono(16384 * 2);
            var source = new LoopingSampleSource(src, 44100, 2);
            var shifter = new SmbPitchShiftingSampleProvider(source) { PitchFactor = 0.8f };

            var buffer = new float[4096]; // 2048 stereo pairs
            for (int i = 0; i < 8; i++) shifter.Read(buffer);

            bool anyNonZero = false;
            foreach (var v in buffer)
            {
                Assert.That(float.IsFinite(v), Is.True);
                if (Math.Abs(v) > 1e-4f) anyNonZero = true;
            }
            Assert.That(anyNonZero, Is.True);
        }

        [Test]
        public void SmbProvider_Mono_SteadyStateReadsDoNotAllocate()
        {
            var source = new LoopingSampleSource(MakeSineMono(16384), 44100, 1);
            var shifter = new SmbPitchShiftingSampleProvider(source) { PitchFactor = 1.5f };
            var buffer = new float[4096];

            // Warm up (some JIT tiering / one-time paths may allocate on the first few calls).
            for (int i = 0; i < 4; i++) shifter.Read(buffer);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 50; i++) shifter.Read(buffer);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0),
                $"Mono SmbPitchShiftingSampleProvider.Read should not allocate after warm-up; got {after - before} bytes over 50 reads");
        }

        [Test]
        public void SmbProvider_Stereo_SteadyStateReadsDoNotAllocate()
        {
            var source = new LoopingSampleSource(MakeSineMono(32768), 44100, 2);
            var shifter = new SmbPitchShiftingSampleProvider(source) { PitchFactor = 1.5f };
            var buffer = new float[4096];

            for (int i = 0; i < 4; i++) shifter.Read(buffer); // warm up + grow internal buffers

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 50; i++) shifter.Read(buffer);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0),
                $"Stereo SmbPitchShiftingSampleProvider.Read should not allocate after warm-up; got {after - before} bytes over 50 reads");
        }
    }
}
