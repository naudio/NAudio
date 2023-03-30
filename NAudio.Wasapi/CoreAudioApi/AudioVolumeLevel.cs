using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wasapi.CoreAudioApi
{
    public class AudioVolumeLevel
    {
        private readonly IAudioVolumeLevel audioVolumeLevelInterface;
        private bool isLevelRangeRead = false;
        private float minLevelDb, maxLevelDb, stepping;

        internal AudioVolumeLevel(IAudioVolumeLevel audioVolumeLevel)
        {
            audioVolumeLevelInterface = audioVolumeLevel;
        }

        public uint ChannelCount
        {
            get
            {
                audioVolumeLevelInterface.GetChannelCount(out uint result);
                return result;
            }
        }

        public void GetLevelRange(uint channel, out float minLevelDb, out float maxLevelDb, out float stepping)
        {
            audioVolumeLevelInterface.GetLevelRange(channel, out minLevelDb, out maxLevelDb, out stepping);
        }

        public float GetLevel(uint channel)
        {
            audioVolumeLevelInterface.GetLevel(channel, out float result);
            return result;
        }

        public void SetLevel(uint channel, float value)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevel(channel, value, ref guid);
        }

        public void SetLevelUniform(float value)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevelUniform(value, ref guid);
        }

        public void SetLevelAllChannel(float[] values, uint channels)
        {
            var guid = Guid.Empty;
            audioVolumeLevelInterface.SetLevelAllChannel(values, channels, ref guid);
        }
    }
}
