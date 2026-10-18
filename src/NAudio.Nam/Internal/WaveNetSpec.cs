using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NAudio.Nam.Internal;

/// <summary>
/// A parsed and validated WaveNet configuration: the general NAM WaveNet used by A1
/// models (standard, lite, feather, nano) and custom architectures. Both the legacy
/// keys (<c>kernel_size</c>, <c>head_size</c>, <c>head_bias</c>, <c>gated</c>) and the
/// current ones (<c>kernel_sizes</c>, <c>head</c>, <c>gating_mode</c>, <c>bottleneck</c>,
/// <c>layer1x1</c>) are read, as in NeuralAmpModelerCore's <c>parse_config_json</c>.
/// Features this engine does not implement are rejected with
/// <see cref="NotSupportedException"/>, because the weight stream alone cannot
/// reveal them.
/// </summary>
internal sealed class WaveNetSpec
{
    private static readonly string[] FilmKeys =
    [
        "conv_pre_film", "conv_post_film", "input_mixin_pre_film", "input_mixin_post_film",
        "activation_pre_film", "activation_post_film", "layer1x1_post_film", "head1x1_post_film",
    ];

    public required IReadOnlyList<LayerArraySpec> LayerArrays { get; init; }

    public int WeightCount
    {
        get
        {
            var total = 0;
            foreach (var la in LayerArrays)
                total += la.WeightCount;
            return total + 1; // head scale
        }
    }

    public sealed class LayerArraySpec
    {
        public required int InputSize { get; init; }
        public required int Channels { get; init; }
        public required int Bottleneck { get; init; }
        public required int[] KernelSizes { get; init; }
        public required int[] Dilations { get; init; }
        public required Activation[] Activations { get; init; }
        /// <summary>Per layer: null for no gating, or the gate's activation for a gated layer.</summary>
        public required Activation?[] GateActivations { get; init; }
        public required bool Layer1x1 { get; init; }
        public required int HeadSize { get; init; }
        public required int HeadKernelSize { get; init; }
        public required int HeadDilation { get; init; }
        public required bool HeadBias { get; init; }

        public int WeightCount
        {
            get
            {
                var total = InputSize * Channels; // rechannel, no bias
                for (var i = 0; i < Dilations.Length; i++)
                {
                    var zRows = GateActivations[i] != null ? 2 * Bottleneck : Bottleneck;
                    total += KernelSizes[i] * Channels * zRows + zRows; // dilated conv with bias
                    total += zRows;                                     // input mixin, no bias
                    if (Layer1x1)
                        total += Bottleneck * Channels + Channels;      // 1x1 with bias
                }
                return total + HeadKernelSize * Bottleneck * HeadSize + (HeadBias ? HeadSize : 0);
            }
        }
    }

    public static WaveNetSpec Parse(JsonElement config)
    {
        if (config.ValueKind != JsonValueKind.Object)
            throw new FormatException("The WaveNet config is not an object.");
        if (!IsAbsentOrNull(config, "condition_dsp"))
            throw new NotSupportedException("WaveNet models with a conditioning model (condition_dsp) are not supported.");
        if (!IsAbsentOrNull(config, "head"))
            throw new NotSupportedException("WaveNet models with a post-stack head are not supported.");
        if (GetInt(config, "in_channels", 1) != 1)
            throw new NotSupportedException("Only single-input WaveNet models are supported.");
        if (!config.TryGetProperty("layers", out var layersJson) || layersJson.ValueKind != JsonValueKind.Array || layersJson.GetArrayLength() == 0)
            throw new FormatException("A WaveNet needs a non-empty \"layers\" array.");

        var arrays = new List<LayerArraySpec>();
        foreach (var la in layersJson.EnumerateArray())
            arrays.Add(ParseLayerArray(la, arrays.Count));

        for (var i = 0; i < arrays.Count; i++)
        {
            var expectedInput = i == 0 ? 1 : arrays[i - 1].Channels;
            if (arrays[i].InputSize != expectedInput)
                throw new FormatException($"Layer array {i} has input_size {arrays[i].InputSize}, expected {expectedInput}.");
            // The previous array's head output seeds this array's head accumulator.
            if (i > 0 && arrays[i - 1].HeadSize != arrays[i].Bottleneck)
                throw new FormatException($"Layer array {i - 1}'s head_size must equal layer array {i}'s bottleneck.");
        }
        if (arrays[^1].HeadSize != 1)
            throw new NotSupportedException("Only single-output WaveNet models are supported.");

        return new WaveNetSpec { LayerArrays = arrays };
    }

    private static LayerArraySpec ParseLayerArray(JsonElement la, int index)
    {
        if (la.ValueKind != JsonValueKind.Object)
            throw new FormatException($"Layer array {index} is not an object.");
        string Where(string what) => $"layer array {index}: {what}";

        if (GetInt(la, "condition_size", 0) != 1)
            throw new NotSupportedException(Where("only condition_size 1 is supported"));
        if (GetInt(la, "groups_input", 1) != 1 || GetInt(la, "groups_input_mixin", 1) != 1)
            throw new NotSupportedException(Where("grouped convolutions are not supported"));
        if (!IsAbsentOrNull(la, "slimmable"))
            throw new NotSupportedException(Where("slimmable layers are not supported"));
        foreach (var key in FilmKeys)
        {
            if (la.TryGetProperty(key, out var film) && film.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.False => false,
                JsonValueKind.Object => GetBool(film, "active", false),
                _ => true,
            })
                throw new NotSupportedException(Where($"FiLM ({key}) is not supported"));
        }
        if (la.TryGetProperty("head1x1", out var head1x1) && head1x1.ValueKind == JsonValueKind.Object && GetBool(head1x1, "active", false))
            throw new NotSupportedException(Where("head1x1 is not supported"));

        var channels = RequireInt(la, "channels", index);
        var bottleneck = GetInt(la, "bottleneck", channels);
        var inputSize = RequireInt(la, "input_size", index);

        var layer1x1 = true;
        if (la.TryGetProperty("layer1x1", out var l1) && l1.ValueKind == JsonValueKind.Object)
        {
            layer1x1 = GetBool(l1, "active", true);
            if (GetInt(l1, "groups", 1) != 1)
                throw new NotSupportedException(Where("grouped layer1x1 is not supported"));
        }
        if (!layer1x1 && bottleneck != channels)
            throw new FormatException(Where("an inactive layer1x1 needs bottleneck equal to channels"));

        int headSize, headKernel = 1, headDilation = 1;
        bool headBias;
        if (la.TryGetProperty("head", out var head) && head.ValueKind != JsonValueKind.Null)
        {
            if (head.ValueKind != JsonValueKind.Object)
                throw new FormatException(Where("'head' must be an object"));
            headSize = RequireInt(head, "out_channels", index);
            headKernel = RequireInt(head, "kernel_size", index);
            headDilation = GetInt(head, "head_dilation", 1);
            headBias = GetBool(head, "bias", false);
        }
        else
        {
            headSize = RequireInt(la, "head_size", index);
            headBias = GetBool(la, "head_bias", false);
        }

        if (!la.TryGetProperty("dilations", out var dilationsJson) || dilationsJson.ValueKind != JsonValueKind.Array || dilationsJson.GetArrayLength() == 0)
            throw new FormatException(Where("dilations are missing"));
        var dilations = ReadInts(dilationsJson, index);
        var n = dilations.Length;

        int[] kernelSizes;
        if (la.TryGetProperty("kernel_sizes", out var ks))
            kernelSizes = ReadInts(ks, index);
        else
            kernelSizes = Repeat(RequireInt(la, "kernel_size", index), n);
        if (kernelSizes.Length != n)
            throw new FormatException(Where("kernel_sizes must have one entry per dilation"));

        if (!la.TryGetProperty("activation", out var act))
            throw new FormatException(Where("activation is missing"));
        var activations = new Activation[n];
        if (act.ValueKind == JsonValueKind.Array)
        {
            if (act.GetArrayLength() != n)
                throw new FormatException(Where("activation must have one entry per layer"));
            var i = 0;
            foreach (var a in act.EnumerateArray())
                activations[i++] = Activation.Parse(a);
        }
        else
        {
            var single = Activation.Parse(act);
            Array.Fill(activations, single);
        }

        var gates = new Activation?[n];
        if (la.TryGetProperty("gating_mode", out var gating) && gating.ValueKind != JsonValueKind.Null)
        {
            la.TryGetProperty("secondary_activation", out var secondary);
            for (var i = 0; i < n; i++)
            {
                var mode = gating.ValueKind == JsonValueKind.Array
                    ? (gating.GetArrayLength() == n ? gating[i].GetString() : throw new FormatException(Where("gating_mode must have one entry per layer")))
                    : gating.GetString();
                if (mode == "none")
                    continue;
                if (mode != "gated")
                    throw new NotSupportedException(Where($"gating_mode '{mode}' is not supported"));
                var s = secondary.ValueKind == JsonValueKind.Array ? secondary[i] : secondary;
                gates[i] = s.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? Activation.Sigmoid : Activation.Parse(s);
            }
        }
        else if (la.TryGetProperty("gated", out var gated) && gated.ValueKind == JsonValueKind.True)
        {
            Array.Fill(gates, Activation.Sigmoid);
        }

        foreach (var k in kernelSizes)
        {
            if (k < 1)
                throw new FormatException(Where("kernel sizes must be at least 1"));
        }
        foreach (var d in dilations)
        {
            if (d < 1)
                throw new FormatException(Where("dilations must be at least 1"));
        }
        if (channels < 1 || bottleneck < 1 || inputSize < 1 || headSize < 1 || headKernel < 1 || headDilation < 1)
            throw new FormatException(Where("sizes must be at least 1"));

        return new LayerArraySpec
        {
            InputSize = inputSize,
            Channels = channels,
            Bottleneck = bottleneck,
            KernelSizes = kernelSizes,
            Dilations = dilations,
            Activations = activations,
            GateActivations = gates,
            Layer1x1 = layer1x1,
            HeadSize = headSize,
            HeadKernelSize = headKernel,
            HeadDilation = headDilation,
            HeadBias = headBias,
        };
    }

    private static int[] ReadInts(JsonElement array, int index)
    {
        if (array.ValueKind != JsonValueKind.Array)
            throw new FormatException($"Layer array {index}: expected an array of integers.");
        var result = new int[array.GetArrayLength()];
        var i = 0;
        foreach (var e in array.EnumerateArray())
            result[i++] = e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v) ? v : throw new FormatException($"Layer array {index}: expected integers.");
        return result;
    }

    private static int[] Repeat(int value, int count)
    {
        var result = new int[count];
        Array.Fill(result, value);
        return result;
    }

    private static int RequireInt(JsonElement obj, string name, int index) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)
            ? i
            : throw new FormatException($"Layer array {index}: '{name}' is missing or not an integer.");

    private static bool IsAbsentOrNull(JsonElement obj, string name) =>
        !obj.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    private static int GetInt(JsonElement obj, string name, int fallback) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var v) ? v : fallback;

    private static bool GetBool(JsonElement obj, string name, bool fallback) =>
        obj.TryGetProperty(name, out var value) ? value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => fallback,
        } : fallback;
}
