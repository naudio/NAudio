// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AudioSessionManager
    /// 
    /// Designed to manage audio sessions and in particuar the
    /// SimpleAudioVolume interface to adjust a session volume
    /// </summary>
    public class AudioSessionManager
    {
        private IAudioSessionManager audioSessionInterface;

        private SimpleAudioVolume simpleAudioVolume = null;
        private AudioSessionControl audioSessionControl = null;

        internal AudioSessionManager(IAudioSessionManager audioSessionManager)
        {
            audioSessionInterface = audioSessionManager;
        }

        /// <summary>
        /// SimpleAudioVolume object
        /// for adjusting the volume for the user session
        /// </summary>
        public SimpleAudioVolume SimpleAudioVolume
        {
            get
            {
                if (simpleAudioVolume == null)
                {
                    ISimpleAudioVolume simpleAudioInterface;

                    audioSessionInterface.GetSimpleAudioVolume(Guid.Empty, 0, out simpleAudioInterface);

                    simpleAudioVolume = new SimpleAudioVolume(simpleAudioInterface);
                }
                return simpleAudioVolume;
            }
        }

        /// <summary>
        /// AudioSessionControl object
        /// for registring for callbacks and other session information
        /// </summary>
        public AudioSessionControl AudioSessionControl
        {
            get
            {
                if (audioSessionControl == null)
                {
                    IAudioSessionControl audioSessionControlInterface;

                    audioSessionInterface.GetAudioSessionControl(Guid.Empty, 0, out audioSessionControlInterface);

                    audioSessionControl = new AudioSessionControl(audioSessionControlInterface);
                }
                return audioSessionControl;
            }
        }
    }
}
