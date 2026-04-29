using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Endpoint Volume.
    /// Volume change notifications from COM arrive on a background thread.
    /// If a <see cref="SynchronizationContext"/> is captured at construction time,
    /// notifications are marshaled to that context (e.g. the UI thread).
    /// </summary>
    public class AudioEndpointVolume : IDisposable
    {
        private IAudioEndpointVolume audioEndPointVolume;
        private readonly SynchronizationContext syncContext;
        private AudioEndpointVolumeCallback callBack;

        private Guid notificationGuid = Guid.Empty;

        /// <summary>
        /// GUID to pass to AudioEndpointVolumeCallback
        /// </summary>
        public Guid NotificationGuid {
            get => notificationGuid;
            set => notificationGuid = value;
        }

        /// <summary>
        /// On Volume Notification
        /// </summary>
        public event AudioEndpointVolumeNotificationDelegate OnVolumeNotification;

        /// <summary>
        /// Volume Range
        /// </summary>
        public AudioEndpointVolumeVolumeRange VolumeRange { get; }

        /// <summary>
        /// Hardware Support
        /// </summary>
        public EEndpointHardwareSupport HardwareSupport { get; }

        /// <summary>
        /// Step Information
        /// </summary>
        public AudioEndpointVolumeStepInformation StepInformation { get; }

        /// <summary>
        /// Channels
        /// </summary>
        public AudioEndpointVolumeChannels Channels { get; }

        /// <summary>
        /// Master Volume Level
        /// </summary>
        public float MasterVolumeLevel
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.GetMasterVolumeLevel(out var result));
                return result;
            }
            set
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.SetMasterVolumeLevel(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Master Volume Level Scalar
        /// </summary>
        public float MasterVolumeLevelScalar
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.GetMasterVolumeLevelScalar(out var result));
                return result;
            }
            set
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.SetMasterVolumeLevelScalar(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Mute
        /// </summary>
        public bool Mute
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.GetMute(out var result));
                return result;
            }
            set
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.SetMute(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Volume Step Up
        /// </summary>
        public void VolumeStepUp()
        {
            CoreAudioException.ThrowIfFailed(audioEndPointVolume.VolumeStepUp(ref notificationGuid));
        }

        /// <summary>
        /// Volume Step Down
        /// </summary>
        public void VolumeStepDown()
        {
            CoreAudioException.ThrowIfFailed(audioEndPointVolume.VolumeStepDown(ref notificationGuid));
        }

        /// <summary>
        /// Creates a new Audio endpoint volume
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal AudioEndpointVolume(IntPtr nativePointer)
        {
            syncContext = SynchronizationContext.Current;
            try
            {
                audioEndPointVolume = (IAudioEndpointVolume)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                    nativePointer, CreateObjectFlags.UniqueInstance);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
            Channels = new AudioEndpointVolumeChannels(audioEndPointVolume);
            StepInformation = new AudioEndpointVolumeStepInformation(audioEndPointVolume);
            CoreAudioException.ThrowIfFailed(audioEndPointVolume.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            VolumeRange = new AudioEndpointVolumeVolumeRange(audioEndPointVolume);
            callBack = new AudioEndpointVolumeCallback(this);
            var callBackPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callBack, CreateComInterfaceFlags.None);
            try
            {
                CoreAudioException.ThrowIfFailed(audioEndPointVolume.RegisterControlChangeNotify(callBackPtr));
            }
            finally
            {
                Marshal.Release(callBackPtr);
            }
        }

        internal void FireNotification(AudioVolumeNotificationData notificationData)
        {
            var handler = OnVolumeNotification;
            if (handler != null)
            {
                if (syncContext != null)
                {
                    syncContext.Post(_ => handler(notificationData), null);
                }
                else
                {
                    handler(notificationData);
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (callBack != null)
            {
                var callBackPtr = ComActivation.ComWrappers.GetOrCreateComInterfaceForObject(callBack, CreateComInterfaceFlags.None);
                try
                {
                    audioEndPointVolume.UnregisterControlChangeNotify(callBackPtr);
                }
                finally
                {
                    Marshal.Release(callBackPtr);
                }
                callBack = null;
            }
            // Deterministic release is important: in exclusive mode the device cannot be
            // re-opened until all COM references are released.
            if (audioEndPointVolume != null)
            {
                if ((object)audioEndPointVolume is ComObject co)
                {
                    co.FinalRelease();
                }
                audioEndPointVolume = null;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
