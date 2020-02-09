using System;

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
