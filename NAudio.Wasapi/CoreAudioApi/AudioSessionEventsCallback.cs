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
    /// AudioSessionEvents callback implementation
    /// </summary>
    public class AudioSessionEventsCallback : IAudioSessionEvents
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
        /// <param name="displayName">The new display name for the session.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnDisplayNameChanged(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string displayName,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnDisplayNameChanged(displayName);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the display icon for the session has changed.
        /// </summary>
        /// <param name="iconPath">The path for the new display icon for the session.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnIconPathChanged(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnIconPathChanged(iconPath);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the volume level or muting state of the session has changed.
        /// </summary>
        /// <param name="volume">The new volume level for the audio session.</param>
        /// <param name="isMuted">The new muting state.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnSimpleVolumeChanged(
            [In] [MarshalAs(UnmanagedType.R4)] float volume,
            [In] [MarshalAs(UnmanagedType.Bool)] bool isMuted,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnVolumeChanged(volume, isMuted);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the volume level of an audio channel in the session submix has changed.
        /// </summary>
        /// <param name="channelCount">The channel count.</param>
        /// <param name="newVolumes">An array of volumnes cooresponding with each channel index.</param>
        /// <param name="channelIndex">The number of the channel whose volume level changed.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnChannelVolumeChanged(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelCount,
            [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr newVolumes, // Pointer to float array
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelIndex,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnChannelVolumeChanged(channelCount, newVolumes, channelIndex);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the grouping parameter for the session has changed.
        /// </summary>
        /// <param name="groupingId">The new grouping parameter for the session.</param>
        /// <param name="eventContext">A user context value that is passed to the notification callback.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnGroupingParamChanged(
            [In] ref Guid groupingId,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnGroupingParamChanged(ref groupingId);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the stream-activity state of the session has changed.
        /// </summary>
        /// <param name="state">The new session state.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnStateChanged(
            [In] AudioSessionState state)
        {
            audioSessionEventsHandler.OnStateChanged(state);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the session has been disconnected.
        /// </summary>
        /// <param name="disconnectReason">The reason that the audio session was disconnected.</param>
        /// <returns>An HRESULT code indicating whether the operation succeeded of failed.</returns>
        public int OnSessionDisconnected(
            [In] AudioSessionDisconnectReason disconnectReason)
        {
            audioSessionEventsHandler.OnSessionDisconnected(disconnectReason);

            return 0;
        }
    }
}
