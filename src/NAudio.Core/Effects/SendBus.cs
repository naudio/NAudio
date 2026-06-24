using System;
using NAudio.Wave;

namespace NAudio.Effects;

/// <summary>
/// An auxiliary effect send/return bus: a shared interleaved buffer that
/// sources mix a portion of their signal into, run through one
/// <see cref="IAudioEffect"/> (typically a reverb or chorus), with the wet
/// result returned (added) into a destination mix.
///
/// This is the generic plumbing a mixer or sampler uses to share one reverb
/// or chorus instance across many voices, rather than instantiating the
/// effect per voice. The effect runs every block so its tail keeps sounding
/// after the sends stop. Not thread-safe; drive it from the audio thread.
/// </summary>
public sealed class SendBus
{
    private readonly IAudioEffect effect;
    private float[] buffer;
    private int channels;
    // frames of effect tail left to flush since the last written input; a
    // fresh bus has no tail, so it idles until something is sent
    private long tailRemaining;

    /// <summary>
    /// Creates a send bus around an effect. The effect should be fully wet
    /// (return only its processed signal); <see cref="AudioEffect.Mix"/>
    /// defaults to 1, which is correct here.
    /// </summary>
    public SendBus(IAudioEffect effect)
    {
        this.effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }

    /// <summary>The hosted effect, exposed so callers can tweak or bypass it.</summary>
    public IAudioEffect Effect => effect;

    /// <summary>
    /// How long (in frames) the effect keeps running after the last block of
    /// written input, i.e. a conservative upper bound on its audible tail.
    /// While input keeps arriving the effect always runs; once input has been
    /// silent for this long, <see cref="ProcessReturn(Span{float}, int, bool)"/>
    /// skips the effect entirely until input is written again. 0 (the
    /// default) disables idle skipping — the effect runs every block.
    /// </summary>
    public int IdleTimeoutFrames { get; set; }

    /// <summary>
    /// Whether the bus is currently idle: idle skipping is enabled and the
    /// tail window since the last written input has fully elapsed (a fresh
    /// bus starts idle — it has no tail to flush).
    /// </summary>
    public bool IsIdle => IdleTimeoutFrames > 0 && tailRemaining <= 0;

    /// <summary>
    /// Configures the bus for a format and a maximum block size (frames per
    /// <see cref="ProcessReturn(Span{float}, int)"/> call). Allocates the send buffer.
    /// </summary>
    public void Configure(WaveFormat format, int maxFrames)
    {
        if (format == null) throw new ArgumentNullException(nameof(format));
        if (maxFrames < 1) throw new ArgumentOutOfRangeException(nameof(maxFrames));
        channels = format.Channels;
        buffer = new float[maxFrames * channels];
        effect.Configure(format);
    }

    /// <summary>
    /// The send buffer for the next <paramref name="frames"/> frames, cleared
    /// and ready for sources to accumulate into. Valid until the next
    /// <see cref="ProcessReturn(Span{float}, int)"/>.
    /// </summary>
    public Span<float> PrepareSend(int frames)
    {
        var span = buffer.AsSpan(0, frames * channels);
        span.Clear();
        return span;
    }

    /// <summary>
    /// Runs the effect over the accumulated send signal and adds the wet
    /// result into <paramref name="destination"/> (the dry mix). Call once per
    /// block after sources have written their sends via <see cref="PrepareSend"/>.
    /// This overload assumes input may have been written, so the effect always
    /// runs (and any idle countdown re-arms).
    /// </summary>
    public void ProcessReturn(Span<float> destination, int frames)
        => ProcessReturn(destination, frames, inputWritten: true);

    /// <summary>
    /// As <see cref="ProcessReturn(Span{float}, int)"/>, but with the caller
    /// reporting whether anything was actually mixed into the send buffer this
    /// block. When input has been silent for longer than
    /// <see cref="IdleTimeoutFrames"/> the effect's tail has decayed below
    /// audibility, so the effect processing (and the return add) is skipped
    /// until a send writes again — an idle reverb/chorus then costs nothing.
    /// </summary>
    public void ProcessReturn(Span<float> destination, int frames, bool inputWritten)
    {
        if (inputWritten)
        {
            tailRemaining = IdleTimeoutFrames; // re-arm the tail window
        }
        else if (IdleTimeoutFrames > 0)
        {
            if (tailRemaining <= 0) return; // tail fully flushed: skip the effect
            tailRemaining -= frames;
        }

        var span = buffer.AsSpan(0, frames * channels);
        effect.Process(span);
        for (int i = 0; i < span.Length; i++) destination[i] += span[i];
    }

    /// <summary>Clears the hosted effect's internal state (delay lines, etc.).</summary>
    public void Reset()
    {
        effect.Reset();
        tailRemaining = 0; // no state left, so no tail to flush
    }
}
