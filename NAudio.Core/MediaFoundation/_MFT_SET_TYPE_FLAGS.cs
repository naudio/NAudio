using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for the setting or testing the media type on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum _MFT_SET_TYPE_FLAGS
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Test the proposed media type, but do not set it.
        /// </summary>
        MFT_SET_TYPE_TEST_ONLY = 0x00000001
    }
}