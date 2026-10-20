using System;
using NAudio.Effects;
using NAudio.Nam.Internal;
using NAudio.Wave;

namespace NAudio.Nam;

/// <summary>
/// An NAudio effect that runs audio through a Neural Amp Modeler capture, making the
/// signal sound like it went through the captured amp or pedal. Add it to an
/// <see cref="EffectChain"/> or wrap a source in an <see cref="EffectSampleProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// NAM networks are mono. With more than one channel the effect processes the
/// average of the channels and writes the result to every channel. If your guitar
/// is on one input of a stereo interface, the silent other channel halves the level
/// into the model; pick the guitar channel out to mono first so the drive matches
/// the capture.
/// </para>
/// <para>
/// NAM models are trained at one sample rate (normally 48 kHz) and must run at it. At
/// that rate the effect adds no latency. At any other rate it resamples internally:
/// the audio is converted to the model's rate, processed, and converted back with
/// 64-tap sinc interpolation, which adds a fixed latency of about 1.5 ms, reported by
/// <see cref="AudioEffect.LatencySamples"/>. See <see cref="IsResampling"/>. Either way it
/// does not allocate while processing.
/// </para>
/// </remarks>
public sealed class NamEffect : AudioEffect
{
    private readonly NamProcessor processor;
    private readonly ResamplingBridge.BlockProcessor processAtModelRate;
    private readonly float[] mono = new float[256];
    private ResamplingBridge? bridge;

    /// <summary>
    /// Creates an effect from a loaded model. <paramref name="size"/> picks the network
    /// from 0 (smallest) to 1 (largest): for an A2 model, below 0.5 runs A2-Lite and
    /// 0.5 or above runs A2-Full. See <see cref="NamModel.GetSubmodel"/>.
    /// </summary>
    public NamEffect(NamModel model, double size = 1.0)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        processor = model.CreateProcessor(size);
        processAtModelRate = samples => processor.Process(samples, samples);
    }

    /// <summary>The model this effect runs.</summary>
    public NamModel Model { get; }

    /// <summary>The network within <see cref="Model"/> that this effect runs.</summary>
    public NamSubmodel Submodel => processor.Submodel;

    /// <summary>
    /// True when the configured sample rate differs from the model's, so the effect
    /// resamples internally and reports the resulting delay in
    /// <see cref="AudioEffect.LatencySamples"/>. False before the effect is configured.
    /// </summary>
    public bool IsResampling => bridge != null;

    /// <inheritdoc />
    public override int LatencySamples => bridge?.LatencySamples ?? 0;

    internal long UnderrunSamples => bridge?.UnderrunSamples ?? 0;

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        var modelRate = Submodel.SampleRate;
        bridge = modelRate.HasValue && modelRate.Value != format.SampleRate
            ? new ResamplingBridge(format.SampleRate, modelRate.Value)
            : null;
        processor.Reset();
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        if (channels == 1)
        {
            ProcessMono(buffer);
            return;
        }

        var scale = 1f / channels;
        var framesPerChunk = mono.Length;
        for (var start = 0; start < buffer.Length; start += framesPerChunk * channels)
        {
            var frames = Math.Min(framesPerChunk, (buffer.Length - start) / channels);
            if (frames == 0)
                break;
            var interleaved = buffer.Slice(start, frames * channels);
            var chunk = mono.AsSpan(0, frames);
            for (var f = 0; f < frames; f++)
            {
                var sum = 0f;
                for (var ch = 0; ch < channels; ch++)
                    sum += interleaved[f * channels + ch];
                chunk[f] = sum * scale;
            }
            ProcessMono(chunk);
            for (var f = 0; f < frames; f++)
            {
                var y = chunk[f];
                for (var ch = 0; ch < channels; ch++)
                    interleaved[f * channels + ch] = y;
            }
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        processor.Reset();
        bridge?.Reset();
    }

    private void ProcessMono(Span<float> samples)
    {
        var active = bridge;
        if (active == null)
            processor.Process(samples, samples);
        else
            active.Process(samples, processAtModelRate);
    }
}
