using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Flanger: a very short LFO-modulated delay with feedback, producing the classic
/// sweeping comb-filter "jet" effect. Feedback may be negative for an inverted
/// (hollow) comb.
/// </summary>
public sealed class FlangerEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Base Delay", "ms", 0.5f, 10f, () => BaseDelayMs, v => BaseDelayMs = v),
        EffectParameter.Continuous("Depth", "ms", 0f, 5f, () => DepthMs, v => DepthMs = v),
        EffectParameter.Continuous("Rate", "Hz", 0.05f, 5f, () => RateHz, v => RateHz = v),
        EffectParameter.Continuous("Feedback", "", -0.95f, 0.95f, () => Feedback, v => Feedback = v)
    };

    private Lfo lfo;
    private DelayLine[] lines = Array.Empty<DelayLine>();

    /// <summary>Centre delay in milliseconds. Default 1.5 ms.</summary>
    public float BaseDelayMs { get; set; } = 1.5f;

    /// <summary>Modulation depth in milliseconds. Default 2 ms.</summary>
    public float DepthMs { get; set; } = 2f;

    /// <summary>Modulation rate in Hz. Default 0.3 Hz.</summary>
    public float RateHz { get; set; } = 0.3f;

    /// <summary>Feedback amount, -0.99 to 0.99. Default 0.3.</summary>
    public float Feedback { get; set; } = 0.3f;

    /// <summary>
    /// Creates a flanger with a 50/50 default mix.
    /// </summary>
    public FlangerEffect()
    {
        Mix = 0.5f;
    }

    /// <summary>
    /// Locks <see cref="RateHz"/> to a tempo and note division.
    /// </summary>
    public void SyncToTempo(double bpm, NoteDivision division)
        => RateHz = (float)TempoTime.Hertz(bpm, division);

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        lfo = new Lfo(format.SampleRate);
        var max = (int)((BaseDelayMs + DepthMs + 2f) * 0.001f * format.SampleRate) + 2;
        lines = new DelayLine[format.Channels];
        for (var ch = 0; ch < format.Channels; ch++)
            lines[ch] = new DelayLine(max);
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        lfo.FrequencyHz = RateHz <= 0f ? 0.01f : RateHz;
        var feedback = Math.Clamp(Feedback, -0.99f, 0.99f);
        var baseSamples = BaseDelayMs * 0.001f * SampleRate;
        var depthSamples = DepthMs * 0.001f * SampleRate;
        var maxSamples = lines[0].MaxDelaySamples;

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var mod = 0.5f * (1f + lfo.Process());
            var d = baseSamples + depthSamples * mod;
            if (d < 1f) d = 1f;
            else if (d > maxSamples - 1) d = maxSamples - 1;

            for (var ch = 0; ch < channels; ch++)
            {
                var delayed = lines[ch].Read(d);
                lines[ch].Write(buffer[i + ch] + delayed * feedback);
                buffer[i + ch] = delayed;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        lfo?.Reset();
        foreach (var line in lines)
            line.Reset();
    }
}
