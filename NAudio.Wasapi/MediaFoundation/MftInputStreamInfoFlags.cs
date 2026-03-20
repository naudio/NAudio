using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an input stream on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: <c>_MFT_INPUT_STREAM_INFO_FLAGS</c>.
    /// Defined in <c>mftransform.h</c>.
    /// See <see href="https://learn.microsoft.com/windows/win32/api/mftransform/ne-mftransform-_mft_input_stream_info_flags">MS Learn</see>.
    /// </remarks>
    [Flags]
    public enum MftInputStreamInfoFlags
    {
        /// <summary>
        /// No flags set
        /// </summary>
        None = 0,
        /// <summary>
        /// Each media sample of input data must contain complete, unbroken units of data.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_WHOLE_SAMPLES</remarks>
        WholeSamples = 0x00000001,
        /// <summary>
        /// Each media sample must contain exactly one unit of data.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER</remarks>
        SingleSamplePerBuffer = 0x00000002,
        /// <summary>
        /// All input samples must be the same size.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_FIXED_SAMPLE_SIZE</remarks>
        FixedSampleSize = 0x00000004,
        /// <summary>
        /// The MFT holds input buffers.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_HOLDS_BUFFERS</remarks>
        HoldsBuffers = 0x00000008,
        /// <summary>
        /// The MFT does not hold input samples after ProcessInput returns.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_DOES_NOT_ADDREF</remarks>
        DoesNotAddRef = 0x00000100,
        /// <summary>
        /// This input stream can be removed by calling DeleteInputStream.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_REMOVABLE</remarks>
        Removable = 0x00000200,
        /// <summary>
        /// This input stream is optional.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_OPTIONAL</remarks>
        Optional = 0x00000400,
        /// <summary>
        /// The MFT can perform in-place processing.
        /// </summary>
        /// <remarks>MFT_INPUT_STREAM_PROCESSES_IN_PLACE</remarks>
        ProcessesInPlace = 0x00000800
    }
}
