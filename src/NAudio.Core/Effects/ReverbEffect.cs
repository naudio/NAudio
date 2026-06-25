using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Schroeder–Moorer algorithmic reverb in the classic Freeverb topology (Jezar,
/// public domain): per stereo bank, eight parallel low-pass-damped comb filters into
/// four series all-pass filters, with a stereo-spread offset. Low CPU and a
/// dependable sound — the lightweight baseline reverb. Reimplemented idiomatically
/// (denormal-safe, allocation-free steady state); not a transliteration.
/// </summary>
public sealed class ReverbEffect : AudioEffect, IParameterized
{
    private IReadOnlyList<EffectParameter> parameters;

    /// <summary>Generic parameter list (excludes Bypass/Mix, which are on the base).</summary>
    public IReadOnlyList<EffectParameter> Parameters => parameters ??= new[]
    {
        EffectParameter.Continuous("Room Size", "", 0f, 1f, () => RoomSize, v => RoomSize = v),
        EffectParameter.Continuous("Damping", "", 0f, 1f, () => Damping, v => Damping = v),
        EffectParameter.Continuous("Width", "", 0f, 1f, () => Width, v => Width = v)
    };

    private const float FixedGain = 0.015f;
    private const float ScaleRoom = 0.28f;
    private const float OffsetRoom = 0.7f;
    private const float ScaleDamp = 0.4f;
    private const int StereoSpread = 23;

    private static readonly int[] CombTuning =
        { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
    private static readonly int[] AllpassTuning = { 556, 441, 341, 225 };

    private Comb[,] combs;       // [bank, 8]
    private Allpass[,] allpasses; // [bank, 4]

    /// <summary>Room size, 0 (small) to 1 (large). Default 0.5.</summary>
    public float RoomSize { get; set; } = 0.5f;

    /// <summary>High-frequency damping, 0 (bright) to 1 (dark). Default 0.5.</summary>
    public float Damping { get; set; } = 0.5f;

    /// <summary>Stereo width of the reverb, 0 (mono) to 1 (wide). Default 1.</summary>
    public float Width { get; set; } = 1f;

    /// <summary>
    /// Creates the reverb with a sensible default wet/dry mix.
    /// </summary>
    public ReverbEffect()
    {
        Mix = 0.3f;
    }

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        var scale = format.SampleRate / 44100f;
        combs = new Comb[2, CombTuning.Length];
        allpasses = new Allpass[2, AllpassTuning.Length];
        for (var bank = 0; bank < 2; bank++)
        {
            var spread = bank == 1 ? StereoSpread : 0;
            for (var i = 0; i < CombTuning.Length; i++)
                combs[bank, i] = new Comb(Math.Max(1, (int)((CombTuning[i] + spread) * scale)));
            for (var i = 0; i < AllpassTuning.Length; i++)
                allpasses[bank, i] = new Allpass(Math.Max(1, (int)((AllpassTuning[i] + spread) * scale)));
        }
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var feedback = RoomSize * ScaleRoom + OffsetRoom;
        var damp1 = Damping * ScaleDamp;
        // The comb lengths scale with sample rate (so the decay time is already
        // stable), but this damping one-pole is per-sample, so re-map its
        // coefficient to keep the damping cutoff frequency — hence the
        // high-frequency decay — sample-rate invariant.
        if (SampleRate != 44100 && damp1 > 0f)
            damp1 = MathF.Pow(damp1, 44100f / SampleRate);
        var width = Math.Clamp(Width, 0f, 1f);
        var wet1 = width * 0.5f + 0.5f;
        var wet2 = (1f - width) * 0.5f;

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            var mono = 0f;
            for (var ch = 0; ch < channels; ch++)
                mono += buffer[i + ch];
            var input = mono * FixedGain;

            var outL = ProcessBank(0, input, feedback, damp1);
            var outR = channels >= 2 ? ProcessBank(1, input, feedback, damp1) : outL;

            if (channels >= 2)
            {
                buffer[i] = outL * wet1 + outR * wet2;
                buffer[i + 1] = outR * wet1 + outL * wet2;
                for (var ch = 2; ch < channels; ch++)
                    buffer[i + ch] = (outL + outR) * 0.5f;
            }
            else
            {
                buffer[i] = outL;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        if (combs == null)
            return;
        for (var bank = 0; bank < 2; bank++)
        {
            for (var i = 0; i < CombTuning.Length; i++)
                combs[bank, i].Reset();
            for (var i = 0; i < AllpassTuning.Length; i++)
                allpasses[bank, i].Reset();
        }
    }

    private float ProcessBank(int bank, float input, float feedback, float damp1)
    {
        var output = 0f;
        for (var i = 0; i < CombTuning.Length; i++)
            output += combs[bank, i].Process(input, feedback, damp1);
        for (var i = 0; i < AllpassTuning.Length; i++)
            output = allpasses[bank, i].Process(output);
        return output;
    }

    private sealed class Comb
    {
        private readonly float[] buffer;
        private int index;
        private float store;

        public Comb(int size) => buffer = new float[size];

        public float Process(float input, float feedback, float damp1)
        {
            var output = buffer[index];
            store = DenormalGuard.Flush(output * (1f - damp1) + store * damp1);
            buffer[index] = input + store * feedback;
            if (++index >= buffer.Length)
                index = 0;
            return output;
        }

        public void Reset()
        {
            Array.Clear(buffer);
            store = 0f;
            index = 0;
        }
    }

    private sealed class Allpass
    {
        private readonly float[] buffer;
        private int index;

        public Allpass(int size) => buffer = new float[size];

        public float Process(float input)
        {
            var buffered = buffer[index];
            var output = -input + buffered;
            buffer[index] = DenormalGuard.Flush(input + buffered * 0.5f);
            if (++index >= buffer.Length)
                index = 0;
            return output;
        }

        public void Reset()
        {
            Array.Clear(buffer);
            index = 0;
        }
    }
}
