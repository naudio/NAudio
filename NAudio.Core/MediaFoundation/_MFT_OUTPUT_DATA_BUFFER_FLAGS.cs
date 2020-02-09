using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for the IMFTransform::ProcessOutput method. 
    /// </summary>
    [Flags]
    public enum _MFT_OUTPUT_DATA_BUFFER_FLAGS
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The MFT can still generate output from this stream without receiving any more input. 
        /// </summary>
        MFT_OUTPUT_DATA_BUFFER_INCOMPLETE = 0x01000000,
        /// <summary>
        /// The format has changed on this output stream, or there is a new preferred format for this stream. 
        /// </summary>
        MFT_OUTPUT_DATA_BUFFER_FORMAT_CHANGE = 0x00000100,
        /// <summary>
        /// The MFT has removed this output stream. 
        /// </summary>
        MFT_OUTPUT_DATA_BUFFER_STREAM_END = 0x00000200,
        /// <summary>
        /// There is no sample ready for this stream.
        /// </summary>
        MFT_OUTPUT_DATA_BUFFER_NO_SAMPLE = 0x00000300

    };
}