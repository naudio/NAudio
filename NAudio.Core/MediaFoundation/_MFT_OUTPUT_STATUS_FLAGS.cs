using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates whether a Media Foundation transform (MFT) can produce output data.
    /// </summary>
    [Flags]
    public enum _MFT_OUTPUT_STATUS_FLAGS
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// There is a sample available for at least one output stream.
        /// </summary>
        MFT_OUTPUT_STATUS_SAMPLE_READY = 0x00000001
    }
}