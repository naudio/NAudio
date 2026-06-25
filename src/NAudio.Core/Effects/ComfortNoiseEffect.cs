using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Adds a low level of gently shaped (low-passed) noise. Typically placed after a
/// gate or noise suppressor so processed silence is not unnaturally "dead" — a small
/// comfort-noise floor keeps the channel sounding live. Deterministic generator.
/// </summary>
public sealed class ComfortNoiseEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Level", "dB", -90f, -20f, () => LevelDb, v => LevelDb = v),
        EffectParameter.Continuous("Tone", "", 0f, 1f, () => Tone, v => Tone = v)
    };

    private const uint InitialRngState = 0x6D2B79F5;

    private uint rngState = InitialRngState;
    private float[] toneState = Array.Empty<float>();

    /// <summary>Noise level in dBFS. Default -60 dB.</summary>
    public float LevelDb { get; set; } = -60f;

    /// <summary>Tone shaping, 0 (full-band/bright) to 1 (heavily low-passed/dark). Default 0.6.</summary>
    public float Tone { get; set; } = 0.6f;

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        toneState = new float[format.Channels];
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var level = MathF.Pow(10f, LevelDb * (1f / 20f));
        // Tone maps to a one-pole low-pass coefficient (1 = no filtering).
        var coeff = 1f - Math.Clamp(Tone, 0f, 1f) * 0.97f;

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                var white = NextNoise();
                toneState[ch] += coeff * (white - toneState[ch]);
                buffer[i + ch] += toneState[ch] * level;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        Array.Clear(toneState);
        rngState = InitialRngState; // a true reset is reproducible
    }

    private float NextNoise()
    {
        // xorshift32 → uniform white noise in [-1, 1).
        rngState ^= rngState << 13;
        rngState ^= rngState >> 17;
        rngState ^= rngState << 5;
        return rngState / 2147483648f - 1f;
    }
}
