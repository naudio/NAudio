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
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Meter Information
    /// </summary>
    public class AudioMeterInformation : IDisposable
    {
        private IAudioMeterInformation audioMeterInformation;
        private IntPtr nativePointer;

        /// <summary>
        /// Creates a new AudioMeterInformation wrapper — ownership of the COM pointer is transferred.
        /// </summary>
        /// <param name="nativePointer">Raw COM pointer — ownership is transferred to this instance</param>
        internal AudioMeterInformation(IntPtr nativePointer)
        {
            this.nativePointer = nativePointer;
            audioMeterInformation = (IAudioMeterInformation)Marshal.GetObjectForIUnknown(nativePointer);
            CoreAudioException.ThrowIfFailed(audioMeterInformation.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            PeakValues = new AudioMeterInformationChannels(audioMeterInformation);
        }

        /// <summary>
        /// Creates a new AudioMeterInformation wrapper from a borrowed interface (e.g. QI from AudioSessionControl).
        /// This instance does not own the COM pointer.
        /// </summary>
        /// <param name="borrowed">IAudioMeterInformation obtained via QI on an existing RCW</param>
        internal AudioMeterInformation(IAudioMeterInformation borrowed)
        {
            audioMeterInformation = borrowed;
            CoreAudioException.ThrowIfFailed(audioMeterInformation.QueryHardwareSupport(out var hardwareSupp));
            HardwareSupport = (EEndpointHardwareSupport)hardwareSupp;
            PeakValues = new AudioMeterInformationChannels(audioMeterInformation);
        }

        /// <summary>
        /// Peak Values
        /// </summary>
        public AudioMeterInformationChannels PeakValues { get; }

        /// <summary>
        /// Hardware Support
        /// </summary>
        public EEndpointHardwareSupport HardwareSupport { get; }

        /// <summary>
        /// Master Peak Value
        /// </summary>
        public float MasterPeakValue
        {
            get
            {
                CoreAudioException.ThrowIfFailed(audioMeterInformation.GetPeakValue(out var result));
                return result;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioMeterInformation != null)
            {
                audioMeterInformation = null;
            }
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
