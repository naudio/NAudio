using System;
using System.IO;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace NAudio.Nam.Tests;

[TestFixture]
[Category("UnitTest")]
public class NamModelTests
{
    private static JsonNode A2Json() => JsonNode.Parse(File.ReadAllText(TestData.Path("A2.nam")))!;

    private static JsonNode FullSubmodelJson() => A2Json()["config"]!["submodels"]![1]!["model"]!.DeepClone();

    [Test]
    public void LoadsA2Container()
    {
        var model = TestData.LoadA2();

        Assert.That(model.Version, Is.EqualTo("0.7.0"));
        Assert.That(model.Architecture, Is.EqualTo("SlimmableContainer"));
        Assert.That(model.Submodels, Has.Count.EqualTo(2));

        var lite = model.Submodels[0];
        Assert.That(lite.IsA2Lite, Is.True);
        Assert.That(lite.Channels, Is.EqualTo(3));
        Assert.That(lite.WeightCount, Is.EqualTo(1871));
        Assert.That(lite.MaxSize, Is.EqualTo(0.5));
        Assert.That(lite.SampleRate, Is.EqualTo(48000));

        var full = model.Submodels[1];
        Assert.That(full.IsA2Full, Is.True);
        Assert.That(full.Channels, Is.EqualTo(8));
        Assert.That(full.WeightCount, Is.EqualTo(12146));
        Assert.That(full.MaxSize, Is.EqualTo(1.0));
    }

    [Test]
    public void ReadsMetadata()
    {
        var model = TestData.LoadA2();

        Assert.That(model.Metadata.Loudness, Is.EqualTo(-26.3839).Within(1e-4));
        Assert.That(model.Metadata.InputLevelDbu, Is.EqualTo(14.76).Within(1e-9));
        Assert.That(model.Metadata.Name, Is.Null);
        Assert.That(model.Submodels[0].Metadata.Loudness, Is.EqualTo(-24.0382).Within(1e-4));
    }

    [TestCase(0.0, 3)]
    [TestCase(0.49, 3)]
    [TestCase(0.5, 8)]
    [TestCase(1.0, 8)]
    public void SizePicksSubmodelLikeNam(double size, int channels)
    {
        Assert.That(TestData.LoadA2().GetSubmodel(size).Channels, Is.EqualTo(channels));
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    [TestCase(double.NaN)]
    public void RejectsSizeOutsideZeroToOne(double size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.LoadA2().GetSubmodel(size));
    }

    [Test]
    public void RejectsOtherArchitectures()
    {
        const string lstm = """{"version":"0.5.4","architecture":"LSTM","config":{"input_size":1,"hidden_size":3,"num_layers":1},"weights":[],"sample_rate":48000}""";
        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(lstm));
        Assert.That(ex!.Message, Does.Contain("LSTM"));
    }

    [Test]
    public void RejectsNonA2WaveNet()
    {
        var json = FullSubmodelJson();
        json["config"]!["layers"]![0]!["dilations"]![0] = 2;

        var ex = Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("dilations"));
    }

    [Test]
    public void RejectsGatedWaveNet()
    {
        var json = FullSubmodelJson();
        json["config"]!["layers"]![0]!["gating_mode"]![3] = "gated";

        Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
    }

    [TestCase("0.4.9")]
    [TestCase("0.8.0")]
    [TestCase("1.0.0")]
    public void RejectsUnsupportedVersions(string version)
    {
        var json = FullSubmodelJson();
        json["version"] = version;

        Assert.Throws<NotSupportedException>(() => NamModel.Parse(json.ToJsonString()));
    }

    [Test]
    public void AcceptsNewerPatchVersion()
    {
        var json = FullSubmodelJson();
        json["version"] = "0.7.3";

        Assert.That(NamModel.Parse(json.ToJsonString()).Version, Is.EqualTo("0.7.3"));
    }

    [Test]
    public void RejectsWrongWeightCount()
    {
        var json = FullSubmodelJson();
        json["weights"]!.AsArray().RemoveAt(0);

        var ex = Assert.Throws<FormatException>(() => NamModel.Parse(json.ToJsonString()));
        Assert.That(ex!.Message, Does.Contain("12146"));
    }

    [Test]
    public void RejectsInvalidJson()
    {
        Assert.Throws<FormatException>(() => NamModel.Parse("{ not json"));
    }

    [Test]
    public void RejectsUnsortedContainer()
    {
        var json = A2Json();
        json["config"]!["submodels"]![0]!["max_value"] = 1.0;

        Assert.Throws<FormatException>(() => NamModel.Parse(json.ToJsonString()));
    }
}
