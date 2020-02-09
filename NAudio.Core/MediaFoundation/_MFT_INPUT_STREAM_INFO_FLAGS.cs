using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an input stream on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum _MFT_INPUT_STREAM_INFO_FLAGS
    {
        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,
        /// <summary>
        /// Each media sample (IMFSample interface) of input data must contain complete, unbroken units of data. 
        /// </summary>
        MFT_INPUT_STREAM_WHOLE_SAMPLES = 0x00000001,
        /// <summary>
        /// Each media sample that the client provides as input must contain exactly one unit of data, as defined for the MFT_INPUT_STREAM_WHOLE_SAMPLES flag.
        /// </summary>
        MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER = 0x00000002,
        /// <summary>
        /// All input samples must be the same size.
        /// </summary>
        MFT_INPUT_STREAM_FIXED_SAMPLE_SIZE = 0x00000004,
        /// <summary>
        /// MTF Input Stream Holds buffers
        /// </summary>
        MFT_INPUT_STREAM_HOLDS_BUFFERS = 0x00000008,
        /// <summary>
        /// The MFT does not hold input samples after the IMFTransform::ProcessInput method returns.
        /// </summary>
        MFT_INPUT_STREAM_DOES_NOT_ADDREF = 0x00000100,
        /// <summary>
        /// This input stream can be removed by calling IMFTransform::DeleteInputStream.
        /// </summary>
        MFT_INPUT_STREAM_REMOVABLE = 0x00000200,
        /// <summary>
        /// This input stream is optional. 
        /// </summary>
        MFT_INPUT_STREAM_OPTIONAL = 0x00000400,
        /// <summary>
        /// The MFT can perform in-place processing.
        /// </summary>
        MFT_INPUT_STREAM_PROCESSES_IN_PLACE = 0x00000800
    }
}