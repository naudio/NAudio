using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Feedback-delay-network reverb in the clean, modulated-FDN style described by
/// Signalsmith Audio: eight delay lines mixed each sample through an orthonormal
/// Hadamard matrix, with per-line frequency damping and slow delay-length modulation
/// for a smooth, dense, metallic-free tail. Decay is set directly as an RT60 time.
/// The flagship quality reverb. (Algorithm/topology reference only — original
/// implementation.)
/// </summary>
public sealed class FdnReverbEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Decay", "s", 0.1f, 10f, () => DecaySeconds, v => DecaySeconds = v),
        EffectParameter.Continuous("Size", "", 0.1f, 2f, () => Size, v => Size = v),
        EffectParameter.Continuous("Damping", "", 0f, 1f, () => Damping, v => Damping = v),
        EffectParameter.Continuous("Mod Depth", "ms", 0f, 5f, () => ModulationDepthMs, v => ModulationDepthMs = v),
        EffectParameter.Continuous("Mod Rate", "Hz", 0.05f, 5f, () => ModulationRateHz, v => ModulationRateHz = v),
        EffectParameter.Continuous("Pre-Delay", "ms", 0f, 200f, () => PreDelayMs, v => PreDelayMs = v),
        EffectParameter.Continuous("Width", "", 0f, 1f, () => Width, v => Width = v)
    };

    private const int Lines = 8;

    // Mutually-incommensurate base delays (ms) — primes-ish to avoid resonances.
    private static readonly float[] BaseDelaysMs =
        { 29.7f, 34.1f, 37.3f, 41.1f, 43.7f, 47.3f, 53.9f, 59.3f };

    private DelayLine[] lines = Array.Empty<DelayLine>();
    private DelayLine preDelay;
    private Lfo[] modulators = Array.Empty<Lfo>();
    private readonly float[] damped = new float[Lines];
    private readonly float[] dampState = new float[Lines];
    private readonly float[] mixed = new float[Lines];

    /// <summary>Reverb decay time (RT60) in seconds. Default 2.0 s.</summary>
    public float DecaySeconds { get; set; } = 2f;

    /// <summary>Room size multiplier applied to the delay lengths, 0.1–2. Default 1.</summary>
    public float Size { get; set; } = 1f;

    /// <summary>High-frequency damping, 0 (bright) to 1 (dark). Default 0.3.</summary>
    public float Damping { get; set; } = 0.3f;

    /// <summary>Delay-modulation depth in milliseconds (lush detune). Default 0.5 ms.</summary>
    public float ModulationDepthMs { get; set; } = 0.5f;

    /// <summary>Delay-modulation rate in Hz. Default 0.5 Hz.</summary>
    public float ModulationRateHz { get; set; } = 0.5f;

    /// <summary>Pre-delay before the reverb tail, in milliseconds. Default 20 ms.</summary>
    public float PreDelayMs { get; set; } = 20f;

    /// <summary>Stereo width, 0 (mono) to 1 (wide). Default 1.</summary>
    public float Width { get; set; } = 1f;

    /// <summary>
    /// Creates the reverb with a sensible default wet/dry mix.
    /// </summary>
    public FdnReverbEffect()
    {
        Mix = 0.3f;
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        var sr = format.SampleRate;
        var maxLine = (int)(BaseDelaysMs[^1] * 2f * 0.001f * sr) + (int)(0.02f * sr) + 4;
        lines = new DelayLine[Lines];
        modulators = new Lfo[Lines];
        for (var i = 0; i < Lines; i++)
        {
            lines[i] = new DelayLine(maxLine);
            modulators[i] = new Lfo(sr) { FrequencyHz = 0.5f };
        }
        preDelay = new DelayLine(Math.Max(2, (int)(0.5f * sr)));
        Array.Clear(dampState);
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var sr = SampleRate;
        var size = Math.Clamp(Size, 0.1f, 2f);
        var rt60 = MathF.Max(0.05f, DecaySeconds);
        var dampCoeff = 1f - Math.Clamp(Damping, 0f, 1f) * 0.85f;
        var modDepth = MathF.Max(0f, ModulationDepthMs) * 0.001f * sr;
        var modRate = ModulationRateHz <= 0f ? 0.01f : ModulationRateHz;
        var preDelaySamples = Math.Clamp(PreDelayMs * 0.001f * sr, 1f, preDelay.MaxDelaySamples - 1);
        var width = Math.Clamp(Width, 0f, 1f);

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var mono = 0f;
            for (var ch = 0; ch < channels; ch++)
                mono += buffer[i + ch];
            mono *= 0.5f;

            preDelay.Write(mono);
            var input = preDelay.Read(preDelaySamples);

            for (var n = 0; n < Lines; n++)
            {
                modulators[n].FrequencyHz = modRate * (0.85f + 0.04f * n);
                var length = BaseDelaysMs[n] * 0.001f * sr * size;
                var readPos = length + modDepth * (0.5f + 0.5f * modulators[n].Process());
                if (readPos < 1f) readPos = 1f;
                else if (readPos > lines[n].MaxDelaySamples - 1) readPos = lines[n].MaxDelaySamples - 1;

                var d = lines[n].Read(readPos);
                dampState[n] += dampCoeff * (d - dampState[n]);
                damped[n] = dampState[n];
            }

            Hadamard8(damped, mixed);

            var outL = 0f;
            var outR = 0f;
            for (var n = 0; n < Lines; n++)
            {
                var length = BaseDelaysMs[n] * 0.001f * sr * size;
                // Per-line gain that yields the requested RT60 (matrix is energy-preserving).
                var g = MathF.Pow(10f, -3f * length / (rt60 * sr));
                lines[n].Write(input + mixed[n] * g);

                if ((n & 1) == 0) outL += damped[n];
                else outR += damped[n];
            }
            outL *= 0.5f;
            outR *= 0.5f;

            if (channels >= 2)
            {
                var mid = (outL + outR) * 0.5f;
                buffer[i] = mid + (outL - mid) * width;
                buffer[i + 1] = mid + (outR - mid) * width;
                for (var ch = 2; ch < channels; ch++)
                    buffer[i + ch] = mid;
            }
            else
            {
                buffer[i] = (outL + outR) * 0.5f;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        foreach (var line in lines)
            line.Reset();
        preDelay?.Reset();
        foreach (var m in modulators)
            m.Reset();
        Array.Clear(dampState);
    }

    // In-place fast Walsh–Hadamard transform on 8 elements, normalised to be
    // orthonormal (energy-preserving), so the matrix sets diffusion, not decay.
    private static void Hadamard8(float[] source, float[] destination)
    {
        for (var i = 0; i < Lines; i++)
            destination[i] = source[i];

        for (var step = 1; step < Lines; step <<= 1)
        {
            for (var i = 0; i < Lines; i += step << 1)
            {
                for (var j = i; j < i + step; j++)
                {
                    var a = destination[j];
                    var b = destination[j + step];
                    destination[j] = a + b;
                    destination[j + step] = a - b;
                }
            }
        }

        const float norm = 0.35355339f; // 1 / sqrt(8)
        for (var i = 0; i < Lines; i++)
            destination[i] *= norm;
    }
}
