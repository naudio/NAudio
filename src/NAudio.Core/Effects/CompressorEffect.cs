using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// How a dynamics processor measures signal level.
/// </summary>
public enum DetectorMode
{
    /// <summary>Instantaneous absolute peak.</summary>
    Peak,
    /// <summary>Smoothed root-mean-square (more like perceived loudness).</summary>
    Rms
}

/// <summary>
/// Feed-forward dynamic-range compressor with a soft knee, peak or RMS detection,
/// and attack/release ballistics. Detection is channel-linked (one gain for all
/// channels) so the stereo image is preserved. Exposes the live gain reduction for
/// metering.
/// </summary>
public sealed class CompressorEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Threshold", "dB", -60f, 0f, () => ThresholdDb, v => ThresholdDb = v),
        EffectParameter.Continuous("Ratio", "", 1f, 20f, () => Ratio, v => Ratio = v),
        EffectParameter.Continuous("Knee", "dB", 0f, 24f, () => KneeDb, v => KneeDb = v),
        EffectParameter.Continuous("Attack", "ms", 0.1f, 200f, () => AttackMs, v => AttackMs = v),
        EffectParameter.Continuous("Release", "ms", 5f, 1000f, () => ReleaseMs, v => ReleaseMs = v),
        EffectParameter.Continuous("Make-up", "dB", 0f, 24f, () => MakeUpGainDb, v => MakeUpGainDb = v),
        EffectParameter.Choice("Detector", new[] { "Peak", "RMS" }, () => (int)Detector, i => Detector = (DetectorMode)i),
        EffectParameter.Meter("Gain Reduction", "dB", 0f, 24f, () => GainReductionDb)
    };

    private EnvelopeFollower reductionFollower;
    private float msEnvelope;
    private float rmsCoefficient;
    private float rmsWindowMs = 10f;
    private float attackMs = 10f;
    private float releaseMs = 100f;

    /// <summary>Threshold in dBFS above which compression starts. Default -18 dB.</summary>
    public float ThresholdDb { get; set; } = -18f;

    /// <summary>Compression ratio (≥ 1). 4 means 4:1. Default 4.</summary>
    public float Ratio { get; set; } = 4f;

    /// <summary>Soft-knee width in dB (0 = hard knee). Default 6 dB.</summary>
    public float KneeDb { get; set; } = 6f;

    /// <summary>Make-up gain in dB applied after compression. Default 0 dB.</summary>
    public float MakeUpGainDb { get; set; }

    /// <summary>Level detector mode. Default <see cref="DetectorMode.Peak"/>.</summary>
    public DetectorMode Detector { get; set; } = DetectorMode.Peak;

    /// <summary>RMS averaging window in milliseconds (used when <see cref="Detector"/> is RMS). Default 10 ms.</summary>
    public float RmsWindowMs
    {
        get => rmsWindowMs;
        set
        {
            rmsWindowMs = value;
            if (WaveFormat != null)
                rmsCoefficient = 1f - MathF.Exp(-1f / (rmsWindowMs * 0.001f * SampleRate));
        }
    }

    /// <summary>Attack time in milliseconds. Default 10 ms.</summary>
    public float AttackMs
    {
        get => attackMs;
        set
        {
            attackMs = value;
            if (reductionFollower != null)
                reductionFollower.AttackMilliseconds = value;
        }
    }

    /// <summary>Release time in milliseconds. Default 100 ms.</summary>
    public float ReleaseMs
    {
        get => releaseMs;
        set
        {
            releaseMs = value;
            if (reductionFollower != null)
                reductionFollower.ReleaseMilliseconds = value;
        }
    }

    /// <summary>The most recent gain reduction in dB (≥ 0), for metering.</summary>
    public float GainReductionDb { get; private set; }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        reductionFollower = new EnvelopeFollower(attackMs, releaseMs, format.SampleRate);
        rmsCoefficient = 1f - MathF.Exp(-1f / (rmsWindowMs * 0.001f * format.SampleRate));
        msEnvelope = 0f;
        GainReductionDb = 0f;
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var ratio = Ratio < 1f ? 1f : Ratio;
        var knee = KneeDb < 0f ? 0f : KneeDb;
        var makeUp = DbToLinear(MakeUpGainDb);

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            float key;
            if (Detector == DetectorMode.Rms)
            {
                var sumSquares = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var s = buffer[i + ch];
                    sumSquares += s * s;
                }
                var meanSquare = sumSquares / channels;
                msEnvelope += rmsCoefficient * (meanSquare - msEnvelope);
                key = MathF.Sqrt(msEnvelope);
            }
            else
            {
                key = 0f;
                for (var ch = 0; ch < channels; ch++)
                {
                    var a = MathF.Abs(buffer[i + ch]);
                    if (a > key)
                        key = a;
                }
            }

            var over = LinearToDb(key) - ThresholdDb;

            // Soft-knee gain computer → desired reduction in dB (≥ 0).
            float reduction;
            if (2f * over <= -knee)
            {
                reduction = 0f;
            }
            else if (2f * over >= knee || knee == 0f)
            {
                reduction = over - over / ratio;
            }
            else
            {
                var x = over + knee * 0.5f;
                reduction = (1f - 1f / ratio) * x * x / (2f * knee);
            }

            var smoothed = reductionFollower.ProcessRectified(reduction);
            GainReductionDb = smoothed;
            var gain = DbToLinear(-smoothed) * makeUp;

            for (var ch = 0; ch < channels; ch++)
                buffer[i + ch] *= gain;
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        reductionFollower?.Reset();
        msEnvelope = 0f;
        GainReductionDb = 0f;
    }

    private static float LinearToDb(float linear) => 20f * MathF.Log10(linear < 1e-9f ? 1e-9f : linear);

    private static float DbToLinear(float db) => MathF.Pow(10f, db * (1f / 20f));
}
