using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates the status of an input stream on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum MftInputStatusFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The input stream can receive more data at this time.
        /// </summary>
        AcceptData = 0x00000001
    }
}
