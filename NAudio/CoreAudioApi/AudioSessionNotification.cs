using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    internal class AudioSessionNotification : IAudioSessionNotification
    {
        private AudioSessionManager parent;

        internal AudioSessionNotification(AudioSessionManager parent)
        {
            this.parent = parent;
        }

        [PreserveSig]
        public int OnSessionCreated(IAudioSessionControl newSession)
        {
            parent.FireSessionCreated(newSession);
            return 0;
        }
    }
}
