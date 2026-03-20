using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an output stream on a Media Foundation transform (MFT).
    /// </summary>
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
        WholeSamples = 0x00000001,
        /// <summary>
        /// Each output sample contains exactly one unit of data.
        /// </summary>
        SingleSamplePerBuffer = 0x00000002,
        /// <summary>
        /// All output samples are the same size.
        /// </summary>
        FixedSampleSize = 0x00000004,
        /// <summary>
        /// The MFT can discard the output data from this output stream, if requested.
        /// </summary>
        Discardable = 0x00000008,
        /// <summary>
        /// This output stream is optional.
        /// </summary>
        Optional = 0x00000010,
        /// <summary>
        /// The MFT provides the output samples for this stream, either by allocating them internally or by operating directly on the input samples.
        /// </summary>
        ProvidesSamples = 0x00000100,
        /// <summary>
        /// The MFT can either provide output samples for this stream or use samples that the client allocates.
        /// </summary>
        CanProvideSamples = 0x00000200,
        /// <summary>
        /// The MFT does not require the client to process the output for this stream.
        /// </summary>
        LazyRead = 0x00000400,
        /// <summary>
        /// The MFT might remove this output stream during streaming.
        /// </summary>
        Removable = 0x00000800
    }
}
