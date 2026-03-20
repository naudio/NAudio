using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for the ProcessOutput method output data buffers.
    /// </summary>
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
        Incomplete = 0x01000000,
        /// <summary>
        /// The format has changed on this output stream, or there is a new preferred format for this stream.
        /// </summary>
        FormatChange = 0x00000100,
        /// <summary>
        /// The MFT has removed this output stream.
        /// </summary>
        StreamEnd = 0x00000200,
        /// <summary>
        /// There is no sample ready for this stream.
        /// </summary>
        NoSample = 0x00000300
    }
}
