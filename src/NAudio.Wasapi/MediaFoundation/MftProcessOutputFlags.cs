using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for processing output samples in a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_PROCESS_OUTPUT_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_process_output_flags">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftProcessOutputFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Do not produce output for streams in which the sample is null.
        /// </summary>
        /// <remarks>MFT_PROCESS_OUTPUT_DISCARD_WHEN_NO_BUFFER</remarks>
        DiscardWhenNoBuffer = 0x00000001,
        /// <summary>
        /// Regenerates the last output sample.
        /// </summary>
        /// <remarks>MFT_PROCESS_OUTPUT_REGENERATE_LAST_OUTPUT</remarks>
        RegenerateLastOutput = 0x00000002
    }
}
