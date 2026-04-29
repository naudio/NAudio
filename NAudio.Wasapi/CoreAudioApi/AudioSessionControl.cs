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
    /// AudioSessionControl object for information
    /// regarding an audio session
    /// </summary>
    public class AudioSessionControl : IDisposable
    {
        private IAudioSessionControl audioSessionControlInterface;
        private IAudioSessionControl2 audioSessionControlInterface2;
        private readonly bool ownsInterface;
        private AudioSessionEventsCallback audioSessionEventCallback;

        /// <summary>
        /// Creates a new AudioSessionControl — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal AudioSessionControl(IntPtr nativePointer)
        {
            try
            {
                audioSessionControlInterface = (IAudioSessionControl)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    nativePointer, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
            ownsInterface = true;
            audioSessionControlInterface2 = audioSessionControlInterface as IAudioSessionControl2;

            if (audioSessionControlInterface is IAudioMeterInformation meters)
                AudioMeterInformation = new AudioMeterInformation(meters);
            if (audioSessionControlInterface is ISimpleAudioVolume volume)
                SimpleAudioVolume = new SimpleAudioVolume(volume);
        }

        /// <summary>
        /// Creates a new AudioSessionControl from a borrowed interface (e.g. COM callback).
        /// This instance does not own the COM pointer.
        /// </summary>
        /// <param name="borrowed">IAudioSessionControl obtained from a COM callback</param>
        internal AudioSessionControl(IAudioSessionControl borrowed)
        {
            audioSessionControlInterface = borrowed;
            audioSessionControlInterface2 = borrowed as IAudioSessionControl2;
            ownsInterface = false;

            if (audioSessionControlInterface is IAudioMeterInformation meters)
                AudioMeterInformation = new AudioMeterInformation(meters);
            if (audioSessionControlInterface is ISimpleAudioVolume volume)
                SimpleAudioVolume = new SimpleAudioVolume(volume);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioSessionEventCallback != null)
            {
                var ptr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(audioSessionEventCallback, CreateComInterfaceFlags.None);
                try
                {
                    audioSessionControlInterface.UnregisterAudioSessionNotification(ptr);
                }
                finally
                {
                    Marshal.Release(ptr);
                }
                audioSessionEventCallback = null;
            }
            if (audioSessionControlInterface != null)
            {
                if (ownsInterface && (object)audioSessionControlInterface is ComObject co)
                {
                    co.FinalRelease();
                }
                audioSessionControlInterface = null;
                audioSessionControlInterface2 = null;
            }
            GC.SuppressFinalize(this);
        }

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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetState(out var state));

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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetDisplayName(out var displayName));

                return displayName;
            }
            set
            {
                if (value != String.Empty)
                {
                    CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetDisplayName(value, Guid.Empty));
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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetIconPath(out var iconPath));

                return iconPath;
            }
            set
            {
                if (value != String.Empty)
                {
                    CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetIconPath(value, Guid.Empty));
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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetSessionIdentifier(out var str));
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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetSessionInstanceIdentifier(out var str));
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
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface2.GetProcessId(out var pid));
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
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface.GetGroupingParam(out var groupingId));

            return groupingId;
        }

        /// <summary>
        /// For chanigng the grouping param and supplying the context of said change
        /// </summary>
        /// <param name="groupingId"></param>
        /// <param name="context"></param>
        public void SetGroupingParam(Guid groupingId, Guid context)
        {
            CoreAudioException.ThrowIfFailed(audioSessionControlInterface.SetGroupingParam(groupingId, context));
        }

        /// <summary>
        /// Registers an even client for callbacks
        /// </summary>
        /// <param name="eventClient"></param>
        public void RegisterEventClient(IAudioSessionEventsHandler eventClient)
        {
            // we could have an array or list of listeners if we like
            audioSessionEventCallback = new AudioSessionEventsCallback(eventClient);
            var ptr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(audioSessionEventCallback, CreateComInterfaceFlags.None);
            try
            {
                CoreAudioException.ThrowIfFailed(audioSessionControlInterface.RegisterAudioSessionNotification(ptr));
            }
            finally
            {
                Marshal.Release(ptr);
            }
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
                var ptr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(audioSessionEventCallback, CreateComInterfaceFlags.None);
                try
                {
                    CoreAudioException.ThrowIfFailed(audioSessionControlInterface.UnregisterAudioSessionNotification(ptr));
                }
                finally
                {
                    Marshal.Release(ptr);
                }
                audioSessionEventCallback = null;
            }
        }
    }
}
