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
        private readonly AudioEndpointVolumeChannels channels;
        private readonly AudioEndpointVolumeStepInformation stepInformation;
        private readonly AudioEndpointVolumeVolumeRange volumeRange;
        private readonly EEndpointHardwareSupport hardwareSupport;
        private AudioEndpointVolumeCallback callBack;

        /// <summary>
        /// On Volume Notification
        /// </summary>
        public event AudioEndpointVolumeNotificationDelegate OnVolumeNotification;

        /// <summary>
        /// Volume Range
        /// </summary>
        public AudioEndpointVolumeVolumeRange VolumeRange
        {
            get
            {
                return volumeRange;
            }
        }

        /// <summary>
        /// Hardware Support
        /// </summary>
        public EEndpointHardwareSupport HardwareSupport
        {
            get
            {
                return hardwareSupport;
            }
        }

        /// <summary>
        /// Step Information
        /// </summary>
        public AudioEndpointVolumeStepInformation StepInformation
        {
            get
            {
                return stepInformation;
            }
        }

        /// <summary>
        /// Channels
        /// </summary>
        public AudioEndpointVolumeChannels Channels
        {
            get
            {
                return channels;
            }
        }

        /// <summary>
        /// Master Volume Level
        /// </summary>
        public float MasterVolumeLevel
        {
            get
            {
                float result;
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMasterVolumeLevel(out result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMasterVolumeLevel(value, Guid.Empty));
            }
        }

        /// <summary>
        /// Master Volume Level Scalar
        /// </summary>
        public float MasterVolumeLevelScalar
        {
            get
            {
                float result;
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMasterVolumeLevelScalar(out result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMasterVolumeLevelScalar(value, Guid.Empty));
            }
        }

        /// <summary>
        /// Mute
        /// </summary>
        public bool Mute
        {
            get
            {
                bool result;
                Marshal.ThrowExceptionForHR(audioEndPointVolume.GetMute(out result));
                return result;
            }
            set
            {
                Marshal.ThrowExceptionForHR(audioEndPointVolume.SetMute(value, Guid.Empty));
            }
        }

        /// <summary>
        /// Volume Step Up
        /// </summary>
        public void VolumeStepUp()
        {
            Marshal.ThrowExceptionForHR(audioEndPointVolume.VolumeStepUp(Guid.Empty));
        }

        /// <summary>
        /// Volume Step Down
        /// </summary>
        public void VolumeStepDown()
        {
            Marshal.ThrowExceptionForHR(audioEndPointVolume.VolumeStepDown(Guid.Empty));
        }

        /// <summary>
        /// Creates a new Audio endpoint volume
        /// </summary>
        /// <param name="realEndpointVolume">IAudioEndpointVolume COM interface</param>
        internal AudioEndpointVolume(IAudioEndpointVolume realEndpointVolume)
        {
            uint hardwareSupp;

            audioEndPointVolume = realEndpointVolume;
            channels = new AudioEndpointVolumeChannels(audioEndPointVolume);
            stepInformation = new AudioEndpointVolumeStepInformation(audioEndPointVolume);
            Marshal.ThrowExceptionForHR(audioEndPointVolume.QueryHardwareSupport(out hardwareSupp));
            hardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            volumeRange = new AudioEndpointVolumeVolumeRange(audioEndPointVolume);
            callBack = new AudioEndpointVolumeCallback(this);
            Marshal.ThrowExceptionForHR(audioEndPointVolume.RegisterControlChangeNotify(callBack));
        }
        
        internal void FireNotification(AudioVolumeNotificationData notificationData)
        {
            AudioEndpointVolumeNotificationDelegate del = OnVolumeNotification;
            if (del != null)
            {
                del(notificationData);
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
                Marshal.ThrowExceptionForHR(audioEndPointVolume.UnregisterControlChangeNotify(callBack));
                callBack = null;
            }
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
