using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Effects;
using NAudio.Wave;

namespace NAudioWpfDemo.RealtimeEffectsDemo.CustomEffects;

/// <summary>
/// Cascadable high-pass + low-pass filter with selectable slope, built directly
/// from <see cref="BiQuadFilter"/> and <see cref="CrossfadingBiQuadFilter"/>.
/// Demonstrates how to compose a custom effect from the low-level filter
/// primitives with click-free retune. 12 dB/oct uses one biquad per side; 24
/// dB/oct cascades two biquads with Butterworth Q values.
/// </summary>
public sealed class FilterEffect : AudioEffect, IParameterized
{
    private const float ButterworthQ12 = 0.707106781f;
    private const float ButterworthQ24Stage1 = 0.541196100f;
    private const float ButterworthQ24Stage2 = 1.306562965f;
    private const int CrossfadeMs = 10;

    private CrossfadingBiQuadFilter[,] hpf; // [channel, stage 0|1]
    private CrossfadingBiQuadFilter[,] lpf;
    private int crossfadeSamples = 1;
    private float hpfCutoffHz = 80f;
    private float lpfCutoffHz = 12000f;
    private bool hpfEnabled = true;
    private bool lpfEnabled = true;
    private int slopeIndex; // 0 = 12 dB/oct, 1 = 24 dB/oct

    private IReadOnlyList<EffectParameter> parameters;

    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Toggle("HPF On", () => hpfEnabled, v => hpfEnabled = v),
        EffectParameter.Continuous("HPF Cutoff", "Hz", 20f, 2000f,
            () => hpfCutoffHz, v => { hpfCutoffHz = v; RebuildHpf(); }),
        EffectParameter.Toggle("LPF On", () => lpfEnabled, v => lpfEnabled = v),
        EffectParameter.Continuous("LPF Cutoff", "Hz", 200f, 20000f,
            () => lpfCutoffHz, v => { lpfCutoffHz = v; RebuildLpf(); }),
        EffectParameter.Choice("Slope", new[] { "12 dB/oct", "24 dB/oct" },
            () => slopeIndex, i => { slopeIndex = i; RebuildHpf(); RebuildLpf(); })
    };

    protected override void OnConfigure(WaveFormat format)
    {
        crossfadeSamples = Math.Max(1, format.SampleRate * CrossfadeMs / 1000);
        hpf = new CrossfadingBiQuadFilter[format.Channels, 2];
        lpf = new CrossfadingBiQuadFilter[format.Channels, 2];
        var (q1, q2) = SlopeQs();
        for (var ch = 0; ch < format.Channels; ch++)
        {
            hpf[ch, 0] = MakeStage(BiQuadFilter.HighPassFilter, format.SampleRate, hpfCutoffHz, q1);
            hpf[ch, 1] = MakeStage(BiQuadFilter.HighPassFilter, format.SampleRate, hpfCutoffHz, q2);
            lpf[ch, 0] = MakeStage(BiQuadFilter.LowPassFilter, format.SampleRate, lpfCutoffHz, q1);
            lpf[ch, 1] = MakeStage(BiQuadFilter.LowPassFilter, format.SampleRate, lpfCutoffHz, q2);
        }
    }

    protected override void ProcessBlock(Span<float> buffer)
    {
        if (hpf == null)
            return;
        var channels = Channels;
        var slope24 = slopeIndex == 1;
        var hp = hpfEnabled;
        var lp = lpfEnabled;
        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            for (var ch = 0; ch < channels; ch++)
            {
                var s = buffer[i + ch];
                if (hp)
                {
                    s = hpf[ch, 0].Transform(s);
                    if (slope24) s = hpf[ch, 1].Transform(s);
                }
                if (lp)
                {
                    s = lpf[ch, 0].Transform(s);
                    if (slope24) s = lpf[ch, 1].Transform(s);
                }
                buffer[i + ch] = s;
            }
        }
    }

    public override void Reset()
    {
        base.Reset();
        if (hpf == null)
            return;
        for (var ch = 0; ch < Channels; ch++)
        {
            hpf[ch, 0].Reset(); hpf[ch, 1].Reset();
            lpf[ch, 0].Reset(); lpf[ch, 1].Reset();
        }
    }

    private CrossfadingBiQuadFilter MakeStage(
        Func<float, float, float, BiQuadFilter> factory, int sr, float cutoff, float q)
        => new CrossfadingBiQuadFilter(factory(sr, cutoff, q), factory(sr, cutoff, q), crossfadeSamples);

    private (float Stage1, float Stage2) SlopeQs() => slopeIndex == 1
        ? (ButterworthQ24Stage1, ButterworthQ24Stage2)
        : (ButterworthQ12, ButterworthQ12); // stage 2 is unused for 12 dB/oct

    private void RebuildHpf()
    {
        if (hpf == null)
            return;
        var (q1, q2) = SlopeQs();
        for (var ch = 0; ch < Channels; ch++)
        {
            hpf[ch, 0].ReplaceStandby(BiQuadFilter.HighPassFilter(SampleRate, hpfCutoffHz, q1));
            hpf[ch, 0].BeginCrossfade();
            hpf[ch, 1].ReplaceStandby(BiQuadFilter.HighPassFilter(SampleRate, hpfCutoffHz, q2));
            hpf[ch, 1].BeginCrossfade();
        }
    }

    private void RebuildLpf()
    {
        if (lpf == null)
            return;
        var (q1, q2) = SlopeQs();
        for (var ch = 0; ch < Channels; ch++)
        {
            lpf[ch, 0].ReplaceStandby(BiQuadFilter.LowPassFilter(SampleRate, lpfCutoffHz, q1));
            lpf[ch, 0].BeginCrossfade();
            lpf[ch, 1].ReplaceStandby(BiQuadFilter.LowPassFilter(SampleRate, lpfCutoffHz, q2));
            lpf[ch, 1].BeginCrossfade();
        }
    }
}
