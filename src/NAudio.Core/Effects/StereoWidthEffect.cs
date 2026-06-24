using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Adjusts stereo width via mid/side processing. <see cref="Width"/> of 0 collapses
/// to mono, 1 leaves the image unchanged, and values above 1 widen it. Width
/// changes are smoothed. Only stereo signals are affected; other channel counts
/// pass through unchanged.
/// </summary>
public sealed class StereoWidthEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Width", "", 0f, 2f, () => Width, v => Width = v)
    };

    private readonly ParameterSmoother width = new();
    private float widthValue = 1f;

    /// <summary>
    /// Stereo width: 0 = mono, 1 = unchanged, &gt;1 = wider (2 is a typical maximum).
    /// </summary>
    public float Width
    {
        get => widthValue;
        set
        {
            widthValue = value < 0f ? 0f : value;
            width.SetTarget(widthValue);
        }
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        width.Configure(format.SampleRate);
        width.Reset(widthValue);
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        if (Channels != 2)
            return;

        for (var i = 0; i + 1 < buffer.Length; i += 2)
        {
            var w = width.Process();
            var mid = (buffer[i] + buffer[i + 1]) * 0.5f;
            var side = (buffer[i] - buffer[i + 1]) * 0.5f * w;
            buffer[i] = mid + side;
            buffer[i + 1] = mid - side;
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        width.Reset(widthValue);
    }
}
