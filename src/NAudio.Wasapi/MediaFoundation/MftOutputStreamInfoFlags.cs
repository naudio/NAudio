using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an output stream on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_OUTPUT_STREAM_INFO_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_output_stream_info_flags">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftOutputStreamInfoFlags
    {
        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,
        /// <summary>
        /// Each media sample of output data contains complete, unbroken units of data.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_WHOLE_SAMPLES</remarks>
        WholeSamples = 0x00000001,
        /// <summary>
        /// Each output sample contains exactly one unit of data.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER</remarks>
        SingleSamplePerBuffer = 0x00000002,
        /// <summary>
        /// All output samples are the same size.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_FIXED_SAMPLE_SIZE</remarks>
        FixedSampleSize = 0x00000004,
        /// <summary>
        /// The MFT can discard the output data from this output stream, if requested.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_DISCARDABLE</remarks>
        Discardable = 0x00000008,
        /// <summary>
        /// This output stream is optional.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_OPTIONAL</remarks>
        Optional = 0x00000010,
        /// <summary>
        /// The MFT provides the output samples for this stream, either by allocating them internally or by operating directly on the input samples.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_PROVIDES_SAMPLES</remarks>
        ProvidesSamples = 0x00000100,
        /// <summary>
        /// The MFT can either provide output samples for this stream or use samples that the client allocates.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES</remarks>
        CanProvideSamples = 0x00000200,
        /// <summary>
        /// The MFT does not require the client to process the output for this stream.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_LAZY_READ</remarks>
        LazyRead = 0x00000400,
        /// <summary>
        /// The MFT might remove this output stream during streaming.
        /// </summary>
        /// <remarks>MFT_OUTPUT_STREAM_REMOVABLE</remarks>
        Removable = 0x00000800
    }
}
