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
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Endpoint Volume
    /// </summary>
    public class AudioEndpointVolume : IDisposable
    {
        private readonly IAudioEndpointVolume audioEndPointVolume;
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
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMasterVolumeLevel(out var result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMasterVolumeLevel(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Master Volume Level Scalar
        /// </summary>
        public float MasterVolumeLevelScalar
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMasterVolumeLevelScalar(out var result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMasterVolumeLevelScalar(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Mute
        /// </summary>
        public bool Mute
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMute(out var result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMute(value, ref notificationGuid));
            }
        }

        /// <summary>
        /// Volume Step Up
        /// </summary>
        public void VolumeStepUp()
        {
            Marshal.ThrowExceptionForHR(audioEndPointVolume.VolumeStepUp(ref notificationGuid));
        }

        /// <summary>
        /// Volume Step Down
        /// </summary>
        public void VolumeStepDown()
        {
            Marshal.ThrowExceptionForHR(audioEndPointVolume.VolumeStepDown(ref notificationGuid));
        }

        /// <summary>
        /// Creates a new Audio endpoint volume
        /// </summary>
        /// <param name="realEndpointVolume">IAudioEndpointVolume COM interface</param>
        internal AudioEndpointVolume(IAudioEndpointVolume realEndpointVolume)
        {
            audioEndPointVolume = realEndpointVolume;
            Channels = new AudioEndpointVolumeChannels(audioEndPointVolume);
            StepInformation = new AudioEndpointVolumeStepInformation(audioEndPointVolume);
            Marshal.ThrowExceptionForHR(audioEndPointVolume.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            VolumeRange = new AudioEndpointVolumeVolumeRange(audioEndPointVolume);
            callBack = new AudioEndpointVolumeCallback(this);
            Marshal.ThrowExceptionForHR(audioEndPointVolume.RegisterControlChangeNotify(callBack));
        }
        
        internal void FireNotification(AudioVolumeNotificationData notificationData)
        {
            OnVolumeNotification?.Invoke(notificationData);
        }
        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (callBack != null)
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.UnregisterControlChangeNotify(callBack));
                callBack = null;
            }
            Marshal.ReleaseComObject(audioEndPointVolume);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~AudioEndpointVolume()
        {
            Dispose();
        }

        #endregion

    }
}
