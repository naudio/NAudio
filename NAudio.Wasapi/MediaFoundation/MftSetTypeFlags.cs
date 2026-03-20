using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for setting or testing the media type on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum MftSetTypeFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Test the proposed media type, but do not set it.
        /// </summary>
        TestOnly = 0x00000001
    }
}
