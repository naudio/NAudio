using NAudio.CoreAudioApi.Interfaces;
using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Mute
    /// </summary>
    public class AudioMute
    {
        private IAudioMute audioMuteInterface;
        internal AudioMute(IAudioMute audioMute)
        {
            audioMuteInterface = audioMute;
        }

        /// <summary>
        /// Is Muted
        /// </summary>
        public bool IsMuted
        {
            get
            {
                audioMuteInterface.GetMute(out var result);
                return result;
            }
            set
            {
                var guid = Guid.Empty;
                audioMuteInterface.SetMute(value, guid);
            }
        }
    }
}
