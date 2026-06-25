using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Removes DC offset and sub-sonic rumble with a first-order high-pass
/// (<c>y[n] = x[n] - x[n-1] + R·y[n-1]</c>), applied independently per channel.
/// Cheap, phase-light, and a sensible first link in a voice or feedback chain.
/// </summary>
public sealed class DcBlockerEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Cut-off", "Hz", 1f, 200f, () => CutoffFrequency, v => CutoffFrequency = v)
    };

    private float[] x1 = Array.Empty<float>();
    private float[] y1 = Array.Empty<float>();
    private float r = 0.995f;
    private float cutoffHz = 20f;

    /// <summary>
    /// Approximate -3 dB cut-off in Hz (default 20 Hz). Negative values are clamped to 0.
    /// </summary>
    public float CutoffFrequency
    {
        get => cutoffHz;
        set
        {
            cutoffHz = value < 0f ? 0f : value;
            RecomputePole();
        }
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        x1 = new float[format.Channels];
        y1 = new float[format.Channels];
        RecomputePole();
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                var x = buffer[i + ch];
                var y = x - x1[ch] + r * y1[ch];
                x1[ch] = x;
                y1[ch] = DenormalGuard.Flush(y);
                buffer[i + ch] = y;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        Array.Clear(x1);
        Array.Clear(y1);
    }

    private void RecomputePole()
    {
        if (SampleRateOrZero <= 0)
            return;
        // Pole radius for a first-order high-pass at the requested cut-off.
        var value = 1f - 2f * MathF.PI * cutoffHz / SampleRateOrZero;
        r = value < 0f ? 0f : value > 0.99999f ? 0.99999f : value;
    }

    private int SampleRateOrZero => WaveFormat == null ? 0 : SampleRate;
}
