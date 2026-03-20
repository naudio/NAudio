using System;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Process output status flags.
    /// </summary>
    [Flags]
    public enum MftProcessOutputStatus
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// The Media Foundation transform (MFT) has created one or more new output streams.
        /// </summary>
        NewStreams = 0x00000100
    }
}
