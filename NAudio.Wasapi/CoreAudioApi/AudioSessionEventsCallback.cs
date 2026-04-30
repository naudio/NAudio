// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AudioSessionEvents callback implementation
    /// </summary>
    [GeneratedComClass]
    public partial class AudioSessionEventsCallback : IAudioSessionEvents
    {
        private readonly IAudioSessionEventsHandler audioSessionEventsHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="handler"></param>
        public AudioSessionEventsCallback(IAudioSessionEventsHandler handler)
        {
            audioSessionEventsHandler = handler;
        }

        /// <summary>
        /// Notifies the client that the display name for the session has changed.
        /// </summary>
        public int OnDisplayNameChanged(string displayName, ref Guid eventContext)
        {
            audioSessionEventsHandler.OnDisplayNameChanged(displayName);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the display icon for the session has changed.
        /// </summary>
        public int OnIconPathChanged(string iconPath, ref Guid eventContext)
        {
            audioSessionEventsHandler.OnIconPathChanged(iconPath);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the volume level or muting state of the session has changed.
        /// </summary>
        public int OnSimpleVolumeChanged(float volume, int isMuted, ref Guid eventContext)
        {
            audioSessionEventsHandler.OnVolumeChanged(volume, isMuted != 0);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the volume level of an audio channel in the session submix has changed.
        /// </summary>
        public int OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex, ref Guid eventContext)
        {
            audioSessionEventsHandler.OnChannelVolumeChanged(channelCount, newVolumes, channelIndex);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the grouping parameter for the session has changed.
        /// </summary>
        public int OnGroupingParamChanged(ref Guid groupingId, ref Guid eventContext)
        {
            audioSessionEventsHandler.OnGroupingParamChanged(ref groupingId);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the stream-activity state of the session has changed.
        /// </summary>
        public int OnStateChanged(AudioSessionState state)
        {
            audioSessionEventsHandler.OnStateChanged(state);
            return 0;
        }

        /// <summary>
        /// Notifies the client that the session has been disconnected.
        /// </summary>
        public int OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            audioSessionEventsHandler.OnSessionDisconnected(disconnectReason);
            return 0;
        }
    }
}
