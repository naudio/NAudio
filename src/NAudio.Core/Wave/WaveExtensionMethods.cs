using System;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// Useful extension methods to make switching between WaveAndSampleProvider easier
    /// </summary>
    public static class WaveExtensionMethods
    {
        /// <summary>
        /// Converts a WaveProvider into a SampleProvider (only works for PCM)
        /// </summary>
        /// <param name="waveProvider">WaveProvider to convert</param>
        /// <returns></returns>
        public static ISampleProvider ToSampleProvider(this IWaveProvider waveProvider)
        {
            return SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(waveProvider);
        }

        /// <summary>
        /// Allows sending an ISampleProvider directly to an IWavePlayer
        /// by wrapping it in a SampleToWaveProvider.
        /// </summary>
        public static void Init(this IWavePlayer wavePlayer, ISampleProvider sampleProvider)
        {
            wavePlayer.Init(new SampleToWaveProvider(sampleProvider));
        }

        /// <summary>
        /// Turns WaveFormatExtensible into a standard waveformat if possible
        /// </summary>
        /// <param name="waveFormat">Input wave format</param>
        /// <returns>A standard PCM or IEEE waveformat, or the original waveformat</returns>
        public static WaveFormat AsStandardWaveFormat(this WaveFormat waveFormat)
        {
            var wfe = waveFormat as WaveFormatExtensible;
            return wfe != null ? wfe.ToStandardWaveFormat() : waveFormat;
        }

        /// <summary>
        /// Converts a ISampleProvider to a IWaveProvider but still 32 bit float
        /// </summary>
        /// <param name="sampleProvider">SampleProvider to convert</param>
        /// <returns>An IWaveProvider</returns>
        public static IWaveProvider ToWaveProvider(this ISampleProvider sampleProvider)
        {
            return new SampleToWaveProvider(sampleProvider);
        }

        /// <summary>
        /// Converts a ISampleProvider to a IWaveProvider and convert to 16 bit
        /// </summary>
        /// <param name="sampleProvider">SampleProvider to convert</param>
        /// <returns>A 16 bit IWaveProvider</returns>
        public static IWaveProvider ToWaveProvider16(this ISampleProvider sampleProvider)
        {
            return new SampleToWaveProvider16(sampleProvider);
        }

        /// <summary>
        /// Converts a stereo ISampleProvider to mono
        /// </summary>
        public static ISampleProvider ToMono(this ISampleProvider source, float leftVol = 0.5f, float rightVol = 0.5f)
        {
            if (source.WaveFormat.Channels == 1) return source;
            return new StereoToMonoSampleProvider(source) { LeftVolume = leftVol, RightVolume = rightVol };
        }

        /// <summary>
        /// Converts a mono ISampleProvider to stereo
        /// </summary>
        public static ISampleProvider ToStereo(this ISampleProvider source, float leftVol = 1.0f, float rightVol = 1.0f)
        {
            if (source.WaveFormat.Channels == 2) return source;
            return new MonoToStereoSampleProvider(source) { LeftVolume = leftVol, RightVolume = rightVol };
        }

        /// <summary>
        /// Concatenates one sample source on the end of another with silence inserted
        /// </summary>
        public static ISampleProvider FollowedBy(this ISampleProvider sampleProvider, TimeSpan silenceDuration, ISampleProvider next)
        {
            var silenceAppended = new OffsetSampleProvider(sampleProvider) { LeadOut = silenceDuration };
            return new ConcatenatingSampleProvider(new ISampleProvider[] { silenceAppended, next });
        }

        /// <summary>
        /// Skips over a specified amount of time (by consuming source stream)
        /// </summary>
        public static ISampleProvider Skip(this ISampleProvider sampleProvider, TimeSpan skipDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { SkipOver = skipDuration };
        }

        /// <summary>
        /// Takes a specified amount of time from the source stream
        /// </summary>
        public static ISampleProvider Take(this ISampleProvider sampleProvider, TimeSpan takeDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { Take = takeDuration };
        }
    }
}
