using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AUDCLNT_STREAMFLAGS
    /// </summary>
    [Flags]
    public enum AudioClientStreamFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_CROSSPROCESS
        /// </summary>
        CrossProcess = 0x00010000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_LOOPBACK
        /// </summary>
        Loopback = 0x00020000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_EVENTCALLBACK 
        /// </summary>
        EventCallback = 0x00040000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_NOPERSIST     
        /// </summary>
        NoPersist = 0x00080000,
    }
}
