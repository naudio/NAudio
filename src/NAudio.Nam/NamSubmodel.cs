using System;
using NAudio.Nam.Internal;

namespace NAudio.Nam;

/// <summary>
/// One network inside a <see cref="NamModel"/>. An A2 file holds two: A2-Lite
/// (3 channels) for low-power hardware and A2-Full (8 channels) for desktops. A
/// file with a single network, such as an A1 WaveNet or an LSTM, has one submodel.
/// </summary>
public sealed class NamSubmodel
{
    private readonly float[] weights;
    private readonly Func<float[], NamDsp> createDsp;

    internal NamSubmodel(double maxSize, string architecture, bool isA2, int channels, int? sampleRate,
        NamModelMetadata metadata, float[] weights, Func<float[], NamDsp> createDsp)
    {
        MaxSize = maxSize;
        Architecture = architecture;
        IsA2 = isA2;
        Channels = channels;
        SampleRate = sampleRate;
        Metadata = metadata;
        this.weights = weights;
        this.createDsp = createDsp;
    }

    /// <summary>
    /// The upper bound of the size range (0 to 1) this submodel covers: a size picks
    /// the first submodel whose <see cref="MaxSize"/> is greater than it, with the last
    /// submodel covering everything above. See <see cref="NamModel.GetSubmodel"/>.
    /// </summary>
    public double MaxSize { get; }

    /// <summary>The network architecture: "WaveNet" or "LSTM".</summary>
    public string Architecture { get; }

    /// <summary>
    /// True for an Architecture 2 (A2) WaveNet, which runs on a dedicated fast path.
    /// </summary>
    public bool IsA2 { get; }

    /// <summary>
    /// The network's width: the channel count of a WaveNet's first layer array (3 for
    /// A2-Lite, 8 for A2-Full, 16 for A1 "standard"), or an LSTM's hidden size. CPU cost
    /// grows with roughly the square of this.
    /// </summary>
    public int Channels { get; }

    /// <summary>True for the A2-Lite network (3 channels).</summary>
    public bool IsA2Lite => IsA2 && Channels == 3;

    /// <summary>True for the A2-Full network (8 channels).</summary>
    public bool IsA2Full => IsA2 && Channels == 8;

    /// <summary>
    /// The sample rate the network was trained at, in Hz (normally 48000), or null if
    /// the file does not say. Audio must be processed at this rate to sound right.
    /// </summary>
    public int? SampleRate { get; }

    /// <summary>The number of trained weights in the network.</summary>
    public int WeightCount => weights.Length;

    /// <summary>The metadata stored with this network.</summary>
    public NamModelMetadata Metadata { get; }

    internal ReadOnlySpan<float> Weights => weights;

    internal NamDsp CreateDsp() => createDsp(weights);
}
