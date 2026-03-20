using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Describes an input stream on a Media Foundation transform (MFT).
    /// </summary>
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
        WholeSamples = 0x00000001,
        /// <summary>
        /// Each media sample must contain exactly one unit of data.
        /// </summary>
        SingleSamplePerBuffer = 0x00000002,
        /// <summary>
        /// All input samples must be the same size.
        /// </summary>
        FixedSampleSize = 0x00000004,
        /// <summary>
        /// The MFT holds input buffers.
        /// </summary>
        HoldsBuffers = 0x00000008,
        /// <summary>
        /// The MFT does not hold input samples after ProcessInput returns.
        /// </summary>
        DoesNotAddRef = 0x00000100,
        /// <summary>
        /// This input stream can be removed by calling DeleteInputStream.
        /// </summary>
        Removable = 0x00000200,
        /// <summary>
        /// This input stream is optional.
        /// </summary>
        Optional = 0x00000400,
        /// <summary>
        /// The MFT can perform in-place processing.
        /// </summary>
        ProcessesInPlace = 0x00000800
    }
}
