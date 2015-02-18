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
        private AudioSessionNotification audioSessionNotification;
        private SessionCollection sessions;

        private SimpleAudioVolume simpleAudioVolume = null;
        private AudioSessionControl audioSessionControl = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newSession"></param>
        public delegate void SessionCreatedDelegate(object sender, IAudioSessionControl newSession);
        
        /// <summary>
        /// Occurs when audio session has been added (for example run another program that use audio playback).
        /// </summary>
        public event SessionCreatedDelegate OnSessionCreated;

        internal AudioSessionManager(IAudioSessionManager audioSessionManager)
        {
            audioSessionInterface = audioSessionManager;

            RefreshSessions();
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

        internal void FireSessionCreated(IAudioSessionControl newSession)
        {
            if (OnSessionCreated != null)
                OnSessionCreated(this, newSession);
        }

        /// <summary>
        /// Refresh session of current device.
        /// </summary>
        public void RefreshSessions()
        {
            UnregisterNotifications();

            IAudioSessionEnumerator _SessionEnum;
            Marshal.ThrowExceptionForHR(audioSessionInterface.GetSessionEnumerator(out _SessionEnum));
            sessions = new SessionCollection(_SessionEnum);

            audioSessionNotification = new AudioSessionNotification(this);
            Marshal.ThrowExceptionForHR(audioSessionInterface.RegisterSessionNotification(audioSessionNotification));
        }

        /// <summary>
        /// Returns list of sessions of current device.
        /// </summary>
        public SessionCollection Sessions
        {
            get
            {
                return sessions;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            UnregisterNotifications();
        }

        private void UnregisterNotifications()
        {
            if (sessions != null)
                sessions = null;

            if (audioSessionNotification != null)
                Marshal.ThrowExceptionForHR(audioSessionInterface.UnregisterSessionNotification(audioSessionNotification));
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~AudioSessionManager()
        {
            Dispose();
        }
    }
}
