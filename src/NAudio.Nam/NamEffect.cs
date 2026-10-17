using System;
using NAudio.Effects;
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
/// The effect must run at the model's sample rate (normally 48 kHz);
/// <see cref="IAudioEffect.Configure"/> throws otherwise. Resample other rates, for
/// example with <c>WdlResamplingSampleProvider</c>. It adds no latency and does not
/// allocate while processing.
/// </para>
/// </remarks>
public sealed class NamEffect : AudioEffect
{
    private readonly NamProcessor processor;
    private readonly float[] mono = new float[256];

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
    }

    /// <summary>The model this effect runs.</summary>
    public NamModel Model { get; }

    /// <summary>The network within <see cref="Model"/> that this effect runs.</summary>
    public NamSubmodel Submodel => processor.Submodel;

    /// <inheritdoc />
    protected override void OnConfigure(WaveFormat format)
    {
        var expected = Submodel.SampleRate;
        if (expected.HasValue && expected.Value != format.SampleRate)
            throw new ArgumentException(
                $"This NAM model was trained at {expected.Value} Hz but the audio is {format.SampleRate} Hz. Resample the audio to {expected.Value} Hz before the effect.",
                nameof(format));
        processor.Reset();
    }

    /// <inheritdoc />
    protected override void ProcessBlock(Span<float> buffer)
    {
        var channels = Channels;
        if (channels == 1)
        {
            processor.Process(buffer, buffer);
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
            processor.Process(chunk, chunk);
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
    }
}
