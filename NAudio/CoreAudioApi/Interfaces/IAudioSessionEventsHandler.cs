using System;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// interface to receive session related events
    /// </summary>
    public interface IAudioSessionEventsHandler
    {
        /// <summary>
        /// notification of volume changes including muting of audio session
        /// </summary>
        /// <param name="volume">the current volume</param>
        /// <param name="isMuted">the current mute state, true muted, false otherwise</param>
        void OnVolumeChanged(float volume, bool isMuted);
    }
}
