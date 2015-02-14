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
        private IAudioSessionControl audioSessionControlInterface;
        private AudioSessionEventsCallback audioSessionEventCallback = null;

        internal AudioSessionControl(IAudioSessionControl audioSessionControl)
        {
            audioSessionControlInterface = audioSessionControl;
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~AudioSessionControl()
        {
            if (audioSessionEventCallback != null)
            {
                Marshal.ThrowExceptionForHR(audioSessionControlInterface.UnregisterAudioSessionNotification(audioSessionEventCallback));
            }
            Dispose();
        }

        #endregion

        /// <summary>
        /// The current state of the audio session
        /// </summary>
        public AudioSessionState State
        {
            get
            {
                AudioSessionState state;

                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetState(out state));

                return state;
            }
        }

        /// <summary>
        /// The name of the audio session
        /// </summary>
        public string DisplayName
        {
            get
            {
                string displayName = String.Empty;

                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetDisplayName(out displayName));

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
        /// the path to the icon shown in the mixer
        /// </summary>
        public string IconPath
        {
            get
            {
                string iconPath = String.Empty;

                Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetIconPath(out iconPath));

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
        /// the grouping param for an audio session grouping
        /// </summary>
        /// <returns></returns>
        public Guid GetGroupingParam()
        {
            Guid groupingId = Guid.Empty;

            Marshal.ThrowExceptionForHR(audioSessionControlInterface.GetGroupingParam(out groupingId));

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
            }
        }
    }
}
