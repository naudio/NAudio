using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Utility class for converting to SampleProvider
    /// </summary>
    static class SampleProviderConverters
    {
        /// <summary>
        /// Helper function to go from IWaveProvider to a SampleProvider
        /// Must already be PCM or IEEE float
        /// </summary>
        /// <param name="waveProvider">The WaveProvider to convert</param>
        /// <returns>A sample provider</returns>
        public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
        {
            return ConvertAudioSourceIntoSampleSource(waveProvider.ToAudioSource()).ToSampleProvider();
        }

        /// <summary>
        /// Helper function to go from IAudioSource to an ISampleSource
        /// Must already be PCM or IEEE float
        /// </summary>
        /// <param name="audioSource">The AudioSource to convert</param>
        /// <returns>A sample source</returns>
        public static ISampleSource ConvertAudioSourceIntoSampleSource(IAudioSource audioSource)
        {
            ISampleSource sampleSource;
            if (audioSource.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                // go to float
                if (audioSource.WaveFormat.BitsPerSample == 8)
                {
                    sampleSource = new Pcm8BitToSampleProvider(audioSource);
                }
                else if (audioSource.WaveFormat.BitsPerSample == 16)
                {
                    sampleSource = new Pcm16BitToSampleProvider(audioSource);
                }
                else if (audioSource.WaveFormat.BitsPerSample == 24)
                {
                    sampleSource = new Pcm24BitToSampleProvider(audioSource);
                }
                else if (audioSource.WaveFormat.BitsPerSample == 32)
                {
                    sampleSource = new Pcm32BitToSampleProvider(audioSource);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            else if (audioSource.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                if (audioSource.WaveFormat.BitsPerSample == 64)
                    sampleSource = new WaveToSampleProvider64(audioSource);
                else
                    sampleSource = new WaveToSampleProvider(audioSource);
            }
            else
            {
                throw new ArgumentException("Unsupported source encoding");
            }
            return sampleSource;
        }
    }
}
