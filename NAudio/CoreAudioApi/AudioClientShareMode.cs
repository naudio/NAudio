using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AUDCLNT_SHAREMODE
    /// </summary>
    public enum AudioClientShareMode
    {
        /// <summary>
        /// AUDCLNT_SHAREMODE_SHARED,
        /// </summary>
        Shared,
        /// <summary>
        /// AUDCLNT_SHAREMODE_EXCLUSIVE
        /// </summary>
        Exclusive,
    }
}
