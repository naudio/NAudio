using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Output Data Buffer Flags
    /// </summary>
    [Flags]
    public enum DmoOutputDataBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// DMO_OUTPUT_DATA_BUFFERF_SYNCPOINT
        /// </summary>
        SyncPoint = 0x00000001,
        /// <summary>
        /// DMO_OUTPUT_DATA_BUFFERF_TIME
        /// </summary>
        Time = 0x00000002,
        /// <summary>
        /// DMO_OUTPUT_DATA_BUFFERF_TIMELENGTH
        /// </summary>
        TimeLength = 0x00000004,
        /// <summary>
        /// DMO_OUTPUT_DATA_BUFFERF_INCOMPLETE
        /// </summary>
        Incomplete = 0x01000000
    }
}
