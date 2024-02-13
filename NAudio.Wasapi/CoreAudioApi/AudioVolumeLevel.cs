using NAudio.CoreAudioApi.Interfaces;
using System;

namespace NAudio.Wasapi.CoreAudioApi
{
    /// <summary>
    /// Audio Volume Level
    /// </summary>
    public class AudioVolumeLevel
    {
        private readonly IAudioVolumeLevel audioVolumeLevelInterface;

        internal AudioVolumeLevel(IAudioVolumeLevel audioVolumeLevel)
        {
            audioVolumeLevelInterface = audioVolumeLevel;
        }

        /// <summary>
        /// Channel Count
        /// </summary>
        public uint ChannelCount
        {
            get
            {
                audioVolumeLevelInterface.GetChannelCount(out uint result);
                return result;
            }
        }

        /// <summary>
        /// Get Level Range
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="minLevelDb">Minimum Level dB</param>
        /// <param name="maxLevelDb">Maximum Level dB</param>
        /// <param name="stepping">Stepping</param>
        public void GetLevelRange(uint channel, out float minLevelDb, out float maxLevelDb, out float stepping)
        {
            audioVolumeLevelInterface.GetLevelRange(channel, out minLevelDb, out maxLevelDb, out stepping);
        }

        /// <summary>
        /// Get Channel Volume Level
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <returns>Volume Level</returns>
        public float GetLevel(uint channel)
        {
            audioVolumeLevelInterface.GetLevel(channel, out float result);
            return result;
        }

        /// <summary>
        /// Set Channel Volume Level
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="value">Volume</param>
        public void SetLevel(uint channel, float value)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevel(channel, value, ref guid);
        }

        /// <summary>
        /// Sets all channels in the audio stream to the same uniform volume level, in decibels.
        /// </summary>
        /// <param name="value">Volume in decibels</param>
        public void SetLevelUniform(float value)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevelUniform(value, ref guid);
        }

        /// <summary>
        /// sets the volume levels, in decibels, of all the channels in the audio stream
        /// </summary>
        /// <param name="values">Volume levels in decibels</param>
        /// <param name="channels">Channels</param>
        public void SetLevelAllChannel(float[] values, uint channels)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevelAllChannel(values, channels, ref guid);
        }
    }
}
