using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Lo-fi bit crusher: reduces amplitude resolution (bit depth) and, optionally,
/// the effective sample rate by sample-and-hold decimation down to a chosen
/// target rate. The decimation deliberately aliases (the classic gritty
/// bitcrusher character); enable <see cref="Smoothing"/> for a softer, more
/// "vintage" tone. Per-channel state keeps a stereo image coherent.
/// </summary>
public sealed class BitCrusherEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    // 0 = off (no sample-rate reduction); the rest are target rates in Hz.
    private static readonly int[] RateOptions =
        { 0, 32000, 22050, 16000, 11025, 8000, 6000, 4000 };

    private static readonly string[] RateLabels =
        { "Off", "32 kHz", "22.05 kHz", "16 kHz", "11.025 kHz", "8 kHz", "6 kHz", "4 kHz" };

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Bit Depth", "bits", 1f, 32f, () => BitDepth, v => BitDepth = (int)MathF.Round(v)),
        EffectParameter.Choice("Sample Rate", RateLabels, RateChoiceIndex, i => TargetSampleRate = RateOptions[i]),
        EffectParameter.Toggle("Smooth", () => Smoothing, v => Smoothing = v)
    };

    private float[] held = Array.Empty<float>();
    private float[] smoothState = Array.Empty<float>();
    private int counter;
    private int bitDepth = 8;
    private int targetSampleRate = 22050;

    /// <summary>Quantisation resolution in bits, 1–32. Default 8.</summary>
    public int BitDepth
    {
        get => bitDepth;
        set => bitDepth = value < 1 ? 1 : value > 32 ? 32 : value;
    }

    /// <summary>
    /// Target effective sample rate in Hz. The signal is sample-and-hold
    /// decimated to the nearest integer division of the host rate. 0 (or any
    /// value at/above the host rate) disables sample-rate reduction. Default
    /// 22050 Hz.
    /// </summary>
    public int TargetSampleRate
    {
        get => targetSampleRate;
        set => targetSampleRate = value < 0 ? 0 : value;
    }

    /// <summary>
    /// When true, a one-pole low-pass softens the decimated signal (less
    /// aliasing, smoother "vintage" tone) instead of the raw gritty
    /// sample-and-hold. Default false.
    /// </summary>
    public bool Smoothing { get; set; }

    private int RateChoiceIndex()
    {
        for (var i = 0; i < RateOptions.Length; i++)
            if (RateOptions[i] == targetSampleRate)
                return i;
        return 0; // a custom rate not in the list ⇒ show as "Off" in the generic UI
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        held = new float[format.Channels];
        smoothState = new float[format.Channels];
        counter = 0;
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var step = 2f / (1L << bitDepth); // 2^bits quantisation levels over [-1, 1]
        var hold = targetSampleRate <= 0 || targetSampleRate >= SampleRate
            ? 1
            : Math.Max(1, (int)MathF.Round((float)SampleRate / targetSampleRate));

        var smoothing = Smoothing && hold > 1;
        var oneMinusA = 0f;
        if (smoothing)
        {
            var fc = MathF.Min(targetSampleRate * 0.45f, SampleRate * 0.45f);
            oneMinusA = 1f - MathF.Exp(-2f * MathF.PI * fc / SampleRate);
        }

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var refresh = counter == 0;
            for (var ch = 0; ch < channels; ch++)
            {
                if (refresh)
                {
                    var x = buffer[i + ch];
                    if (x < -1f) x = -1f;
                    else if (x > 1f) x = 1f;
                    held[ch] = MathF.Round(x / step) * step;
                }

                if (smoothing)
                {
                    smoothState[ch] = DenormalGuard.Flush(
                        smoothState[ch] + oneMinusA * (held[ch] - smoothState[ch]));
                    buffer[i + ch] = smoothState[ch];
                }
                else
                {
                    buffer[i + ch] = held[ch];
                }
            }
            if (++counter >= hold)
                counter = 0;
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        Array.Clear(held);
        Array.Clear(smoothState);
        counter = 0;
    }
}
