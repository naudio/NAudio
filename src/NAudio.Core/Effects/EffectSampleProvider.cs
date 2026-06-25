using System;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Adapts an <see cref="IAudioEffect"/> into the pull-model
/// <see cref="ISampleProvider"/> pipeline: reads from a source and processes the
/// samples through the effect in place.
/// </summary>
public sealed class EffectSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly IAudioEffect effect;

    /// <summary>
    /// Wraps <paramref name="source"/> so its output is processed by
    /// <paramref name="effect"/>. The effect is configured with the source's format.
    /// </summary>
    public EffectSampleProvider(ISampleProvider source, IAudioEffect effect)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(effect);
        this.source = source;
        this.effect = effect;
        effect.Configure(source.WaveFormat);
    }

    /// <summary>
    /// The effect being applied.
    /// </summary>
    public IAudioEffect Effect => effect;

    /// <inheritdoc />
    public WaveFormat WaveFormat => source.WaveFormat;

    /// <inheritdoc />
    public int Read(Span<float> buffer)
    {
        var samplesRead = source.Read(buffer);
        if (samplesRead > 0)
            effect.Process(buffer.Slice(0, samplesRead));
        return samplesRead;
    }
}
