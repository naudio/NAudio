using System;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace NAudio.Nam.Internal;

/// <summary>
/// The activation functions a NAM WaveNet layer can use, applied in place to a span
/// of values. The formulas match NeuralAmpModelerCore's (NAM/activations.h).
/// </summary>
internal sealed class Activation
{
    private enum Kind { Tanh, Hardtanh, FastTanh, ReLU, LeakyReLU, Sigmoid, SiLU, Hardswish, Softsign, LeakyHardtanh }

    private readonly Kind kind;
    private readonly float negativeSlope;
    private readonly float minValue = -1f, maxValue = 1f, minSlope, maxSlope;

    private Activation(Kind kind, float negativeSlope = 0.01f)
    {
        this.kind = kind;
        this.negativeSlope = negativeSlope;
    }

    private Activation(float minValue, float maxValue, float minSlope, float maxSlope)
    {
        kind = Kind.LeakyHardtanh;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.minSlope = minSlope;
        this.maxSlope = maxSlope;
    }

    public static Activation Sigmoid { get; } = new(Kind.Sigmoid);

    /// <summary>
    /// Parses an activation as a .nam file stores it: a name such as "Tanh", or an
    /// object with a "type" and optional parameters. Throws
    /// <see cref="NotSupportedException"/> for activations this library does not run.
    /// </summary>
    public static Activation Parse(JsonElement json)
    {
        string? name;
        if (json.ValueKind == JsonValueKind.String)
            name = json.GetString();
        else if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            name = type.GetString();
        else
            throw new FormatException("An activation must be a name or an object with a \"type\".");

        return name switch
        {
            "Tanh" => new Activation(Kind.Tanh),
            "Hardtanh" => new Activation(Kind.Hardtanh),
            "Fasttanh" => new Activation(Kind.FastTanh),
            "ReLU" => new Activation(Kind.ReLU),
            "LeakyReLU" => new Activation(Kind.LeakyReLU, GetFloat(json, "negative_slope", 0.01f)),
            "Sigmoid" => new Activation(Kind.Sigmoid),
            "SiLU" => new Activation(Kind.SiLU),
            "Hardswish" => new Activation(Kind.Hardswish),
            "Softsign" => new Activation(Kind.Softsign),
            "LeakyHardtanh" or "LeakyHardTanh" => new Activation(
                GetFloat(json, "min_val", -1f), GetFloat(json, "max_val", 1f),
                GetFloat(json, "min_slope", 0.01f), GetFloat(json, "max_slope", 0.01f)),
            _ => throw new NotSupportedException($"The '{name}' activation is not supported."),
        };
    }

    public void Apply(Span<float> x)
    {
        switch (kind)
        {
            case Kind.Tanh:
                TensorPrimitives.Tanh(x, x);
                break;
            case Kind.Sigmoid:
                TensorPrimitives.Sigmoid(x, x);
                break;
            case Kind.ReLU:
                TensorPrimitives.Max(x, 0f, x);
                break;
            case Kind.Hardtanh:
                TensorPrimitives.Max(x, -1f, x);
                TensorPrimitives.Min(x, 1f, x);
                break;
            case Kind.LeakyReLU:
                LeakyRelu(x, negativeSlope);
                break;
            case Kind.FastTanh:
                foreach (ref var v in x)
                    v = FastTanh(v);
                break;
            case Kind.SiLU:
                foreach (ref var v in x)
                    v *= 1f / (1f + MathF.Exp(-v));
                break;
            case Kind.Hardswish:
                foreach (ref var v in x)
                    v = v * Math.Clamp(v + 3f, 0f, 6f) * (1f / 6f);
                break;
            case Kind.Softsign:
                foreach (ref var v in x)
                    v /= 1f + MathF.Abs(v);
                break;
            case Kind.LeakyHardtanh:
                foreach (ref var v in x)
                {
                    if (v < minValue)
                        v = (v - minValue) * minSlope + minValue;
                    else if (v > maxValue)
                        v = (v - maxValue) * maxSlope + maxValue;
                }
                break;
        }
    }

    private static void LeakyRelu(Span<float> x, float slope)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var vectors = MemoryMarshal.Cast<float, Vector<float>>(x);
            var s = new Vector<float>(slope);
            for (var v = 0; v < vectors.Length; v++)
                vectors[v] = Vector.ConditionalSelect(Vector.GreaterThan(vectors[v], Vector<float>.Zero), vectors[v], vectors[v] * s);
            i = vectors.Length * Vector<float>.Count;
        }
        for (; i < x.Length; i++)
            x[i] = x[i] > 0f ? x[i] : slope * x[i];
    }

    // Rational approximation used by NeuralAmpModelerCore's "Fasttanh".
    private static float FastTanh(float x)
    {
        var ax = MathF.Abs(x);
        var x2 = x * x;
        return x * (2.45550750702956f + 2.45550750702956f * ax + (0.893229853513558f + 0.821226666969744f * ax) * x2)
            / (2.44506634652299f + (2.44506634652299f + x2) * MathF.Abs(x + 0.814642734961073f * x * ax));
    }

    private static float GetFloat(JsonElement json, string name, float fallback) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetSingle()
            : fallback;
}
