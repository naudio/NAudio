using System;

namespace NAudio.Wave.SampleProviders;

/// <summary>
/// Sample Provider to allow fading in and out
/// </summary>
public class FadeInOutSampleProvider : ISampleProvider
{
    private enum FadeState
    {
        Silence,
        FadingIn,
        FullVolume,
        FadingOut,
    }

    private readonly object lockObject = new();
    private readonly ISampleProvider source;
    private int fadeSamplePosition;
    private int fadeSampleCount;
    private FadeState fadeState;

    /// <summary>
    /// Creates a new FadeInOutSampleProvider
    /// </summary>
    /// <param name="source">The source stream with the audio to be faded in or out</param>
    /// <param name="initiallySilent">If true, we start faded out</param>
    public FadeInOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
    {
        this.source = source;
        fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
    }

    /// <summary>
    /// Requests that a fade-in begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
    public void BeginFadeIn(double fadeDurationInMilliseconds)
    {
        lock (lockObject)
        {
            fadeSamplePosition = 0;
            fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
            fadeState = FadeState.FadingIn;
        }
    }

    /// <summary>
    /// Requests that a fade-out begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
    public void BeginFadeOut(double fadeDurationInMilliseconds)
    {
        lock (lockObject)
        {
            fadeSamplePosition = 0;
            fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
            fadeState = FadeState.FadingOut;
        }
    }

    /// <summary>
    /// Raised once when a fade-in started via <see cref="BeginFadeIn"/> reaches full volume.
    /// Fires from the <see cref="Read"/> call on which the transition happens, after the
    /// internal lock has been released, so handlers may safely call back into
    /// <see cref="BeginFadeIn"/> / <see cref="BeginFadeOut"/> (e.g. to schedule a fade-out).
    /// </summary>
    public event EventHandler FadeInComplete;

    /// <summary>
    /// Raised once when a fade-out started via <see cref="BeginFadeOut"/> reaches silence.
    /// Fires from the <see cref="Read"/> call on which the transition happens, after the
    /// internal lock has been released, so handlers may safely call back into
    /// <see cref="BeginFadeIn"/> / <see cref="BeginFadeOut"/> (e.g. to signal stop, or start
    /// a cross-fade into the next source).
    /// </summary>
    public event EventHandler FadeOutComplete;

    /// <summary>
    /// Reads samples from this sample provider into a span
    /// </summary>
    public int Read(Span<float> buffer)
    {
        int sourceSamplesRead = source.Read(buffer);
        bool fadeInCompleted = false;
        bool fadeOutCompleted = false;
        lock (lockObject)
        {
            if (fadeState == FadeState.FadingIn)
            {
                FadeIn(buffer, sourceSamplesRead);
                fadeInCompleted = fadeState == FadeState.FullVolume;
            }
            else if (fadeState == FadeState.FadingOut)
            {
                FadeOut(buffer, sourceSamplesRead);
                fadeOutCompleted = fadeState == FadeState.Silence;
            }
            else if (fadeState == FadeState.Silence)
            {
                buffer.Clear();
            }
        }
        // Fire outside the lock so a handler that calls BeginFadeIn/BeginFadeOut doesn't deadlock.
        if (fadeInCompleted) FadeInComplete?.Invoke(this, EventArgs.Empty);
        if (fadeOutCompleted) FadeOutComplete?.Invoke(this, EventArgs.Empty);
        return sourceSamplesRead;
    }

    private void FadeOut(Span<float> buffer, int sourceSamplesRead)
    {
        int sample = 0;
        while (sample < sourceSamplesRead)
        {
            float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
                buffer[sample++] *= multiplier;
            }
            fadeSamplePosition++;
            if (fadeSamplePosition > fadeSampleCount)
            {
                fadeState = FadeState.Silence;
                buffer.Slice(sample, sourceSamplesRead - sample).Clear();
                break;
            }
        }
    }

    private void FadeIn(Span<float> buffer, int sourceSamplesRead)
    {
        int sample = 0;
        while (sample < sourceSamplesRead)
        {
            float multiplier = (fadeSamplePosition / (float)fadeSampleCount);
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
                buffer[sample++] *= multiplier;
            }
            fadeSamplePosition++;
            if (fadeSamplePosition > fadeSampleCount)
            {
                fadeState = FadeState.FullVolume;
                break;
            }
        }
    }

    /// <summary>
    /// WaveFormat of this SampleProvider
    /// </summary>
    public WaveFormat WaveFormat => source.WaveFormat;
}
