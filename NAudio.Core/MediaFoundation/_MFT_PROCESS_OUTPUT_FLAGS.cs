using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for processing output samples in a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum _MFT_PROCESS_OUTPUT_FLAGS
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// Do not produce output for streams in which the pSample member of the MFT_OUTPUT_DATA_BUFFER structure is NULL. 
        /// </summary>
        MFT_PROCESS_OUTPUT_DISCARD_WHEN_NO_BUFFER = 0x00000001,
        /// <summary>
        /// Regenerates the last output sample.
        /// </summary>
        MFT_PROCESS_OUTPUT_REGENERATE_LAST_OUTPUT = 0x00000002 
    }
}