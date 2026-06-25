using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Noise gate / downward expander. Below the threshold the signal is attenuated;
/// with a high <see cref="Ratio"/> it behaves as a hard gate, with a lower ratio as
/// a gentle expander. Includes hysteresis (separate open/close points), a hold time
/// to avoid chatter, and independent attack/release. Detection is channel-linked.
/// </summary>
public sealed class GateEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Threshold", "dB", -80f, 0f, () => ThresholdDb, v => ThresholdDb = v),
        EffectParameter.Continuous("Range", "dB", -100f, 0f, () => RangeDb, v => RangeDb = v),
        EffectParameter.Continuous("Ratio", "", 1f, 20f, () => Ratio, v => Ratio = v),
        EffectParameter.Continuous("Hysteresis", "dB", 0f, 12f, () => HysteresisDb, v => HysteresisDb = v),
        EffectParameter.Continuous("Attack", "ms", 0.1f, 50f, () => AttackMs, v => AttackMs = v),
        EffectParameter.Continuous("Hold", "ms", 0f, 500f, () => HoldMs, v => HoldMs = v),
        EffectParameter.Continuous("Release", "ms", 5f, 1000f, () => ReleaseMs, v => ReleaseMs = v),
        EffectParameter.Meter("Gain Reduction", "dB", 0f, 100f, () => GainReductionDb)
    };

    private bool open;
    private int holdRemaining;
    private int holdSamples;
    private float gainDb;
    private float attackCoefficient;
    private float releaseCoefficient;
    private float attackMs = 1f;
    private float holdMs = 10f;
    private float releaseMs = 100f;

    /// <summary>Level (dBFS) below which the gate starts to close. Default -40 dB.</summary>
    public float ThresholdDb { get; set; } = -40f;

    /// <summary>Maximum attenuation in dB when fully closed (negative). Default -80 dB.</summary>
    public float RangeDb { get; set; } = -80f;

    /// <summary>Expansion ratio; higher is more gate-like. Default 10.</summary>
    public float Ratio { get; set; } = 10f;

    /// <summary>Hysteresis in dB: the gate re-closes this far below the threshold. Default 3 dB.</summary>
    public float HysteresisDb { get; set; } = 3f;

    /// <summary>Attack (open) time in milliseconds. Default 1 ms.</summary>
    public float AttackMs
    {
        get => attackMs;
        set { attackMs = value; RecomputeTimes(); }
    }

    /// <summary>Hold time in milliseconds before the gate is allowed to close. Default 10 ms.</summary>
    public float HoldMs
    {
        get => holdMs;
        set { holdMs = value; RecomputeTimes(); }
    }

    /// <summary>Release (close) time in milliseconds. Default 100 ms.</summary>
    public float ReleaseMs
    {
        get => releaseMs;
        set { releaseMs = value; RecomputeTimes(); }
    }

    /// <summary>The most recent gain reduction in dB (≥ 0), for metering.</summary>
    public float GainReductionDb { get; private set; }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        open = false;
        holdRemaining = 0;
        gainDb = RangeDb;
        GainReductionDb = -RangeDb;
        RecomputeTimes();
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var openThreshold = ThresholdDb;
        var closeThreshold = ThresholdDb - MathF.Abs(HysteresisDb);
        var maxReduction = MathF.Abs(RangeDb);
        var ratio = Ratio < 1f ? 1f : Ratio;

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var peak = 0f;
            for (var ch = 0; ch < channels; ch++)
            {
                var a = MathF.Abs(buffer[i + ch]);
                if (a > peak)
                    peak = a;
            }
            var levelDb = 20f * MathF.Log10(peak < 1e-9f ? 1e-9f : peak);

            if (levelDb >= openThreshold)
                open = true;
            else if (levelDb < closeThreshold)
                open = false;

            float targetReduction;
            if (open)
            {
                holdRemaining = holdSamples;
                targetReduction = 0f;
            }
            else if (holdRemaining > 0)
            {
                holdRemaining--;
                targetReduction = 0f;
            }
            else
            {
                targetReduction = (openThreshold - levelDb) * (ratio - 1f);
                if (targetReduction < 0f)
                    targetReduction = 0f;
                else if (targetReduction > maxReduction)
                    targetReduction = maxReduction;
            }

            var targetGainDb = -targetReduction;
            // Opening = gain rising → attack; closing = gain falling → release.
            var coefficient = targetGainDb > gainDb ? attackCoefficient : releaseCoefficient;
            gainDb = targetGainDb + coefficient * (gainDb - targetGainDb);
            GainReductionDb = -gainDb;

            var gain = MathF.Pow(10f, gainDb * (1f / 20f));
            for (var ch = 0; ch < channels; ch++)
                buffer[i + ch] *= gain;
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        open = false;
        holdRemaining = 0;
        gainDb = RangeDb;
        GainReductionDb = -RangeDb;
    }

    private void RecomputeTimes()
    {
        if (WaveFormat == null)
            return;
        var sampleRate = SampleRate;
        attackCoefficient = CoefficientFor(attackMs, sampleRate);
        releaseCoefficient = CoefficientFor(releaseMs, sampleRate);
        holdSamples = Math.Max(0, (int)MathF.Round(holdMs * 0.001f * sampleRate));
    }

    private static float CoefficientFor(float milliseconds, int sampleRate)
    {
        if (milliseconds <= 0f)
            return 0f;
        return MathF.Exp(-1f / (milliseconds * 0.001f * sampleRate));
    }
}
