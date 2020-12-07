using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates the status of an input stream on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum _MFT_INPUT_STATUS_FLAGS
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The input stream can receive more data at this time.
        /// </summary>
        MFT_INPUT_STATUS_ACCEPT_DATA = 0x00000001
    }
}