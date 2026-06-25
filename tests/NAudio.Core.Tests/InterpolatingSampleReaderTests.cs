using System;
using NUnit.Framework;
using NAudio.Dsp;

namespace NAudio.Core.Tests;

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

    // Renders the same stream twice — per-sample Read(increment) versus block
    // Read(span, increment) in odd-sized chunks — and asserts bit-identical
    // output, written counts, Position and Ended transitions after every
    // chunk. The per-sample path is the source of truth; the block path's
    // safe-window fast loops must be indistinguishable from it.
    private static void AssertBlockReadMatchesPerSample(SampleSource source, InterpolationQuality quality,
        Func<int, double> incrementForChunk, int totalSamples, int releaseAtChunk, string context)
    {
        int[] chunkSizes = { 1, 7, 64, 113 };
        var perSample = new InterpolatingSampleReader(source) { Quality = quality };
        var block = new InterpolatingSampleReader(source) { Quality = quality };
        var expected = new float[113];
        var actual = new float[113];
        int produced = 0;
        int chunkIndex = 0;
        while (produced < totalSamples)
        {
            if (chunkIndex == releaseAtChunk)
            {
                perSample.Release();
                block.Release();
            }
            int chunk = chunkSizes[chunkIndex % chunkSizes.Length];
            double increment = incrementForChunk(chunkIndex);
            int expectedCount = 0;
            for (int i = 0; i < chunk; i++)
            {
                if (perSample.Ended) break;
                expected[expectedCount++] = perSample.Read(increment);
            }
            int actualCount = block.Read(actual.AsSpan(0, chunk), increment);
            string where = $"{context} chunk={chunkIndex} inc={increment} at sample {produced}";
            Assert.That(actualCount, Is.EqualTo(expectedCount), where);
            for (int i = 0; i < expectedCount; i++)
            {
                if (BitConverter.SingleToInt32Bits(expected[i]) != BitConverter.SingleToInt32Bits(actual[i]))
                    Assert.Fail($"{where}+{i}: expected {expected[i]:R} but was {actual[i]:R}");
            }
            Assert.That(block.Ended, Is.EqualTo(perSample.Ended), where);
            Assert.That(block.Position, Is.EqualTo(perSample.Position), where);
            produced += chunk;
            chunkIndex++;
            if (block.Ended) break;
        }
    }

    private static readonly double[] EquivalenceIncrements = { 0.25, 0.5, 0.997, 1.0, 1.5, 2.7 };

    [Test]
    public void BlockReadMatchesPerSampleReadBitExactlyAcrossRandomizedConfigs()
    {
        // seeded sweep: quality x loop mode x random data/bounds/loop
        // points/crossfade, increments fixed or varying per chunk, short
        // (down to 4-sample) and long loops, release at varied times
        var rng = new Random(0xACE5);
        foreach (InterpolationQuality quality in Enum.GetValues<InterpolationQuality>())
        {
            foreach (LoopMode loopMode in Enum.GetValues<LoopMode>())
            {
                for (int cfg = 0; cfg < 8; cfg++)
                {
                    bool shortData = rng.Next(3) == 0;
                    int length = shortData ? rng.Next(12, 30) : rng.Next(150, 400);
                    var data = new float[length];
                    for (int i = 0; i < length; i++) data[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
                    int start = rng.Next(0, 4);
                    int end = length - rng.Next(0, 4);
                    int? loopStart = null, loopEnd = null;
                    int crossfade = 0;
                    if (loopMode != LoopMode.None)
                    {
                        int maxLen = end - start;
                        int loopLength = rng.Next(3) == 0 ? 4 : rng.Next(8, Math.Max(9, maxLen));
                        loopLength = Math.Clamp(loopLength, 1, maxLen);
                        int ls = start + rng.Next(0, end - start - loopLength + 1);
                        loopStart = ls;
                        loopEnd = ls + loopLength;
                        crossfade = rng.Next(2) == 0 ? 0 : rng.Next(1, 24); // ctor clamps
                    }
                    var source = new SampleSource(data, 44100, loopMode, start, end, loopStart, loopEnd, crossfade);
                    bool constantIncrement = rng.Next(2) == 0;
                    double fixedIncrement = EquivalenceIncrements[rng.Next(EquivalenceIncrements.Length)];
                    // increments must be deterministic per chunk index (the
                    // helper asks once per chunk for both renderers)
                    int incrementSeed = rng.Next();
                    Func<int, double> incrementForChunk = constantIncrement
                        ? (_ => fixedIncrement)
                        : (chunkIndex => EquivalenceIncrements[
                              new Random(incrementSeed + chunkIndex).Next(EquivalenceIncrements.Length)]);
                    int releaseAtChunk = loopMode == LoopMode.UntilRelease ? rng.Next(0, 10) : -1;
                    AssertBlockReadMatchesPerSample(source, quality, incrementForChunk, 350, releaseAtChunk,
                        $"quality={quality} mode={loopMode} cfg={cfg} len={length} start={start} end={end} " +
                        $"loop=[{loopStart},{loopEnd}) xfade={crossfade} release@{releaseAtChunk}");
                }
            }
        }
    }

    [Test]
    public void BlockReadMatchesPerSampleAcrossTheCrossfadeZone()
    {
        // a ramp makes seam/crossfade mistakes obvious; the 20-sample
        // crossfade zone must be handled by the careful per-sample path
        var data = Ramp(300);
        var source = new SampleSource(data, 44100, LoopMode.Continuous, 0, 300, 60, 220, 20);
        foreach (InterpolationQuality quality in Enum.GetValues<InterpolationQuality>())
        {
            AssertBlockReadMatchesPerSample(source, quality,
                chunkIndex => EquivalenceIncrements[chunkIndex % EquivalenceIncrements.Length],
                600, -1, $"crossfade quality={quality}");
        }
    }

    [Test]
    public void BlockReadMatchesPerSampleOnAFourSampleLoop()
    {
        // a loop barely longer than the Hermite tap span offers at most a
        // one-sample safe window, so nearly every sample takes the careful
        // per-sample path — including multi-wrap advances (increment 2.7,
        // most of the 4-sample loop length)
        var data = new float[] { 0.1f, -0.7f, 0.9f, -0.2f, 0.5f, -0.4f, 0.3f, 0.8f, -0.6f, 0.2f };
        var source = new SampleSource(data, 44100, LoopMode.UntilRelease,
            start: 0, end: 10, loopStart: 2, loopEnd: 6);
        AssertBlockReadMatchesPerSample(source, InterpolationQuality.Hermite,
            chunkIndex => EquivalenceIncrements[(chunkIndex * 5) % EquivalenceIncrements.Length],
            400, 6, "four-sample loop");
    }

    [Test]
    public void BlockReadEndedTransitionMatchesPerSample()
    {
        // a fractional increment so the end is crossed mid-chunk
        var data = Ramp(97);
        var source = new SampleSource(data, 44100, LoopMode.None, start: 1, end: 96);
        foreach (InterpolationQuality quality in Enum.GetValues<InterpolationQuality>())
        {
            AssertBlockReadMatchesPerSample(source, quality, _ => 0.997, 200, -1,
                $"ended transition quality={quality}");
        }
    }
}
