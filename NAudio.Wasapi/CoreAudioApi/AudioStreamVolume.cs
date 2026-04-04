using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Manages the AudioStreamVolume for the <see cref="AudioClient"/>.
    /// </summary>
    public class AudioStreamVolume : IDisposable
    {
        IAudioStreamVolume audioStreamVolumeInterface;
        private IntPtr nativePointer;

        internal AudioStreamVolume(IntPtr nativePointer)
        {
            this.nativePointer = nativePointer;
            audioStreamVolumeInterface = (IAudioStreamVolume)Marshal.GetObjectForIUnknown(nativePointer);
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
            CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.GetChannelCount(out var channels));
            var levels = new float[channels];
            CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.GetAllVolumes(channels, levels));
            return levels;
        }

        /// <summary>
        /// Returns the current number of channels in this audio stream.
        /// </summary>
        public int ChannelCount
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.GetChannelCount(out var channels));
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
            unchecked 
            {
                index = (uint)channelIndex;
            }
            CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.GetChannelVolume(index, out var level));
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
                throw new ArgumentNullException(nameof(levels));
            }
            if (levels.Length != channelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levels),
                    String.Format(CultureInfo.InvariantCulture, "SetAllVolumes MUST be supplied with a volume level for ALL channels. The AudioStream has {0} channels and you supplied {1} channels.",
                                  channelCount, levels.Length));
            }
            for (int i = 0; i < levels.Length; i++)
            {
                float level = levels[i];
                if (level < 0.0f) throw new ArgumentOutOfRangeException(nameof(levels), "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
                if (level > 1.0f) throw new ArgumentOutOfRangeException(nameof(levels), "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
            }
            unchecked
            {
                CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.SetAllVoumes((uint)channelCount, levels));
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

            if (level < 0.0f) throw new ArgumentOutOfRangeException(nameof(level), "Volume must be between 0.0 and 1.0");
            if (level > 1.0f) throw new ArgumentOutOfRangeException(nameof(level), "Volume must be between 0.0 and 1.0");
            unchecked
            {
                CoreAudioException.ThrowIfFailed(audioStreamVolumeInterface.SetChannelVolume((uint)index, level));
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioStreamVolumeInterface != null)
            {
                audioStreamVolumeInterface = null;
            }
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
