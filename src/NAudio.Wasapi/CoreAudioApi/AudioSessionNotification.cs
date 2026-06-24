using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi
{
    [GeneratedComClass]
    internal partial class AudioSessionNotification : IAudioSessionNotification
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
