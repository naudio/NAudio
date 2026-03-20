using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates whether a Media Foundation transform (MFT) can produce output data.
    /// </summary>
    [Flags]
    public enum MftOutputStatusFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// There is a sample available for at least one output stream.
        /// </summary>
        SampleReady = 0x00000001
    }
}
