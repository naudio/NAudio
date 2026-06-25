using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Applies a gain (volume) to the signal, expressed in decibels or as a linear
/// multiplier. Gain changes are smoothed so automating the level never clicks.
/// </summary>
public sealed class GainEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Gain", "dB", -60f, 24f, () => GainDb, v => GainDb = v)
    };

    private readonly ParameterSmoother gain = new();
    private float linearGain = 1f;

    /// <summary>
    /// Linear gain multiplier (1 = unity). Setting this updates <see cref="GainDb"/>.
    /// </summary>
    public float LinearGain
    {
        get => linearGain;
        set
        {
            linearGain = value < 0f ? 0f : value;
            gain.SetTarget(linearGain);
        }
    }

    /// <summary>
    /// Gain in decibels (0 dB = unity). Setting this updates <see cref="LinearGain"/>.
    /// </summary>
    public float GainDb
    {
        get => 20f * MathF.Log10(linearGain <= 0f ? 1e-9f : linearGain);
        set => LinearGain = MathF.Pow(10f, value / 20f);
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        gain.Configure(format.SampleRate);
        gain.Reset(linearGain);
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        if (gain.IsSettled)
        {
            if (gain.Current != 1f)
                TensorPrimitives.Multiply(buffer, gain.Current, buffer);
            return;
        }

        for (var i = 0; i < buffer.Length; i++)
            buffer[i] *= gain.Process();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        gain.Reset(linearGain);
    }
}
