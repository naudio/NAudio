using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Defines flags for processing output samples in a Media Foundation transform (MFT).
    /// </summary>
    [Flags]
    public enum MftProcessOutputFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Do not produce output for streams in which the sample is null.
        /// </summary>
        DiscardWhenNoBuffer = 0x00000001,
        /// <summary>
        /// Regenerates the last output sample.
        /// </summary>
        RegenerateLastOutput = 0x00000002
    }
}
