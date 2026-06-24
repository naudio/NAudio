using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for the ProcessOutput method output data buffers.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_OUTPUT_DATA_BUFFER_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_output_data_buffer_flags">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftOutputDataBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The MFT can still generate output from this stream without receiving any more input.
        /// </summary>
        /// <remarks>MFT_OUTPUT_DATA_BUFFER_INCOMPLETE</remarks>
        Incomplete = 0x01000000,
        /// <summary>
        /// The format has changed on this output stream, or there is a new preferred format for this stream.
        /// </summary>
        /// <remarks>MFT_OUTPUT_DATA_BUFFER_FORMAT_CHANGE</remarks>
        FormatChange = 0x00000100,
        /// <summary>
        /// The MFT has removed this output stream.
        /// </summary>
        /// <remarks>MFT_OUTPUT_DATA_BUFFER_STREAM_END</remarks>
        StreamEnd = 0x00000200,
        /// <summary>
        /// There is no sample ready for this stream.
        /// </summary>
        /// <remarks>MFT_OUTPUT_DATA_BUFFER_NO_SAMPLE</remarks>
        NoSample = 0x00000300
    }
}
