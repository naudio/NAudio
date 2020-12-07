using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an output stream on a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum _MFT_OUTPUT_STREAM_INFO_FLAGS
    {
        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,
        /// <summary>
        /// Each media sample (IMFSample interface) of output data from the MFT contains complete, unbroken units of data.
        /// </summary>
        MFT_OUTPUT_STREAM_WHOLE_SAMPLES             = 0x00000001, 
        /// <summary>
        /// Each output sample contains exactly one unit of data, as defined for the MFT_OUTPUT_STREAM_WHOLE_SAMPLES flag.
        /// </summary>
        MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER  = 0x00000002, 
        /// <summary>
        /// All output samples are the same size.
        /// </summary>
        MFT_OUTPUT_STREAM_FIXED_SAMPLE_SIZE         = 0x00000004, 
        /// <summary>
        /// The MFT can discard the output data from this output stream, if requested by the client.
        /// </summary>
        MFT_OUTPUT_STREAM_DISCARDABLE               = 0x00000008, 
        /// <summary>
        /// This output stream is optional.
        /// </summary>
        MFT_OUTPUT_STREAM_OPTIONAL                  = 0x00000010, 
        /// <summary>
        /// The MFT provides the output samples for this stream, either by allocating them internally or by operating directly on the input samples.
        /// </summary>
        MFT_OUTPUT_STREAM_PROVIDES_SAMPLES          = 0x00000100, 
        /// <summary>
        /// The MFT can either provide output samples for this stream or it can use samples that the client allocates. 
        /// </summary>
        MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES       = 0x00000200, 
        /// <summary>
        /// The MFT does not require the client to process the output for this stream. 
        /// </summary>
        MFT_OUTPUT_STREAM_LAZY_READ                 = 0x00000400, 
        /// <summary>
        /// The MFT might remove this output stream during streaming.
        /// </summary>
        MFT_OUTPUT_STREAM_REMOVABLE                 = 0x00000800 
    }
}