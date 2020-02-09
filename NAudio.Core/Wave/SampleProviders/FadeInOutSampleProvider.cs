namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample Provider to allow fading in and out
    /// </summary>
    public class FadeInOutSampleProvider : ISampleProvider
    {
        enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new object();
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
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to write to</param>
        /// <param name="count">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }
            return sourceSamplesRead;
        }

        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                buffer[n + offset] = 0;
            }
        }

        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    // clear out the end
                    ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
                    break;
                }
            }
        }

        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    // no need to multiply any more
                    break;
                }
            }
        }

        /// <summary>
        /// WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }
}
