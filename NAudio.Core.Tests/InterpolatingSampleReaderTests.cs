using System;
using NUnit.Framework;
using NAudio.Dsp;

namespace NAudio.Core.Tests
{
    [TestFixture]
    public class InterpolatingSampleReaderTests
    {
        private static float[] Ramp(int length)
        {
            var data = new float[length];
            for (int i = 0; i < length; i++) data[i] = i;
            return data;
        }

        [Test]
        public void LoopCrossfadeSmoothsTheSeam()
        {
            // a ramp 0..199; loop [50,150) so the values at the seam differ wildly
            // (~149 vs ~50). Without a crossfade the wrap steps by ~99; the
            // crossfade blends the approach to LoopEnd into the lead-in before
            // LoopStart, so the largest step shrinks dramatically.
            var data = Ramp(200);

            float MaxStep(int crossfade)
            {
                var reader = new InterpolatingSampleReader(
                    new SampleSource(data, 44100, LoopMode.Continuous, 0, 200, 50, 150, crossfade));
                float prev = reader.Read(1.0);
                float max = 0f;
                for (int i = 0; i < 400; i++)
                {
                    float cur = reader.Read(1.0);
                    max = Math.Max(max, Math.Abs(cur - prev));
                    prev = cur;
                }
                return max;
            }

            float hardStep = MaxStep(0);
            float fadedStep = MaxStep(20);
            Assert.That(hardStep, Is.GreaterThan(50f), "expected a large seam discontinuity without a crossfade");
            Assert.That(fadedStep, Is.LessThan(hardStep / 5f), "crossfade should greatly reduce the seam step");
        }

        [Test]
        public void ConstantDataReadsConstantAtAnyIncrement()
        {
            var data = new float[16];
            Array.Fill(data, 0.75f);
            var reader = new InterpolatingSampleReader(new SampleSource(data, 44100));
            for (int i = 0; i < 10; i++)
            {
                Assert.That(reader.Read(0.37), Is.EqualTo(0.75f).Within(1e-5f));
            }
        }

        [Test]
        public void UnityIncrementReadsConsecutiveSamples()
        {
            var reader = new InterpolatingSampleReader(new SampleSource(Ramp(8), 44100));
            for (int i = 0; i < 8; i++)
            {
                Assert.That(reader.Read(1.0), Is.EqualTo((float)i).Within(1e-5f));
            }
        }

        [Test]
        public void LinearInterpolationOnRampGivesMidpoints()
        {
            var reader = new InterpolatingSampleReader(new SampleSource(Ramp(8), 44100))
            {
                Quality = InterpolationQuality.Linear
            };
            Assert.That(reader.Read(0.5), Is.EqualTo(0.0f).Within(1e-5f));
            Assert.That(reader.Read(0.5), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(reader.Read(0.5), Is.EqualTo(1.0f).Within(1e-5f));
            Assert.That(reader.Read(0.5), Is.EqualTo(1.5f).Within(1e-5f));
        }

        [Test]
        public void NoLoopEndsAtEndAndReturnsZeroThereafter()
        {
            var reader = new InterpolatingSampleReader(new SampleSource(Ramp(4), 44100));
            for (int i = 0; i < 4; i++) reader.Read(1.0);
            Assert.That(reader.Ended, Is.True);
            Assert.That(reader.Read(1.0), Is.EqualTo(0f));
        }

        [Test]
        public void ContinuousLoopWrapsBackToLoopStart()
        {
            var data = new float[] { 10, 11, 12, 13 };
            var source = new SampleSource(data, 44100, LoopMode.Continuous);
            var reader = new InterpolatingSampleReader(source);
            float[] expected = { 10, 11, 12, 13, 10, 11, 12, 13, 10 };
            foreach (var e in expected)
            {
                Assert.That(reader.Read(1.0), Is.EqualTo(e).Within(1e-5f));
            }
            Assert.That(reader.Ended, Is.False);
        }

        [Test]
        public void UntilReleaseLoopsThenPlaysTailAfterRelease()
        {
            // [10,11,12,13] is the loop, [20,21] is the post-loop tail
            var data = new float[] { 10, 11, 12, 13, 20, 21 };
            var source = new SampleSource(data, 44100, LoopMode.UntilRelease,
                start: 0, end: 6, loopStart: 0, loopEnd: 4);
            var reader = new InterpolatingSampleReader(source);

            // two passes of the loop then two more samples
            float[] firstSix = { 10, 11, 12, 13, 10, 11 };
            foreach (var e in firstSix)
                Assert.That(reader.Read(1.0), Is.EqualTo(e).Within(1e-5f));

            reader.Release();

            // now plays through to the end of the data
            float[] tail = { 12, 13, 20, 21 };
            foreach (var e in tail)
                Assert.That(reader.Read(1.0), Is.EqualTo(e).Within(1e-5f));

            Assert.That(reader.Ended, Is.True);
        }

        [Test]
        public void BlockReadReturnsWrittenCountAndStopsAtEnd()
        {
            var reader = new InterpolatingSampleReader(new SampleSource(Ramp(4), 44100));
            var buffer = new float[10];
            int written = reader.Read(buffer, 1.0);
            Assert.That(written, Is.EqualTo(4));
            Assert.That(reader.Ended, Is.True);
            for (int i = 0; i < 4; i++)
                Assert.That(buffer[i], Is.EqualTo((float)i).Within(1e-5f));
        }

        [Test]
        public void StartOffsetBeginsPlaybackPartWayIn()
        {
            var data = new float[] { 0, 1, 2, 3, 4, 5 };
            var source = new SampleSource(data, 44100, LoopMode.None, start: 2);
            var reader = new InterpolatingSampleReader(source);
            Assert.That(reader.Read(1.0), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(reader.Read(1.0), Is.EqualTo(3f).Within(1e-5f));
        }
    }
}
