using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates whether a Media Foundation transform (MFT) can produce output data.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_OUTPUT_STATUS_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_output_status_flags">MS Learn</see>.
    /// </remarks>
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
        /// <remarks>MFT_OUTPUT_STATUS_SAMPLE_READY</remarks>
        SampleReady = 0x00000001
    }
}
