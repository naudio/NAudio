using System;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// Per-band compression settings for a <see cref="MultibandCompressorEffect"/>.
/// </summary>
public sealed class MultibandCompressorBand
{
    /// <summary>Threshold in dBFS above which the band is compressed. Default -18 dB.</summary>
    public float ThresholdDb { get; set; } = -18f;

    /// <summary>Compression ratio (≥ 1). Default 3.</summary>
    public float Ratio { get; set; } = 3f;

    /// <summary>Attack time in milliseconds. Default 10 ms.</summary>
    public float AttackMs { get; set; } = 10f;

    /// <summary>Release time in milliseconds. Default 120 ms.</summary>
    public float ReleaseMs { get; set; } = 120f;

    /// <summary>Make-up gain in dB applied to the band. Default 0 dB.</summary>
    public float MakeUpGainDb { get; set; }

    /// <summary>The most recent gain reduction in dB (≥ 0), for metering.</summary>
    public float GainReductionDb { get; internal set; }
}

/// <summary>
/// Multiband compressor: a Linkwitz–Riley crossover splits the signal into bands,
/// each independently compressed, then recombined. Useful for transparent level
/// control, de-essing-style band taming, or mastering glue. Detection is
/// channel-linked per band.
/// </summary>
public sealed class MultibandCompressorEffect : AudioEffect
{
    private readonly float[] crossoverFrequencies;
    private readonly MultibandCompressorBand[] bands;
    private LinkwitzRileyCrossover[] crossovers = Array.Empty<LinkwitzRileyCrossover>();
    private EnvelopeFollower[] followers = Array.Empty<EnvelopeFollower>();
    private float[,] bandSamples;

    /// <summary>
    /// Creates a multiband compressor.
    /// </summary>
    /// <param name="crossoverFrequencies">Ascending crossover frequencies in Hz.
    /// Defaults to a 3-band 120 Hz / 2 kHz split.</param>
    public MultibandCompressorEffect(params float[] crossoverFrequencies)
    {
        this.crossoverFrequencies = crossoverFrequencies is { Length: > 0 }
            ? (float[])crossoverFrequencies.Clone()
            : new[] { 120f, 2000f };
        bands = new MultibandCompressorBand[this.crossoverFrequencies.Length + 1];
        for (var i = 0; i < bands.Length; i++)
            bands[i] = new MultibandCompressorBand();
    }

    /// <summary>The per-band settings (low → high).</summary>
    public IReadOnlyList<MultibandCompressorBand> Bands => bands;

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        crossovers = new LinkwitzRileyCrossover[format.Channels];
        for (var ch = 0; ch < format.Channels; ch++)
            crossovers[ch] = new LinkwitzRileyCrossover(format.SampleRate, crossoverFrequencies);

        followers = new EnvelopeFollower[bands.Length];
        for (var b = 0; b < bands.Length; b++)
            followers[b] = new EnvelopeFollower(bands[b].AttackMs, bands[b].ReleaseMs, format.SampleRate);

        bandSamples = new float[format.Channels, bands.Length];
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        var bandCount = bands.Length;
        Span<float> split = stackalloc float[bandCount];
        Span<float> gain = stackalloc float[bandCount];

        for (var i = 0; i + channels <= buffer.Length; i += channels)
        {
            for (var b = 0; b < bandCount; b++)
                gain[b] = 0f; // reused below as per-band peak first

            for (var ch = 0; ch < channels; ch++)
            {
                crossovers[ch].Process(buffer[i + ch], split);
                for (var b = 0; b < bandCount; b++)
                {
                    bandSamples[ch, b] = split[b];
                    var a = MathF.Abs(split[b]);
                    if (a > gain[b])
                        gain[b] = a; // channel-linked band peak
                }
            }

            for (var b = 0; b < bandCount; b++)
            {
                var band = bands[b];
                var ratio = band.Ratio < 1f ? 1f : band.Ratio;
                followers[b].AttackMilliseconds = band.AttackMs;
                followers[b].ReleaseMilliseconds = band.ReleaseMs;

                var keyDb = 20f * MathF.Log10(gain[b] < 1e-9f ? 1e-9f : gain[b]);
                var over = keyDb - band.ThresholdDb;
                var target = over > 0f ? over - over / ratio : 0f;
                var reduction = followers[b].ProcessRectified(target);
                band.GainReductionDb = reduction;
                gain[b] = MathF.Pow(10f, (band.MakeUpGainDb - reduction) * (1f / 20f));
            }

            for (var ch = 0; ch < channels; ch++)
            {
                var sum = 0f;
                for (var b = 0; b < bandCount; b++)
                    sum += bandSamples[ch, b] * gain[b];
                buffer[i + ch] = sum;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        foreach (var crossover in crossovers)
            crossover.Reset();
        foreach (var follower in followers)
            follower.Reset();
        foreach (var band in bands)
            band.GainReductionDb = 0f;
    }
}
