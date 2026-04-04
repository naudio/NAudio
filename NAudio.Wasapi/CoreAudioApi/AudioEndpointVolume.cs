/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Threading;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

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
        private IntPtr nativePointer;
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
            this.nativePointer = nativePointer;
            audioEndPointVolume = (IAudioEndpointVolume)Marshal.GetObjectForIUnknown(nativePointer);
            Channels = new AudioEndpointVolumeChannels(audioEndPointVolume);
            StepInformation = new AudioEndpointVolumeStepInformation(audioEndPointVolume);
            CoreAudioException.ThrowIfFailed(audioEndPointVolume.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            VolumeRange = new AudioEndpointVolumeVolumeRange(audioEndPointVolume);
            callBack = new AudioEndpointVolumeCallback(this);
            var callBackPtr = Marshal.GetComInterfaceForObject<AudioEndpointVolumeCallback, IAudioEndpointVolumeCallback>(callBack);
            CoreAudioException.ThrowIfFailed(audioEndPointVolume.RegisterControlChangeNotify(callBackPtr));
            Marshal.Release(callBackPtr);
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
                var callBackPtr = Marshal.GetComInterfaceForObject<AudioEndpointVolumeCallback, IAudioEndpointVolumeCallback>(callBack);
                audioEndPointVolume.UnregisterControlChangeNotify(callBackPtr);
                Marshal.Release(callBackPtr);
                callBack = null;
            }
            if (audioEndPointVolume != null)
            {
                audioEndPointVolume = null;
            }
            // Deterministic release is important: in exclusive mode the device cannot be
            // re-opened until all COM references are released.
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
