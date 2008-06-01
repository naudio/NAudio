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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Meter Information
    /// </summary>
    public class AudioMeterInformation
    {
        private IAudioMeterInformation _AudioMeterInformation;
        private EEndpointHardwareSupport _HardwareSupport;
        private AudioMeterInformationChannels _Channels;

        internal AudioMeterInformation(IAudioMeterInformation realInterface)
        {
            int HardwareSupp;

            _AudioMeterInformation = realInterface;
            Marshal.ThrowExceptionForHR(_AudioMeterInformation.QueryHardwareSupport(out HardwareSupp));
            _HardwareSupport = (EEndpointHardwareSupport)HardwareSupp;
            _Channels = new AudioMeterInformationChannels(_AudioMeterInformation);

        }

        /// <summary>
        /// Peak Values
        /// </summary>
        public AudioMeterInformationChannels PeakValues
        {
            get
            {
                return _Channels;
            }
        }

        /// <summary>
        /// Hardware Support
        /// </summary>
        public EEndpointHardwareSupport HardwareSupport
        {
            get
            {
                return _HardwareSupport;
            }
        }

        /// <summary>
        /// Master Peak Value
        /// </summary>
        public float MasterPeakValue
        {
            get
            {
                float result;
                Marshal.ThrowExceptionForHR(_AudioMeterInformation.GetPeakValue(out result));
                return result;
            }
        }
    }
}
