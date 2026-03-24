using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// Extension methods for converting between legacy array-based and modern span-based audio interfaces.
    /// </summary>
    public static class AudioSourceExtensions
    {
        /// <summary>
        /// Converts an <see cref="IWaveProvider"/> to an <see cref="IAudioSource"/>.
        /// If the provider already implements <see cref="IAudioSource"/>, it is returned directly.
        /// Otherwise, a bridging adapter is created using a pooled buffer.
        /// </summary>
        public static IAudioSource ToAudioSource(this IWaveProvider waveProvider)
        {
            if (waveProvider is IAudioSource audioSource)
                return audioSource;
            return new WaveProviderAudioSource(waveProvider);
        }

        /// <summary>
        /// Converts an <see cref="IAudioSource"/> to an <see cref="IWaveProvider"/>.
        /// If the source already implements <see cref="IWaveProvider"/>, it is returned directly.
        /// Otherwise, a bridging adapter is created.
        /// </summary>
        public static IWaveProvider ToWaveProvider(this IAudioSource audioSource)
        {
            if (audioSource is IWaveProvider waveProvider)
                return waveProvider;
            return new AudioSourceWaveProvider(audioSource);
        }

        /// <summary>
        /// Converts an <see cref="ISampleProvider"/> to an <see cref="ISampleSource"/>.
        /// If the provider already implements <see cref="ISampleSource"/>, it is returned directly.
        /// Otherwise, a bridging adapter is created using a pooled buffer.
        /// </summary>
        public static ISampleSource ToSampleSource(this ISampleProvider sampleProvider)
        {
            if (sampleProvider is ISampleSource sampleSource)
                return sampleSource;
            return new SampleProviderSampleSource(sampleProvider);
        }

        /// <summary>
        /// Converts an <see cref="ISampleSource"/> to an <see cref="ISampleProvider"/>.
        /// If the source already implements <see cref="ISampleProvider"/>, it is returned directly.
        /// Otherwise, a bridging adapter is created.
        /// </summary>
        public static ISampleProvider ToSampleProvider(this ISampleSource sampleSource)
        {
            if (sampleSource is ISampleProvider sampleProvider)
                return sampleProvider;
            return new SampleSourceSampleProvider(sampleSource);
        }

        /// <summary>
        /// Converts an <see cref="ISampleSource"/> to an <see cref="IAudioSource"/>.
        /// If the source already implements <see cref="IAudioSource"/>, it is returned directly.
        /// Otherwise, a direct adapter reinterprets the float span as bytes (IEEE float only).
        /// </summary>
        public static IAudioSource ToAudioSource(this ISampleSource sampleSource)
        {
            if (sampleSource is IAudioSource audioSource)
                return audioSource;
            return new SampleSourceAudioSource(sampleSource);
        }
    }
}
