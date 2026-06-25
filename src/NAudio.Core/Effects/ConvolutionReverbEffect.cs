using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Convolution reverb: convolves the signal with an impulse response (a recorded
/// space or a synthetic IR) using partitioned FFT convolution, so the cost is
/// bounded regardless of IR length. Supply a mono IR (applied to every channel) or
/// one IR per channel. With no IR set the effect is a transparent pass-through.
/// Reports its partition latency via <see cref="AudioEffect.LatencySamples"/>.
/// </summary>
public sealed class ConvolutionReverbEffect : AudioEffect
{
    private float[][] impulseResponses;
    private PartitionedConvolver[] convolvers = Array.Empty<PartitionedConvolver>();
    private int partitionSize = 256;

    /// <summary>
    /// Creates a convolution reverb with a sensible default wet/dry mix.
    /// </summary>
    public ConvolutionReverbEffect()
    {
        Mix = 0.3f;
    }

    /// <summary>
    /// FFT partition size (power of two, e.g. 128–1024). Larger lowers CPU at the
    /// cost of latency. Default 256.
    /// </summary>
    public int PartitionSize
    {
        get => partitionSize;
        set
        {
            if (value < 1 || (value & (value - 1)) != 0)
                throw new ArgumentException("Partition size must be a power of two.", nameof(value));
            partitionSize = value;
            if (WaveFormat != null)
                BuildConvolvers();
        }
    }

    /// <inheritdoc />
    public override int LatencySamples => convolvers.Length > 0 ? partitionSize : 0;

    /// <summary>
    /// Sets a single impulse response applied to every channel.
    /// </summary>
    public void SetImpulseResponse(float[] impulseResponse)
    {
        ArgumentNullException.ThrowIfNull(impulseResponse);
        impulseResponses = new[] { impulseResponse };
        if (WaveFormat != null)
            BuildConvolvers();
    }

    /// <summary>
    /// Sets one impulse response per channel. The array length should match the
    /// configured channel count.
    /// </summary>
    public void SetImpulseResponse(float[][] perChannelImpulseResponses)
    {
        ArgumentNullException.ThrowIfNull(perChannelImpulseResponses);
        impulseResponses = perChannelImpulseResponses;
        if (WaveFormat != null)
            BuildConvolvers();
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format) => BuildConvolvers();

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        if (convolvers.Length == 0)
            return;
        var channels = Channels;
        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            for (var ch = 0; ch < channels; ch++)
                buffer[i + ch] = convolvers[ch].Process(buffer[i + ch]);
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        foreach (var convolver in convolvers)
            convolver.Reset();
    }

    private void BuildConvolvers()
    {
        if (impulseResponses == null || WaveFormat == null)
        {
            convolvers = Array.Empty<PartitionedConvolver>();
            return;
        }

        var channels = Channels;
        convolvers = new PartitionedConvolver[channels];
        for (var ch = 0; ch < channels; ch++)
        {
            var ir = impulseResponses.Length == 1
                ? impulseResponses[0]
                : impulseResponses[Math.Min(ch, impulseResponses.Length - 1)];
            convolvers[ch] = new PartitionedConvolver(ir, partitionSize);
        }
    }
}
