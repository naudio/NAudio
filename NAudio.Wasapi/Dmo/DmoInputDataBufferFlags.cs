using System;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Input Data Buffer Flags
    /// </summary>
    [Flags]
    public enum DmoInputDataBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// DMO_INPUT_DATA_BUFFERF_SYNCPOINT
        /// </summary>
        SyncPoint = 0x00000001,
        /// <summary>
        /// DMO_INPUT_DATA_BUFFERF_TIME
        /// </summary>
        Time = 0x00000002,
        /// <summary>
        /// DMO_INPUT_DATA_BUFFERF_TIMELENGTH
        /// </summary>
        TimeLength = 0x00000004
    }
}
