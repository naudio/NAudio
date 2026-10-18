using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NAudio.Nam.Internal;

namespace NAudio.Nam;

/// <summary>
/// A Neural Amp Modeler (<c>.nam</c>) capture of an amp, pedal or other piece of gear,
/// loaded and ready to run. Load a file with <see cref="Load(string)"/>, then process
/// audio through it with a <see cref="NamEffect"/> or a <see cref="NamProcessor"/>.
/// </summary>
/// <remarks>
/// <para>
/// Supported: NAM's Architecture 2 (A2) models, as a <c>SlimmableContainer</c> file
/// holding A2-Lite and A2-Full networks (the format TONE3000 distributes) or as a
/// single A2 WaveNet; A1 WaveNet models (standard, lite, feather, nano) and other
/// WaveNet configurations built from the same features; and LSTM models. Rarer
/// features (FiLM conditioning, grouped convolutions, conditioning models, slimmable
/// layers, post-stack heads) and the ConvNet and Linear architectures are rejected
/// with <see cref="NotSupportedException"/>, naming the feature.
/// </para>
/// <para>
/// A loaded model is immutable and may be shared: each effect or processor created
/// from it keeps its own state.
/// </para>
/// </remarks>
public sealed class NamModel
{
    // NAM file versions this loader understands, matching NeuralAmpModelerCore.
    private static readonly Version EarliestSupportedVersion = new(0, 5, 0);
    private static readonly Version LatestSupportedVersion = new(0, 7, 0);

    private NamModel(string version, string architecture, NamModelMetadata metadata, IReadOnlyList<NamSubmodel> submodels)
    {
        Version = version;
        Architecture = architecture;
        Metadata = metadata;
        Submodels = submodels;
    }

    /// <summary>The NAM file format version, e.g. "0.7.0".</summary>
    public string Version { get; }

    /// <summary>The file's top-level architecture: "SlimmableContainer" or "WaveNet".</summary>
    public string Architecture { get; }

    /// <summary>
    /// The file's top-level metadata. If the file only stores metadata on its
    /// submodels, this is the largest submodel's.
    /// </summary>
    public NamModelMetadata Metadata { get; }

    /// <summary>The networks in the file, smallest first.</summary>
    public IReadOnlyList<NamSubmodel> Submodels { get; }

    /// <summary>Loads a <c>.nam</c> file.</summary>
    /// <exception cref="FormatException">The file is not valid NAM JSON.</exception>
    /// <exception cref="NotSupportedException">The file uses an architecture or version this library cannot run.</exception>
    public static NamModel Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    /// <summary>Loads a model from a stream containing <c>.nam</c> JSON.</summary>
    /// <inheritdoc cref="Load(string)" path="/exception"/>
    public static NamModel Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            using var document = JsonDocument.Parse(stream);
            return FromJson(document.RootElement);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The model is not valid JSON: " + ex.Message, ex);
        }
    }

    /// <summary>Loads a model from a string of <c>.nam</c> JSON.</summary>
    /// <inheritdoc cref="Load(string)" path="/exception"/>
    public static NamModel Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json);
            return FromJson(document.RootElement);
        }
        catch (JsonException ex)
        {
            throw new FormatException("The model is not valid JSON: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Picks the submodel for a size from 0 (smallest, cheapest) to 1 (largest, most
    /// accurate), using NAM's rule: the first submodel whose <see cref="NamSubmodel.MaxSize"/>
    /// is greater than <paramref name="size"/>, or the last one. For an A2 file,
    /// sizes below 0.5 select A2-Lite and 0.5 and above select A2-Full.
    /// </summary>
    public NamSubmodel GetSubmodel(double size = 1.0)
    {
        if (double.IsNaN(size) || size < 0.0 || size > 1.0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Size must be between 0 and 1.");
        for (var i = 0; i < Submodels.Count - 1; i++)
        {
            if (size < Submodels[i].MaxSize)
                return Submodels[i];
        }
        return Submodels[^1];
    }

    /// <summary>
    /// Creates a processor that runs the submodel <see cref="GetSubmodel"/> picks for
    /// <paramref name="size"/>.
    /// </summary>
    public NamProcessor CreateProcessor(double size = 1.0) => new(GetSubmodel(size));

    private static NamModel FromJson(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new FormatException("A NAM model must be a JSON object.");
        var version = CheckVersion(root);
        var architecture = GetRequiredString(root, "architecture");
        var config = GetRequired(root, "config");

        List<NamSubmodel> submodels;
        if (architecture == "SlimmableContainer")
        {
            if (!config.TryGetProperty("submodels", out var entries) || entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() == 0)
                throw new FormatException("A SlimmableContainer must have a non-empty submodels array.");
            submodels = [];
            foreach (var entry in entries.EnumerateArray())
            {
                var maxValue = GetRequired(entry, "max_value");
                if (maxValue.ValueKind != JsonValueKind.Number)
                    throw new FormatException("Each submodel needs a numeric max_value.");
                var maxSize = maxValue.GetDouble();
                if (submodels.Count > 0 && maxSize <= submodels[^1].MaxSize)
                    throw new FormatException("Submodels must be sorted by ascending max_value.");
                var model = GetRequired(entry, "model");
                CheckVersion(model);
                submodels.Add(ParseNetwork(model, maxSize));
            }
            if (submodels[^1].MaxSize < 1.0)
                throw new FormatException("The last submodel's max_value must be at least 1.0.");
        }
        else
        {
            submodels = [ParseNetwork(root, 1.0)];
        }

        var metadata = NamModelMetadata.Parse(root);
        if (ReferenceEquals(metadata, NamModelMetadata.Empty))
            metadata = submodels[^1].Metadata;
        return new NamModel(version, architecture, metadata, submodels.AsReadOnly());
    }

    private static NamSubmodel ParseNetwork(JsonElement model, double maxSize)
    {
        var architecture = GetRequiredString(model, "architecture");
        var config = GetRequired(model, "config");
        var weights = ReadWeights(model);

        int? sampleRate = null;
        if (model.TryGetProperty("sample_rate", out var sr) && sr.ValueKind == JsonValueKind.Number)
        {
            var rate = sr.GetDouble();
            if (rate > 0)
                sampleRate = (int)Math.Round(rate);
        }
        var metadata = NamModelMetadata.Parse(model);

        switch (architecture)
        {
            case "WaveNet":
                if (A2Shape.Check(config, out var a2Channels) == null)
                {
                    CheckWeightCount(weights, A2Shape.WeightCount(a2Channels), $"An A2 model with {a2Channels} channels");
                    return new NamSubmodel(maxSize, architecture, isA2: true, a2Channels, sampleRate, metadata, weights,
                        w => A2WaveNet.Create(a2Channels, w));
                }
                var wavenet = WaveNetSpec.Parse(config);
                CheckWeightCount(weights, wavenet.WeightCount, "This WaveNet configuration");
                return new NamSubmodel(maxSize, architecture, isA2: false, wavenet.LayerArrays[0].Channels, sampleRate, metadata, weights,
                    w => new WaveNet(wavenet, w));
            case "LSTM":
                var lstm = LstmSpec.Parse(config);
                CheckWeightCount(weights, lstm.WeightCount, "This LSTM configuration");
                return new NamSubmodel(maxSize, architecture, isA2: false, lstm.HiddenSize, sampleRate, metadata, weights,
                    w => new Lstm(lstm, w, sampleRate));
            default:
                throw new NotSupportedException($"NAM '{architecture}' models are not supported; WaveNet (A1 and A2) and LSTM models can be loaded.");
        }
    }

    private static float[] ReadWeights(JsonElement model)
    {
        var weightsElement = GetRequired(model, "weights");
        if (weightsElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("weights must be an array.");
        var weights = new float[weightsElement.GetArrayLength()];
        var i = 0;
        foreach (var w in weightsElement.EnumerateArray())
        {
            if (w.ValueKind != JsonValueKind.Number)
                throw new FormatException("weights must contain only numbers.");
            // Parse as double then narrow, as the reference loader does, so the
            // rounding to float is identical.
            weights[i++] = (float)w.GetDouble();
        }
        return weights;
    }

    private static void CheckWeightCount(float[] weights, int expected, string description)
    {
        if (weights.Length != expected)
            throw new FormatException($"{description} has {expected} weights, but this model has {weights.Length}.");
    }

    private static string CheckVersion(JsonElement model)
    {
        var text = GetRequiredString(model, "version");
        var parts = text.Split('.');
        if (parts.Length != 3 || !System.Version.TryParse(text, out var version))
            throw new FormatException($"'{text}' is not a valid NAM file version.");
        // As in NeuralAmpModelerCore: a newer patch release is accepted, a newer
        // minor or major release is not.
        if (version < EarliestSupportedVersion
            || version.Major > LatestSupportedVersion.Major
            || (version.Major == LatestSupportedVersion.Major && version.Minor > LatestSupportedVersion.Minor))
            throw new NotSupportedException($"NAM file version {text} is not supported (supported: {EarliestSupportedVersion} to {LatestSupportedVersion.Major}.{LatestSupportedVersion.Minor}.x).");
        return text;
    }

    private static JsonElement GetRequired(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(name, out var value))
            throw new FormatException($"The model is missing '{name}'.");
        return value;
    }

    private static string GetRequiredString(JsonElement obj, string name)
    {
        var value = GetRequired(obj, name);
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : throw new FormatException($"'{name}' must be a string.");
    }
}
