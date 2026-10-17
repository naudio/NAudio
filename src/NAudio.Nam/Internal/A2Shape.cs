// Ported to C# for NAudio from NeuralAmpModelerCore
// (https://github.com/sdatkinson/NeuralAmpModelerCore), NAM/wavenet/a2_fast.cpp (is_a2_shape).
// Original licence:
//
//     MIT License
//
//     Copyright (c) 2023 Steven Atkinson
//
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Text.Json;

namespace NAudio.Nam.Internal;

/// <summary>
/// The fixed A2 network shape, and a strict check that a WaveNet config matches it.
/// The check mirrors <c>is_a2_shape</c> in NeuralAmpModelerCore: any WaveNet feature
/// A2 does not use (gating, FiLM, grouped convolutions, a conditioning model, ...)
/// is rejected, because the weight stream alone cannot tell such a model apart.
/// </summary>
internal static class A2Shape
{
    public const int LayerCount = 23;
    public const int HeadKernelSize = 16;
    public const float LeakySlope = 0.01f;

    public static ReadOnlySpan<int> KernelSizes =>
        [6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 15, 15, 6, 6, 6, 6, 6, 6, 6];

    public static ReadOnlySpan<int> Dilations =>
        [1, 3, 7, 17, 41, 101, 239, 1, 3, 7, 17, 41, 101, 239, 1, 13, 1, 3, 7, 17, 41, 101, 239];

    private static readonly string[] FilmKeys =
    [
        "conv_pre_film", "conv_post_film", "input_mixin_pre_film", "input_mixin_post_film",
        "activation_pre_film", "activation_post_film", "layer1x1_post_film", "head1x1_post_film",
    ];

    /// <summary>
    /// Returns null if <paramref name="config"/> (a WaveNet "config" object) is the A2
    /// shape, with <paramref name="channels"/> set to 3 (A2-Lite) or 8 (A2-Full);
    /// otherwise returns why it is not.
    /// </summary>
    public static string? Check(JsonElement config, out int channels)
    {
        channels = 0;
        if (config.ValueKind != JsonValueKind.Object)
            return "the WaveNet config is not an object";
        if (!config.TryGetProperty("layers", out var layerArrays) || layerArrays.ValueKind != JsonValueKind.Array || layerArrays.GetArrayLength() != 1)
            return "A2 has exactly one layer array";
        if (!IsAbsentOrNull(config, "head"))
            return "A2 has no post-stack head";
        if (!IsAbsentOrNull(config, "condition_dsp"))
            return "A2 has no conditioning model (condition_dsp)";
        if (!config.TryGetProperty("head_scale", out var headScale) || headScale.ValueKind != JsonValueKind.Number)
            return "head_scale is missing";
        if (GetInt(config, "in_channels", 1) != 1)
            return "A2 models take one input channel";

        var la = layerArrays[0];
        if (la.ValueKind != JsonValueKind.Object)
            return "the layer array is not an object";
        if (GetInt(la, "input_size", 0) != 1 || GetInt(la, "condition_size", 0) != 1)
            return "A2 has input_size and condition_size of 1";

        var ch = GetInt(la, "channels", 0);
        if (ch != GetInt(la, "bottleneck", 0))
            return "A2 has bottleneck equal to channels";
        if (ch != 3 && ch != 8)
            return $"A2 has 3 (Lite) or 8 (Full) channels, not {ch}";

        if (!IntArrayEquals(la, "kernel_sizes", KernelSizes))
            return "kernel_sizes do not match A2";
        if (!IntArrayEquals(la, "dilations", Dilations))
            return "dilations do not match A2";

        if (!la.TryGetProperty("activation", out var activations) || activations.ValueKind != JsonValueKind.Array || activations.GetArrayLength() != LayerCount)
            return "A2 has one activation per layer";
        foreach (var a in activations.EnumerateArray())
        {
            if (a.ValueKind != JsonValueKind.Object || GetString(a, "type") != "LeakyReLU")
                return "A2 uses LeakyReLU activations";
            if (!a.TryGetProperty("negative_slope", out var s) || s.ValueKind != JsonValueKind.Number || MathF.Abs(s.GetSingle() - LeakySlope) > 1e-7f)
                return "A2 uses a LeakyReLU slope of 0.01";
        }

        if (la.TryGetProperty("gating_mode", out var gating) && gating.ValueKind != JsonValueKind.Null)
        {
            if (gating.ValueKind != JsonValueKind.Array || gating.GetArrayLength() != LayerCount)
                return "gating_mode does not match A2";
            foreach (var g in gating.EnumerateArray())
            {
                if (g.ValueKind != JsonValueKind.String || g.GetString() != "none")
                    return "A2 does not use gating";
            }
        }
        if (la.TryGetProperty("gated", out var gated) && gated.ValueKind == JsonValueKind.True)
            return "A2 does not use gating";

        if (la.TryGetProperty("secondary_activation", out var secondary) && secondary.ValueKind != JsonValueKind.Null)
        {
            if (secondary.ValueKind != JsonValueKind.Array || secondary.GetArrayLength() != LayerCount)
                return "secondary_activation does not match A2";
            foreach (var s in secondary.EnumerateArray())
            {
                if (s.ValueKind != JsonValueKind.Null)
                    return "A2 has no secondary activations";
            }
        }

        if (la.TryGetProperty("head1x1", out var head1x1) && head1x1.ValueKind == JsonValueKind.Object && GetBool(head1x1, "active", false))
            return "A2 does not use head1x1";

        if (!la.TryGetProperty("layer1x1", out var layer1x1) || layer1x1.ValueKind != JsonValueKind.Object
            || !GetBool(layer1x1, "active", false) || GetInt(layer1x1, "groups", 1) != 1)
            return "A2 uses an ungrouped layer1x1";

        if (!la.TryGetProperty("head", out var head) || head.ValueKind != JsonValueKind.Object
            || GetInt(head, "out_channels", 0) != 1 || GetInt(head, "kernel_size", 0) != HeadKernelSize
            || GetInt(head, "head_dilation", 1) != 1 || !GetBool(head, "bias", false))
            return "the layer-array head does not match A2 (1 output, kernel 16, bias)";

        foreach (var key in FilmKeys)
        {
            if (!FilmInactive(la, key))
                return $"A2 does not use FiLM ({key})";
        }

        if (GetInt(la, "groups_input", 1) != 1 || GetInt(la, "groups_input_mixin", 1) != 1)
            return "A2 does not use grouped convolutions";
        if (!IsAbsentOrNull(la, "slimmable"))
            return "slimmable WaveNet layers are not A2";

        channels = ch;
        return null;
    }

    /// <summary>The number of weights an A2 model with this many channels has.</summary>
    public static int WeightCount(int channels)
    {
        var total = channels; // rechannel
        foreach (var k in KernelSizes)
            total += k * channels * channels + channels + channels + channels * channels + channels;
        return total + HeadKernelSize * channels + 1 + 1; // head weights, head bias, head scale
    }

    private static bool FilmInactive(JsonElement layerArray, string key)
    {
        if (!layerArray.TryGetProperty(key, out var film))
            return true;
        return film.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.False => true,
            JsonValueKind.Object => !GetBool(film, "active", false),
            _ => false,
        };
    }

    private static bool IntArrayEquals(JsonElement obj, string name, ReadOnlySpan<int> expected)
    {
        if (!obj.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array || array.GetArrayLength() != expected.Length)
            return false;
        var i = 0;
        foreach (var e in array.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Number || !e.TryGetInt32(out var v) || v != expected[i++])
                return false;
        }
        return true;
    }

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

    private static string? GetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
