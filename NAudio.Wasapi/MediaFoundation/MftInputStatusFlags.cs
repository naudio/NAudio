using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Indicates the status of an input stream on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_INPUT_STATUS_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_input_status_flags">MS Learn</see>.
    /// </remarks>
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
        /// <remarks>MFT_INPUT_STATUS_ACCEPT_DATA</remarks>
        AcceptData = 0x00000001
    }
}
