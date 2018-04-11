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
    /// AudioSessionControl object for information
    /// regarding an audio session
    /// </summary>
    public class AudioSessionControl : IDisposable
    {
        private readonly IAudioSessionControl audioSessionControlInterface;
        private readonly IAudioSessionControl2 audioSessionControlInterface2;
        private AudioSessionEventsCallback audioSessionEventCallback;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="audioSessionControl"></param>
        public AudioSessionControl(IAudioSessionControl audioSessionControl)
        {
            audioSessionControlInterface = audioSessionControl;
            audioSessionControlInterface2 = audioSessionControl as IAudioSessionControl2;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var meters = audioSessionControlInterface as IAudioMeterInformation;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var volume = audioSessionControlInterface as ISimpleAudioVolume;
            if (meters != null)
                AudioMeterInformation = new AudioMeterInformation(meters);
            if (volume != null)
                SimpleAudioVolume = new SimpleAudioVolume(volume);
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioSessionEventCallback != null)
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.UnregisterAudioSessionNotification(audioSessionEventCallback));
                audioSessionEventCallback = null;
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~AudioSessionControl()
        {
            Dispose();
        }

        #endregion

        /// <summary>
        /// Audio meter information of the audio session.
        /// </summary>
        public AudioMeterInformation AudioMeterInformation { get; }

        /// <summary>
        /// Simple audio volume of the audio session (for volume and mute status).
        /// </summary>
        public SimpleAudioVolume SimpleAudioVolume { get; }

        /// <summary>
        /// The current state of the audio session.
        /// </summary>
        public AudioSessionState State
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetState(out var state));

                return state;
            }
        }

        /// <summary>
        /// The name of the audio session.
        /// </summary>
        public string DisplayName
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetDisplayName(out var displayName));

                return displayName;
            }
            set
            {
                if (value != String.Empty)
                {
                    Marshal.ThrowExceptionForHR(audioSessionControlInterface.SetDisplayName(value, Guid.Empty));
                }
            }
        }

        /// <summary>
        /// the path to the icon shown in the mixer.
        /// </summary>
        public string IconPath
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetIconPath(out var iconPath));

                return iconPath;
            }
            set
            {
                if (value != String.Empty)
                {
                    Marshal.ThrowExceptionForHR(audioSessionControlInterface.SetIconPath(value, Guid.Empty));
                }
            }
        }

        /// <summary>
        /// The session identifier of the audio session.
        /// </summary>
        public string GetSessionIdentifier
        {
            get
            {
                if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
                Marshal.ThrowExceptionForHR(audioSessionControlInterface2.GetSessionIdentifier(out var str));
                return str;
            }
        }

        /// <summary>
        /// The session instance identifier of the audio session.
        /// </summary>
        public string GetSessionInstanceIdentifier
        {
            get
            {
                if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
                Marshal.ThrowExceptionForHR(audioSessionControlInterface2.GetSessionInstanceIdentifier(out var str));
                return str;
            }
        }

        /// <summary>
        /// The process identifier of the audio session.
        /// </summary>
        public uint GetProcessID
        {
            get
            {
                if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
                Marshal.ThrowExceptionForHR(audioSessionControlInterface2.GetProcessId(out var pid));
                return pid;
            }
        }

        /// <summary>
        /// Is the session a system sounds session.
        /// </summary>
        public bool IsSystemSoundsSession
        {
            get
            {
                if (audioSessionControlInterface2 == null) throw new InvalidOperationException("Not supported on this version of Windows");
                return (audioSessionControlInterface2.IsSystemSoundsSession() == 0);
            }
        }

        /// <summary>
        /// the grouping param for an audio session grouping
        /// </summary>
        /// <returns></returns>
        public Guid GetGroupingParam()
        {
            Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetGroupingParam(out var groupingId));

            return groupingId;
        }

        /// <summary>
        /// For chanigng the grouping param and supplying the context of said change
        /// </summary>
        /// <param name="groupingId"></param>
        /// <param name="context"></param>
        public void SetGroupingParam(Guid groupingId, Guid context)
        {
            Marshal.ThrowExceptionForHR(audioSessionControlInterface.SetGroupingParam(groupingId, context));
        }

        /// <summary>
        /// Registers an even client for callbacks
        /// </summary>
        /// <param name="eventClient"></param>
        public void RegisterEventClient(IAudioSessionEventsHandler eventClient)
        {
            // we could have an array or list of listeners if we like
            audioSessionEventCallback = new AudioSessionEventsCallback(eventClient);
            Marshal.ThrowExceptionForHR(audioSessionControlInterface.RegisterAudioSessionNotification(audioSessionEventCallback));
        }

        /// <summary>
        /// Unregisters an event client from receiving callbacks
        /// </summary>
        /// <param name="eventClient"></param>
        public void UnRegisterEventClient(IAudioSessionEventsHandler eventClient)
        {
            // if one is registered, let it go
            if (audioSessionEventCallback != null)
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.UnregisterAudioSessionNotification(audioSessionEventCallback));
                audioSessionEventCallback = null;
            }
        }
    }
}
