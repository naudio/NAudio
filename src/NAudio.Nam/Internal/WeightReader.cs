using System;

namespace NAudio.Nam.Internal;

/// <summary>Reads a model's flat weight list in order, checking it is the expected length.</summary>
internal ref struct WeightReader(ReadOnlySpan<float> weights)
{
    private readonly ReadOnlySpan<float> weights = weights;
    private int index;

    public float Next()
    {
        if (index >= weights.Length)
            throw new FormatException($"The model has too few weights for its configuration ({weights.Length} found).");
        return weights[index++];
    }

    public readonly void EnsureConsumed()
    {
        if (index != weights.Length)
            throw new FormatException($"The model has {weights.Length - index} more weights than its configuration uses.");
    }
}
