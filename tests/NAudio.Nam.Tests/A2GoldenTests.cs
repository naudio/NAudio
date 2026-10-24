using System;
using System.Text.Json.Nodes;
using NAudio.Nam.Internal;
using NUnit.Framework;

namespace NAudio.Nam.Tests;

/// <summary>
/// Compares this library's output with NeuralAmpModelerCore's for the same model
/// and input. The reference sums in a different order (Eigen GEMMs), so results
/// match to float rounding rather than bit for bit.
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class A2GoldenTests
{
    // The reference's own fast and generic paths differ by up to ~4e-7 on this input.
    private const float Tolerance = 1e-5f;

    [TestCase(0.0, "a2_lite_expected.f32", 3)]
    [TestCase(1.0, "a2_full_expected.f32", 8)]
    public void MatchesReferenceImplementation(double size, string expectedFile, int channels)
    {
        var processor = TestData.LoadA2().CreateProcessor(size);
        Assert.That(processor.Submodel.Channels, Is.EqualTo(channels));

        var actual = TestData.Render(processor, TestData.Input, 64);

        var expected = TestData.ReadFloats(expectedFile);
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        Assert.That(TestData.MaxAbsDifference(actual, expected), Is.LessThan(Tolerance));
    }

    [Test]
    public void LiteAndFullProduceDifferentOutput()
    {
        var lite = TestData.ReadFloats("a2_lite_expected.f32");
        var full = TestData.ReadFloats("a2_full_expected.f32");
        Assert.That(TestData.MaxAbsDifference(lite, full), Is.GreaterThan(0.01f));
    }

    [Test]
    public void TwoLaneFallbackMatchesReference()
    {
        // The 2 x 128-bit layout runs on Arm64; check it here even on AVX hardware.
        var submodel = TestData.LoadA2().GetSubmodel(1.0);
        var dsp = new A2WaveNet<Lanes8x2>(submodel.Channels, submodel.Weights);
        var input = TestData.Input;
        var output = new float[input.Length];
        for (var offset = 0; offset < input.Length; offset += NamDsp.MaxBlockFrames)
        {
            var n = Math.Min(NamDsp.MaxBlockFrames, input.Length - offset);
            dsp.Process(input.AsSpan(offset, n), output.AsSpan(offset, n));
        }

        var expected = TestData.ReadFloats("a2_full_expected.f32");
        Assert.That(TestData.MaxAbsDifference(output, expected), Is.LessThan(Tolerance));
    }

    [TestCase(0.0)]
    [TestCase(1.0)]
    public void BlockSizeDoesNotChangeOutput(double size)
    {
        var model = TestData.LoadA2();
        var input = TestData.Input;
        var reference = TestData.Render(model.CreateProcessor(size), input, 64);

        foreach (var blockSize in new[] { 1, 37, 128, 129, 1000, input.Length })
        {
            var actual = TestData.Render(model.CreateProcessor(size), input, blockSize);
            Assert.That(actual, Is.EqualTo(reference), $"block size {blockSize}");
        }
    }

    [TestCase(0.0)]
    [TestCase(1.0)]
    public void ResetReturnsToTheInitialState(double size)
    {
        var processor = TestData.LoadA2().CreateProcessor(size);
        var input = TestData.Input;
        var first = TestData.Render(processor, input, 64);

        processor.Reset();
        var second = TestData.Render(processor, input, 64);

        Assert.That(TestData.MaxAbsDifference(first, second), Is.LessThan(1e-6f));
    }

    [Test]
    public void ProcessesInPlace()
    {
        var model = TestData.LoadA2();
        var input = TestData.Input;
        var expected = TestData.Render(model.CreateProcessor(), input, 300);

        var buffer = (float[])input.Clone();
        var processor = model.CreateProcessor();
        for (var offset = 0; offset < buffer.Length; offset += 300)
        {
            var block = buffer.AsSpan(offset, Math.Min(300, buffer.Length - offset));
            processor.Process(block, block);
        }

        Assert.That(buffer, Is.EqualTo(expected));
    }

    [Test]
    public void LoadsAStandaloneA2WaveNet()
    {
        // TONE3000 ships containers, but a single A2 WaveNet is a valid file too.
        var container = JsonNode.Parse(System.IO.File.ReadAllText(TestData.Path("A2.nam")))!;
        var full = container["config"]!["submodels"]![1]!["model"]!.ToJsonString();

        var model = NamModel.Parse(full);

        Assert.That(model.Architecture, Is.EqualTo("WaveNet"));
        Assert.That(model.Submodels, Has.Count.EqualTo(1));
        var actual = TestData.Render(model.CreateProcessor(0.0), TestData.Input, 64);
        var expected = TestData.ReadFloats("a2_full_expected.f32");
        Assert.That(TestData.MaxAbsDifference(actual, expected), Is.LessThan(Tolerance));
    }

    [Test]
    public void ProcessingDoesNotAllocate()
    {
        var processor = TestData.LoadA2().CreateProcessor();
        var buffer = new float[512];
        processor.Process(buffer, buffer);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
            processor.Process(buffer, buffer);
        processor.Reset();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }
}
