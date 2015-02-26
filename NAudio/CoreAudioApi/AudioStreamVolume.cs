using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Manages the AudioStreamVolume for the <see cref="AudioClient"/>.
    /// </summary>
    public class AudioStreamVolume : IDisposable
    {
        IAudioStreamVolume audioStreamVolumeInterface;

        internal AudioStreamVolume(IAudioStreamVolume audioStreamVolumeInterface)
        {
            this.audioStreamVolumeInterface = audioStreamVolumeInterface;
        }

        /// <summary>
        /// Verify that the channel index is valid.
        /// </summary>
        /// <param name="channelIndex"></param>
        /// <param name="parameter"></param>
        private void CheckChannelIndex(int channelIndex, string parameter)
        {
            int channelCount = ChannelCount;
            if (channelIndex >= channelCount)
            {
                throw new ArgumentOutOfRangeException(parameter, "You must supply a valid channel index < current count of channels: " + channelCount.ToString());
            }
        }

        /// <summary>
        /// Return the current stream volumes for all channels
        /// </summary>
        /// <returns>An array of volume levels between 0.0 and 1.0 for each channel in the audio stream.</returns>
        public float[] GetAllVolumes()
        {
            uint channels;
            float[] levels;
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelCount(out channels));
            levels = new float[channels];
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetAllVolumes(channels, levels));
            return levels;
        }

        /// <summary>
        /// Returns the current number of channels in this audio stream.
        /// </summary>
        public int ChannelCount
        {
            get
            {
                uint channels;
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelCount(out channels));
                unchecked
                {
                    return (int)channels;
                }
            }
        }

        /// <summary>
        /// Return the current volume for the requested channel.
        /// </summary>
        /// <param name="channelIndex">The 0 based index into the channels.</param>
        /// <returns>The volume level for the channel between 0.0 and 1.0.</returns>
        public float GetChannelVolume(int channelIndex)
        {
            CheckChannelIndex(channelIndex, "channelIndex");

            uint index;
            float level;
            unchecked 
            {
                index = (uint)channelIndex;
            }
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelVolume(index, out level));
            return level;
        }

        /// <summary>
        /// Set the volume level for each channel of the audio stream.
        /// </summary>
        /// <param name="levels">An array of volume levels (between 0.0 and 1.0) one for each channel.</param>
        /// <remarks>
        /// A volume level MUST be supplied for reach channel in the audio stream.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="levels"/> does not contain <see cref="ChannelCount"/> elements.
        /// </exception>
        public void SetAllVolumes(float[] levels)
        {
            // Make friendly Net exceptions for common problems:
            int channelCount = ChannelCount;
            if (levels == null)
            {
                throw new ArgumentNullException("levels");
            }
            if (levels.Length != channelCount)
            {
                throw new ArgumentOutOfRangeException(
                    "levels",
                    String.Format(CultureInfo.InvariantCulture, "SetAllVolumes MUST be supplied with a volume level for ALL channels. The AudioStream has {0} channels and you supplied {1} channels.",
                                  channelCount, levels.Length));
            }
            for (int i = 0; i < levels.Length; i++)
            {
                float level = levels[i];
                if (level < 0.0f) throw new ArgumentOutOfRangeException("levels", "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
                if (level > 1.0f) throw new ArgumentOutOfRangeException("levels", "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
            }
            unchecked
            {
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.SetAllVoumes((uint)channelCount, levels));
            }
        }

        /// <summary>
        /// Sets the volume level for one channel in the audio stream.
        /// </summary>
        /// <param name="index">The 0-based index into the channels to adjust the volume of.</param>
        /// <param name="level">The volume level between 0.0 and 1.0 for this channel of the audio stream.</param>
        public void SetChannelVolume(int index, float level)
        {
            CheckChannelIndex(index, "index");

            if (level < 0.0f) throw new ArgumentOutOfRangeException("level", "Volume must be between 0.0 and 1.0");
            if (level > 1.0f) throw new ArgumentOutOfRangeException("level", "Volume must be between 0.0 and 1.0");
            unchecked
            {
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.SetChannelVolume((uint)index, level));
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release/cleanup objects during Dispose/finalization.
        /// </summary>
        /// <param name="disposing">True if disposing and false if being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (audioStreamVolumeInterface != null)
                {
                    // although GC would do this for us, we want it done now
                    Marshal.ReleaseComObject(audioStreamVolumeInterface);
                    audioStreamVolumeInterface = null;
                }
            }
        }

        #endregion
    }
}
