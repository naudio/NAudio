using System;
using NAudio.Dsp;
using NUnit.Framework;

namespace NAudioTests.Dsp
{
    /// <summary>
    /// Covers the batch <see cref="BiQuadFilter.Transform(ReadOnlySpan{float}, Span{float})"/>
    /// overload — specifically that it produces byte-identical output to the single-sample
    /// <see cref="BiQuadFilter.Transform(float)"/> form, survives being split across calls,
    /// and supports in-place operation.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class BiQuadFilterTests
    {
        private static float[] GenerateTestSignal(int length, int seed = 42)
        {
            var rng = new Random(seed);
            var signal = new float[length];
            for (int i = 0; i < length; i++)
                signal[i] = (float)(rng.NextDouble() * 2 - 1);
            return signal;
        }

        [Test]
        public void BatchTransformMatchesSingleSampleTransform()
        {
            var input = GenerateTestSignal(1024);
            var batchFilter = BiQuadFilter.LowPassFilter(44100f, 1000f, 0.707f);
            var singleFilter = BiQuadFilter.LowPassFilter(44100f, 1000f, 0.707f);

            var batchOutput = new float[input.Length];
            batchFilter.Transform(input, batchOutput);

            var singleOutput = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
                singleOutput[i] = singleFilter.Transform(input[i]);

            Assert.That(batchOutput, Is.EqualTo(singleOutput),
                "batch Transform must produce byte-identical output to sample-by-sample Transform");
        }

        [Test]
        public void SplitBatchPreservesState()
        {
            // Filtering [A..B..C] in one batch must equal filtering A, then B, then C in sequence.
            // This verifies state flushes correctly between calls.
            var input = GenerateTestSignal(300);
            var singleShot = BiQuadFilter.HighPassFilter(44100f, 500f, 1.2f);
            var splitShot = BiQuadFilter.HighPassFilter(44100f, 500f, 1.2f);

            var singleShotOut = new float[input.Length];
            singleShot.Transform(input, singleShotOut);

            var splitShotOut = new float[input.Length];
            splitShot.Transform(input.AsSpan(0, 100), splitShotOut.AsSpan(0, 100));
            splitShot.Transform(input.AsSpan(100, 50), splitShotOut.AsSpan(100, 50));
            splitShot.Transform(input.AsSpan(150, 150), splitShotOut.AsSpan(150, 150));

            Assert.That(splitShotOut, Is.EqualTo(singleShotOut),
                "splitting a batch across multiple Transform calls must preserve filter state");
        }

        [Test]
        public void InPlaceTransformMatchesOutOfPlace()
        {
            // source and destination being the same span is a common usage — applying a filter in
            // place. A biquad reads source[i] before writing destination[i] on each iteration so
            // this is safe.
            var input = GenerateTestSignal(512);
            var filterA = BiQuadFilter.PeakingEQ(44100f, 2000f, 1.0f, 6f);
            var filterB = BiQuadFilter.PeakingEQ(44100f, 2000f, 1.0f, 6f);

            var outOfPlace = new float[input.Length];
            filterA.Transform(input, outOfPlace);

            var inPlace = (float[])input.Clone();
            filterB.Transform(inPlace, inPlace);

            Assert.That(inPlace, Is.EqualTo(outOfPlace),
                "in-place Transform must produce the same output as the out-of-place form");
        }

        [Test]
        public void DestinationShorterThanSourceThrows()
        {
            var filter = BiQuadFilter.LowPassFilter(44100f, 1000f, 0.707f);
            var input = new float[100];
            var destination = new float[50];

            Assert.Throws<ArgumentException>(() => filter.Transform(input, destination));
        }

        [Test]
        public void DestinationEqualToSourceLengthIsAllowed()
        {
            var filter = BiQuadFilter.LowPassFilter(44100f, 1000f, 0.707f);
            var input = new float[100];
            var destination = new float[100];

            Assert.DoesNotThrow(() => filter.Transform(input, destination));
        }

        [Test]
        public void EmptyInputIsNoOp()
        {
            // State must survive an empty call unchanged — if a consumer calls with zero samples
            // it should not perturb subsequent output.
            var filterA = BiQuadFilter.LowPassFilter(44100f, 500f, 0.9f);
            var filterB = BiQuadFilter.LowPassFilter(44100f, 500f, 0.9f);

            var input = GenerateTestSignal(64);
            var outputA = new float[input.Length];
            var outputB = new float[input.Length];

            filterA.Transform(ReadOnlySpan<float>.Empty, Span<float>.Empty);
            filterA.Transform(input, outputA);
            filterB.Transform(input, outputB);

            Assert.That(outputA, Is.EqualTo(outputB));
        }
    }
}
