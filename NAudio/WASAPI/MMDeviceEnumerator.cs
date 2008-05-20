using System;
using System.Collections.Generic;
using System.Text;
using NAudio.WASAPI.Interfaces;

namespace NAudio.WASAPI
{
    /// <summary>
    /// this class is the starting point for working with WASAPI
    /// 
    /// </summary>
    public class MMDeviceEnumerator
    {
        IMMDeviceEnumerator mmdeviceEnumerator;
        public MMDeviceEnumerator()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("WASAPI is only available on Windows Vista or later.");
            }
            mmdeviceEnumerator = (IMMDeviceEnumerator) new MMDeviceEnumeratorComObject();
            
        }

        


    }
}
