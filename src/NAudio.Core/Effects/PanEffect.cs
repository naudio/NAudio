using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Constant-power stereo panner. <see cref="Pan"/> ranges from -1 (hard left)
/// through 0 (centre) to +1 (hard right). Pan changes are smoothed. Only stereo
/// signals are affected; other channel counts pass through unchanged.
/// </summary>
public sealed class PanEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Pan", "", -1f, 1f, () => Pan, v => Pan = v)
    };

    private const float QuarterPi = MathF.PI / 4f;

    private readonly ParameterSmoother pan = new ParameterSmoother();
    private float panValue;

    /// <summary>
    /// Pan position from -1 (hard left) to +1 (hard right); 0 is centre.
    /// </summary>
    public float Pan
    {
        get => panValue;
        set
        {
            panValue = value < -1f ? -1f : value > 1f ? 1f : value;
            pan.SetTarget(panValue);
        }
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        pan.Configure(format.SampleRate);
        pan.Reset(panValue);
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        if (Channels != 2)
            return;

        for (var i = 0; i + 1 < buffer.Length; i += 2)
        {
            // theta sweeps 0..pi/2 across the stereo field; cos/sin keep L^2+R^2 constant.
            var theta = (pan.Process() + 1f) * QuarterPi;
            buffer[i] *= MathF.Cos(theta);
            buffer[i + 1] *= MathF.Sin(theta);
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        pan.Reset(panValue);
    }
}
