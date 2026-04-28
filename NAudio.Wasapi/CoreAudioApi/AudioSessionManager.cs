// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AudioSessionManager
    /// 
    /// Designed to manage audio sessions and in particular the
    /// SimpleAudioVolume interface to adjust a session volume
    /// </summary>
    public class AudioSessionManager : IDisposable
    {
        private IAudioSessionManager audioSessionInterface;
        private IAudioSessionManager2 audioSessionInterface2;
        private AudioSessionNotification audioSessionNotification;
        private SessionCollection sessions;

        private SimpleAudioVolume simpleAudioVolume;
        private AudioSessionControl audioSessionControl;

        /// <summary>
        /// Session created delegate
        /// </summary>
        public delegate void SessionCreatedDelegate(object sender, AudioSessionControl newSession);

        /// <summary>
        /// Occurs when audio session has been added (for example run another program that use audio playback).
        /// </summary>
        public event SessionCreatedDelegate OnSessionCreated;

        /// <summary>
        /// Creates a new AudioSessionManager — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal AudioSessionManager(IntPtr nativePointer)
        {
            try
            {
                audioSessionInterface = ComActivation.WrapUnique<IAudioSessionManager>(nativePointer);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
            audioSessionInterface2 = audioSessionInterface as IAudioSessionManager2;

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
                    CoreAudioException.ThrowIfFailed(audioSessionInterface.GetSimpleAudioVolume(Guid.Empty, 0, out var ptr));
                    simpleAudioVolume = new SimpleAudioVolume(ptr);
                }
                return simpleAudioVolume;
            }
        }

        /// <summary>
        /// AudioSessionControl object
        /// for registering for callbacks and other session information
        /// </summary>
        public AudioSessionControl AudioSessionControl
        {
            get
            {
                if (audioSessionControl == null)
                {
                    CoreAudioException.ThrowIfFailed(audioSessionInterface.GetAudioSessionControl(Guid.Empty, 0, out var ptr));
                    audioSessionControl = new AudioSessionControl(ptr);
                }
                return audioSessionControl;
            }
        }

        internal void FireSessionCreated(IAudioSessionControl newSession)
        {
            OnSessionCreated?.Invoke(this, new AudioSessionControl(newSession));
        }

        /// <summary>
        /// Refresh session of current device.
        /// </summary>
        public void RefreshSessions()
        {
            UnregisterNotifications();

            if (audioSessionInterface2 != null)
            {
                CoreAudioException.ThrowIfFailed(audioSessionInterface2.GetSessionEnumerator(out var sessionEnumPtr));
                sessions = new SessionCollection(sessionEnumPtr);

                audioSessionNotification = new AudioSessionNotification(this);
                var notificationPtr = Marshal.GetComInterfaceForObject<AudioSessionNotification, IAudioSessionNotification>(audioSessionNotification);
                CoreAudioException.ThrowIfFailed(audioSessionInterface2.RegisterSessionNotification(notificationPtr));
                Marshal.Release(notificationPtr);
            }
        }

        /// <summary>
        /// Returns list of sessions of current device.
        /// </summary>
        public SessionCollection Sessions => sessions;

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            UnregisterNotifications();
            simpleAudioVolume?.Dispose();
            simpleAudioVolume = null;
            audioSessionControl?.Dispose();
            audioSessionControl = null;
            if (audioSessionInterface != null)
            {
                if ((object)audioSessionInterface is ComObject co)
                {
                    co.FinalRelease();
                }
                audioSessionInterface = null;
                audioSessionInterface2 = null;
            }
            GC.SuppressFinalize(this);
        }

        private void UnregisterNotifications()
        {
            sessions?.Dispose();
            sessions = null;

            if (audioSessionNotification != null && audioSessionInterface2 != null)
            {
                var notificationPtr = Marshal.GetComInterfaceForObject<AudioSessionNotification, IAudioSessionNotification>(audioSessionNotification);
                audioSessionInterface2.UnregisterSessionNotification(notificationPtr);
                Marshal.Release(notificationPtr);
                audioSessionNotification = null;
            }
        }
    }
}
