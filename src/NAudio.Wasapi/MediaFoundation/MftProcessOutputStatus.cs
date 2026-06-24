using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines status flags for the ProcessOutput method on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_PROCESS_OUTPUT_STATUS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_process_output_status">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftProcessOutputStatus
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The Media Foundation transform (MFT) has created one or more new output streams.
        /// </summary>
        /// <remarks>MFT_PROCESS_OUTPUT_STATUS_NEW_STREAMS</remarks>
        NewStreams = 0x00000100
    }
}
