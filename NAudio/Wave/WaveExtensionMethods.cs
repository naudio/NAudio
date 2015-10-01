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
        /// Allows sending a SampleProvider directly to an IWavePlayer without needing to convert
        /// back to an IWaveProvider
        /// </summary>
        /// <param name="wavePlayer">The WavePlayer</param>
        /// <param name="sampleProvider"></param>
        /// <param name="convertTo16Bit"></param>
        public static void Init(this IWavePlayer wavePlayer, ISampleProvider sampleProvider, bool convertTo16Bit = false)
        {
            IWaveProvider provider = convertTo16Bit ? (IWaveProvider)new SampleToWaveProvider16(sampleProvider) : new SampleToWaveProvider(sampleProvider);
            wavePlayer.Init(provider);
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
        /// Converts a ISampleProvider to a IWaveProvider but and convert to 16 bit
        /// </summary>
        /// <param name="sampleProvider">SampleProvider to convert</param>
        /// <returns>A 16 bit IWaveProvider</returns>
        public static IWaveProvider ToWaveProvider16(this ISampleProvider sampleProvider)
        {
            return new SampleToWaveProvider16(sampleProvider);
        }

        /// <summary>
        /// Concatenates one Sample Provider on the end of another
        /// </summary>
        /// <param name="sampleProvider">The sample provider to play first</param>
        /// <param name="next">The sample provider to play next</param>
        /// <returns>A single sampleprovider to play one after the other</returns>
        public static ISampleProvider FollowedBy(this ISampleProvider sampleProvider, ISampleProvider next)
        {
            return new ConcatenatingSampleProvider(new[] { sampleProvider, next});
        }

        /// <summary>
        /// Concatenates one Sample Provider on the end of another with silence inserted
        /// </summary>
        /// <param name="sampleProvider">The sample provider to play first</param>
        /// <param name="silenceDuration">Silence duration to insert between the two</param>
        /// <param name="next">The sample provider to play next</param>
        /// <returns>A single sample provider</returns>
        public static ISampleProvider FollowedBy(this ISampleProvider sampleProvider, TimeSpan silenceDuration, ISampleProvider next)
        {
            var silenceAppended = new OffsetSampleProvider(sampleProvider) {LeadOut = silenceDuration};
            return new ConcatenatingSampleProvider(new[] { silenceAppended, next });
        }


    }
}
