using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    public class AudioMute
    {
        private IAudioMute audioMuteInterface;
        internal AudioMute(IAudioMute audioMute)
        {
            audioMuteInterface = audioMute;
        }

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
