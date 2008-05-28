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
    /// Audio Endpoint Volume Volume Range
    /// </summary>
    public class AudioEndpointVolumeVolumeRange
    {
        float _VolumeMindB;
        float _VolumeMaxdB;
        float _VolumeIncrementdB;

        internal AudioEndpointVolumeVolumeRange(IAudioEndpointVolume parent)
        {
            Marshal.ThrowExceptionForHR(parent.GetVolumeRange(out _VolumeMindB,out _VolumeMaxdB,out _VolumeIncrementdB));
        }

        /// <summary>
        /// Minimum Decibels
        /// </summary>
        public float MinDecibels
        {
            get
            {
                return _VolumeMindB;
            }
        }

        /// <summary>
        /// Maximum Decibels
        /// </summary>
        public float MaxDecibels
        {
            get
            {
                return _VolumeMaxdB;
            }
        }

        /// <summary>
        /// Increment Decibels
        /// </summary>
        public float IncrementDecibels
        {
            get
            {
                return _VolumeIncrementdB;
            }
        }
    }
}
