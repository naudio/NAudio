using System;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Client Buffer Flags
    /// </summary>
    [Flags]
    public enum AudioClientBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY
        /// </summary>
        DataDiscontinuity = 0x1,
        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_SILENT
        /// </summary>
        Silent = 0x2,
        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR
        /// </summary>
        TimestampError = 0x4

    }
}
