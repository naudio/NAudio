using System;
using System.Collections.Generic;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.RealtimeEffectsDemo.CustomEffects;

/// <summary>
/// A fixed 7-band graphic equaliser at the classic BOSS GE-7 frequencies,
/// composed by containment on the toolkit's <see cref="Equalizer"/>. Demonstrates
/// how to wrap a primitive that takes a dynamic band list as a fixed-parameter
/// effect the auto-panel can render. Lives in the WPF demo, not the toolkit.
/// </summary>
public sealed class SevenBandEqEffect : AudioEffect, IParameterized
{
    private static readonly float[] CentreFrequencies =
        { 100f, 200f, 400f, 800f, 1600f, 3200f, 6400f };
    private const float BandQ = 1.4f;
    private const float GainRangeDb = 15f;

    private readonly Equalizer inner;
    private IReadOnlyList<EffectParameter> parameters;

    public SevenBandEqEffect()
    {
        var bands = new EqualizerBand[CentreFrequencies.Length];
        for (var i = 0; i < bands.Length; i++)
            bands[i] = EqualizerBand.Peaking(CentreFrequencies[i], BandQ, 0f);
        // The inner runs fully wet; the outer's AudioEffect base handles dry/wet
        // around our ProcessBlock so the two mixes don't compound.
        inner = new Equalizer(bands) { Mix = 1f };
    }

    public IReadOnlyList<EffectParameter> Parameters
    {
        get
        {
            if (parameters != null)
                return parameters;
            var list = new EffectParameter[CentreFrequencies.Length];
            for (var i = 0; i < CentreFrequencies.Length; i++)
            {
                var index = i;
                list[i] = EffectParameter.Continuous(FormatFrequency(CentreFrequencies[i]),
                    "dB", -GainRangeDb, GainRangeDb,
                    () => inner.Bands[index].GainDb,
                    v =>
                    {
                        inner.Bands[index].GainDb = v;
                        inner.Update(); // click-free retune via the crossfade
                    });
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

    private static string FormatFrequency(float hz)
        => hz >= 1000f ? $"{hz / 1000f:0.#} kHz" : $"{hz:0} Hz";
}
