using System;
using System.Collections.Generic;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.RealtimeEffectsDemo.CustomEffects;

/// <summary>
/// Fixed 3-band (low / mid / high) compressor at 250 Hz and 2.5 kHz crossovers,
/// composed by containment on <see cref="MultibandCompressorEffect"/>. Per-band
/// threshold and ratio are exposed with per-band gain-reduction meters; attack,
/// release and make-up are fixed at sensible per-band defaults so the panel
/// stays compact. Lives in the WPF demo, not the toolkit.
/// </summary>
public sealed class ThreeBandCompressorEffect : AudioEffect, IParameterized
{
    private static readonly string[] BandNames = { "Low", "Mid", "High" };
    private static readonly (float Attack, float Release)[] BandTiming =
    {
        (30f, 200f), // low: slow
        (10f, 100f), // mid: medium
        (3f, 50f)    // high: fast
    };

    private readonly MultibandCompressorEffect inner;
    private IReadOnlyList<EffectParameter> parameters;

    public ThreeBandCompressorEffect()
    {
        inner = new MultibandCompressorEffect(250f, 2500f) { Mix = 1f };
        for (var b = 0; b < BandNames.Length; b++)
        {
            inner.Bands[b].AttackMs = BandTiming[b].Attack;
            inner.Bands[b].ReleaseMs = BandTiming[b].Release;
            inner.Bands[b].ThresholdDb = -18f;
            inner.Bands[b].Ratio = 3f;
        }
    }

    public IReadOnlyList<EffectParameter> Parameters
    {
        get
        {
            if (parameters != null)
                return parameters;
            var list = new List<EffectParameter>(9);
            for (var b = 0; b < BandNames.Length; b++)
            {
                var band = inner.Bands[b];
                var name = BandNames[b];
                list.Add(EffectParameter.Continuous($"{name} Threshold", "dB", -60f, 0f,
                    () => band.ThresholdDb, v => band.ThresholdDb = v));
                list.Add(EffectParameter.Continuous($"{name} Ratio", ":1", 1f, 20f,
                    () => band.Ratio, v => band.Ratio = v));
                list.Add(EffectParameter.Meter($"{name} GR", "dB", 0f, 24f,
                    () => band.GainReductionDb));
            }
            return parameters = list;
        }
    }

    protected override void OnConfigure(WaveFormat format) => inner.Configure(format);

    protected override void ProcessBlock(Span<float> buffer) => inner.Process(buffer);

    public override void Reset()
    {
        base.Reset();
        inner.Reset();
    }
}
