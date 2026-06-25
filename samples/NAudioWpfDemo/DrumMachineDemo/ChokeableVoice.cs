using System;
using NAudio.Wave;

namespace NAudioWpfDemo.DrumMachineDemo;

/// <summary>
/// Wraps a voice so its choke time can be scheduled per-frame. <see cref="BeginChoke"/>
/// requests a linear fade-out starting <c>delayFrames</c> into the upcoming read, over
/// the given fade duration; once the fade completes the wrapper returns 0 so the
/// <see cref="NAudio.Wave.SampleProviders.MixingSampleProvider"/> drops it.
/// </summary>
/// <remarks>
/// Replaces a direct <c>BeginFadeOut</c> on <see cref="NAudio.Wave.SampleProviders.FadeInOutSampleProvider"/>:
/// that starts the fade at sample 0 of the next read, which is wrong when the new voice
/// triggering the choke fires mid-buffer (small playback buffers tend to hide this;
/// the larger render buffer exposes it as the previous voice being cut entirely).
/// </remarks>
internal class ChokeableVoice : ISampleProvider
{
    private readonly ISampleProvider source;
    private long readFrames;
    private long chokeStartFrame = long.MaxValue;
    private int chokeFadeFrames;
    private bool finished;

    public ChokeableVoice(ISampleProvider source)
    {
        this.source = source;
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    /// <summary>
    /// Schedule a fade-out to begin after <paramref name="delayFrames"/> additional frames
    /// have been read by this wrapper, lasting <paramref name="fadeFrames"/> frames.
    /// Has no effect if a choke is already scheduled or the voice has already finished.
    /// </summary>
    public void BeginChoke(int delayFrames, int fadeFrames)
    {
        if (finished || chokeStartFrame != long.MaxValue) return;
        chokeStartFrame = readFrames + Math.Max(0, delayFrames);
        chokeFadeFrames = Math.Max(1, fadeFrames);
    }

    public int Read(Span<float> buffer)
    {
        if (finished) return 0;
        int channels = source.WaveFormat.Channels;
        int read = source.Read(buffer);
        int framesRead = read / channels;
        if (chokeStartFrame == long.MaxValue || readFrames + framesRead <= chokeStartFrame)
        {
            readFrames += framesRead;
            return read;
        }
        for (int f = 0; f < framesRead; f++)
        {
            long absFrame = readFrames + f;
            if (absFrame < chokeStartFrame) continue;
            long fadeOffset = absFrame - chokeStartFrame;
            if (fadeOffset >= chokeFadeFrames)
            {
                finished = true;
                readFrames += f;
                return f * channels;
            }
            float gain = 1.0f - (float)fadeOffset / chokeFadeFrames;
            int baseIdx = f * channels;
            for (int c = 0; c < channels; c++)
            {
                buffer[baseIdx + c] *= gain;
            }
        }
        readFrames += framesRead;
        return read;
    }
}
