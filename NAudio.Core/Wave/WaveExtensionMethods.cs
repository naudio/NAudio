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

        /// <summary>
        /// Skips over a specified amount of time (by consuming source stream)
        /// </summary>
        /// <param name="sampleProvider">Source sample provider</param>
        /// <param name="skipDuration">Duration to skip over</param>
        /// <returns>A sample provider that skips over the specified amount of time</returns>
        public static ISampleProvider Skip(this ISampleProvider sampleProvider, TimeSpan skipDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { SkipOver = skipDuration};            
        }

        /// <summary>
        /// Takes a specified amount of time from the source stream
        /// </summary>
        /// <param name="sampleProvider">Source sample provider</param>
        /// <param name="takeDuration">Duration to take</param>
        /// <returns>A sample provider that reads up to the specified amount of time</returns>
        public static ISampleProvider Take(this ISampleProvider sampleProvider, TimeSpan takeDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { Take = takeDuration };
        }

        /// <summary>
        /// Converts a Stereo Sample Provider to mono, allowing mixing of channel volume
        /// </summary>
        /// <param name="sourceProvider">Stereo Source Provider</param>
        /// <param name="leftVol">Amount of left channel to mix in (0 = mute, 1 = full, 0.5 for mixing half from each channel)</param>
        /// <param name="rightVol">Amount of right channel to mix in (0 = mute, 1 = full, 0.5 for mixing half from each channel)</param>
        /// <returns>A mono SampleProvider</returns>
        public static ISampleProvider ToMono(this ISampleProvider sourceProvider, float leftVol = 0.5f, float rightVol = 0.5f)
        {
            if(sourceProvider.WaveFormat.Channels == 1) return sourceProvider;
            return new StereoToMonoSampleProvider(sourceProvider) {LeftVolume = leftVol, RightVolume = rightVol};
        }

        /// <summary>
        /// Converts a Mono ISampleProvider to stereo
        /// </summary>
        /// <param name="sourceProvider">Mono Source Provider</param>
        /// <param name="leftVol">Amount to mix to left channel (1.0 is full volume)</param>
        /// <param name="rightVol">Amount to mix to right channel (1.0 is full volume)</param>
        /// <returns></returns>
        public static ISampleProvider ToStereo(this ISampleProvider sourceProvider, float leftVol = 1.0f, float rightVol = 1.0f)
        {
            if (sourceProvider.WaveFormat.Channels == 2) return sourceProvider;
            return new MonoToStereoSampleProvider(sourceProvider) { LeftVolume = leftVol, RightVolume = rightVol };
        }
    }
}
