using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using NAudio.Nam.Internal;
using NUnit.Framework;

namespace NAudio.Nam.Tests;

/// <summary>
/// Compares the general WaveNet and LSTM engines with NeuralAmpModelerCore's output
/// for the same models and input (see TestData/README.md).
/// </summary>
[TestFixture]
[Category("UnitTest")]
public class GeneralModelGoldenTests
{
    private const float Tolerance = 1e-5f;

    // A1 "standard" (16 channels, tanh, 20 layers) is more sensitive to summation
    // order: the reference itself moves by 1.7e-5 on this input between an -O2 and an
    // -O3 -march=native build. A real bug shows up at 1e-2 or more.
    private const float A1StandardTolerance = 1e-4f;

    [TestCase("wavenet_a1_standard.nam", 1.0, "wavenet_a1_standard_expected.f32", "WaveNet", 16, A1StandardTolerance)]
    [TestCase("lstm.nam", 1.0, "lstm_expected.f32", "LSTM", 3, Tolerance)]
    [TestCase("synthetic_wavenet.nam", 1.0, "synthetic_wavenet_expected.f32", "WaveNet", 6, Tolerance)]
    [TestCase("synthetic_lstm.nam", 1.0, "synthetic_lstm_expected.f32", "LSTM", 12, Tolerance)]
    [TestCase("slimmable_container.nam", 0.0, "slimmable_container_s0_expected.f32", "LSTM", 3, Tolerance)]
    [TestCase("slimmable_container.nam", 0.5, "slimmable_container_s0.5_expected.f32", "WaveNet", 3, Tolerance)]
    [TestCase("slimmable_container.nam", 1.0, "slimmable_container_s1_expected.f32", "WaveNet", 4, Tolerance)]
    public void MatchesReferenceImplementation(string modelFile, double size, string expectedFile, string architecture, int channels, float tolerance)
    {
        var processor = NamModel.Load(TestData.Path(modelFile)).CreateProcessor(size);
        Assert.That(processor.Submodel.Architecture, Is.EqualTo(architecture));
        Assert.That(processor.Submodel.Channels, Is.EqualTo(channels));
        Assert.That(processor.Submodel.IsA2, Is.False);

        var actual = TestData.Render(processor, TestData.Input, 64);

        var expected = TestData.ReadFloats(expectedFile);
        Assert.That(TestData.MaxAbsDifference(actual, expected), Is.LessThan(tolerance));
    }

    [TestCase(0.0, "a2_lite_expected.f32")]
    [TestCase(1.0, "a2_full_expected.f32")]
    public void GeneralWaveNetEngineAlsoRunsA2Models(double size, string expectedFile)
    {
        // A2 files normally take the fast path; running one through the general
        // engine cross-checks the two implementations against the same reference.
        var json = JsonNode.Parse(File.ReadAllText(TestData.Path("A2.nam")))!;
        var model = json["config"]!["submodels"]![size < 0.5 ? 0 : 1]!["model"]!;
        using var config = JsonDocument.Parse(model["config"]!.ToJsonString());
        var weights = model["weights"]!.AsArray().Select(w => (float)w!.GetValue<double>()).ToArray();
        var dsp = new WaveNet(WaveNetSpec.Parse(config.RootElement), weights);

        var input = TestData.Input;
        var output = new float[input.Length];
        for (var offset = 0; offset < input.Length; offset += NamDsp.MaxBlockFrames)
        {
            var n = Math.Min(NamDsp.MaxBlockFrames, input.Length - offset);
            dsp.Process(input.AsSpan(offset, n), output.AsSpan(offset, n));
        }

        Assert.That(TestData.MaxAbsDifference(output, TestData.ReadFloats(expectedFile)), Is.LessThan(Tolerance));
    }

    [TestCase("synthetic_wavenet.nam")]
    [TestCase("synthetic_lstm.nam")]
    public void BlockSizeDoesNotChangeOutput(string modelFile)
    {
        var model = NamModel.Load(TestData.Path(modelFile));
        var input = TestData.Input;
        var reference = TestData.Render(model.CreateProcessor(), input, 64);

        foreach (var blockSize in new[] { 1, 37, 128, 1000 })
        {
            var actual = TestData.Render(model.CreateProcessor(), input, blockSize);
            // Vectorised activations may round the last lanes of a block differently.
            Assert.That(TestData.MaxAbsDifference(actual, reference), Is.LessThan(1e-6f), $"block size {blockSize}");
        }
    }

    [TestCase("synthetic_wavenet.nam")]
    [TestCase("synthetic_lstm.nam")]
    [TestCase("wavenet_a1_standard.nam")]
    public void ResetReturnsToTheInitialState(string modelFile)
    {
        var processor = NamModel.Load(TestData.Path(modelFile)).CreateProcessor();
        var input = TestData.Input;
        var first = TestData.Render(processor, input, 64);

        processor.Reset();
        var second = TestData.Render(processor, input, 64);

        Assert.That(TestData.MaxAbsDifference(first, second), Is.LessThan(1e-6f));
    }

    [TestCase("synthetic_wavenet.nam")]
    [TestCase("synthetic_lstm.nam")]
    public void ProcessingDoesNotAllocate(string modelFile)
    {
        var processor = NamModel.Load(TestData.Path(modelFile)).CreateProcessor();
        var buffer = new float[512];
        processor.Process(buffer, buffer);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 50; i++)
            processor.Process(buffer, buffer);
        processor.Reset();

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }

    private static JsonNode Synthetic() => JsonNode.Parse(File.ReadAllText(TestData.Path("synthetic_wavenet.nam")))!;

    private static JsonNode FirstLayerArray(JsonNode model) => model["config"]!["layers"]![0]!;

    [TestCase("conv_pre_film", "FiLM")]
    [TestCase("activation_post_film", "FiLM")]
    [TestCase("head1x1", "head1x1")]
    public void RejectsUnsupportedWaveNetFeatures(string key, string expectedInMessage)
    {
        var json = Synthetic();
        FirstLayerArray(json)[key] = new JsonObject { ["active"] = true, ["shift"] = true, ["groups"] = 1, ["out_channels"] = 2 };

        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain(expectedInMessage));
    }

    [Test]
    public void RejectsBlendedGating()
    {
        var json = Synthetic();
        FirstLayerArray(json)["gating_mode"]![1] = "blended";

        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("blended"));
    }

    [Test]
    public void RejectsGroupedConvolutions()
    {
        var json = Synthetic();
        FirstLayerArray(json)["groups_input"] = 2;

        Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
    }

    [Test]
    public void RejectsConditioningModels()
    {
        var json = Synthetic();
        json["config"]!["condition_dsp"] = JsonNode.Parse(File.ReadAllText(TestData.Path("lstm.nam")));

        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("condition_dsp"));
    }

    [Test]
    public void RejectsUnknownActivations()
    {
        var json = Synthetic();
        FirstLayerArray(json)["activation"]![0] = new JsonObject { ["type"] = "PReLU", ["negative_slope"] = 0.1 };

        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("PReLU"));
    }

    [Test]
    public void RejectsWrongLstmWeightCount()
    {
        var json = JsonNode.Parse(File.ReadAllText(TestData.Path("lstm.nam")))!;
        json["weights"]!.AsArray().RemoveAt(0);

        var ex = Assert.Throws<FormatException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("LSTM"));
    }
}
