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
// modified for NAudio
using System;
using System.Collections.Generic;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// MM Device
    /// </summary>
    public class MMDevice
    {
        #region Variables
        private IMMDevice deviceInterface;
        private PropertyStore _PropertyStore;
        private AudioMeterInformation _AudioMeterInformation;
        private AudioEndpointVolume _AudioEndpointVolume;

        #endregion

        #region Guids
        private static Guid IID_IAudioMeterInformation = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
        private static Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        private static Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
        #endregion

        #region Init
        private void GetPropertyInformation()
        {
            IPropertyStore propstore;
            Marshal.ThrowExceptionForHR(deviceInterface.OpenPropertyStore(StorageAccessMode.Read, out propstore));
            _PropertyStore = new PropertyStore(propstore);
        }

        private AudioClient GetAudioClient()
        {
            object result;
            Marshal.ThrowExceptionForHR(deviceInterface.Activate(ref IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero, out result));
            return new AudioClient(result as IAudioClient);
        }

        private void GetAudioMeterInformation()
        {
            object result;
            Marshal.ThrowExceptionForHR(deviceInterface.Activate(ref IID_IAudioMeterInformation, ClsCtx.ALL, IntPtr.Zero, out result));
            _AudioMeterInformation = new AudioMeterInformation(result as IAudioMeterInformation);
        }

        private void GetAudioEndpointVolume()
        {
            object result;
            Marshal.ThrowExceptionForHR(deviceInterface.Activate(ref IID_IAudioEndpointVolume, ClsCtx.ALL, IntPtr.Zero, out result));
            _AudioEndpointVolume = new AudioEndpointVolume(result as IAudioEndpointVolume);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Audio Client
        /// </summary>
        public AudioClient AudioClient
        {
            get
            {
                // now makes a new one each call to allow caller to manage when to dispose
                // n.b. should probably not be a property anymore
                return GetAudioClient();
            }
        }

        /// <summary>
        /// Audio Meter Information
        /// </summary>
        public AudioMeterInformation AudioMeterInformation
        {
            get
            {
                if (_AudioMeterInformation == null)
                    GetAudioMeterInformation();

                return _AudioMeterInformation;
            }
        }

        /// <summary>
        /// Audio Endpoint Volume
        /// </summary>
        public AudioEndpointVolume AudioEndpointVolume
        {
            get
            {
                if (_AudioEndpointVolume == null)
                    GetAudioEndpointVolume();

                return _AudioEndpointVolume;
            }
        }

        /// <summary>
        /// Properties
        /// </summary>
        public PropertyStore Properties
        {
            get
            {
                if (_PropertyStore == null)
                    GetPropertyInformation();
                return _PropertyStore;
            }
        }

        /// <summary>
        /// Friendly name for the endpoint
        /// </summary>
        public string FriendlyName
        {
            get
            {
                if (_PropertyStore == null)
                {
                    GetPropertyInformation();
                }
                if (_PropertyStore.Contains(PropertyKeys.PKEY_DeviceInterface_FriendlyName))
                {
                    return (string)_PropertyStore[PropertyKeys.PKEY_DeviceInterface_FriendlyName].Value;
                }
                else
                    return "Unknown";
            }
        }

       /// <summary>
       /// Friendly name of device
       /// </summary>
        public string DeviceFriendlyName
        {
            get
            {
                if (_PropertyStore == null)
                {
                    GetPropertyInformation();
                }
                if (_PropertyStore.Contains(PropertyKeys.PKEY_Device_FriendlyName))
                {
                    return (string)_PropertyStore[PropertyKeys.PKEY_Device_FriendlyName].Value;
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        /// <summary>
        /// Device ID
        /// </summary>
        public string ID
        {
            get
            {
                string Result;
                Marshal.ThrowExceptionForHR(deviceInterface.GetId(out Result));
                return Result;
            }
        }

        /// <summary>
        /// Data Flow
        /// </summary>
        public DataFlow DataFlow
        {
            get
            {
                DataFlow Result;
                IMMEndpoint ep = deviceInterface as IMMEndpoint;
                ep.GetDataFlow(out Result);
                return Result;
            }
        }

        /// <summary>
        /// Device State
        /// </summary>
        public DeviceState State
        {
            get
            {
                DeviceState Result;
                Marshal.ThrowExceptionForHR(deviceInterface.GetState(out Result));
                return Result;
            }
        }
        #endregion

        #region Constructor
        internal MMDevice(IMMDevice realDevice)
        {
            deviceInterface = realDevice;
        }
        #endregion

        /// <summary>
        /// To string
        /// </summary>
        public override string ToString()
        {
            return FriendlyName;
        }

    }
}
