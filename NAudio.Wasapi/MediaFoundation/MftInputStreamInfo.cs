using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an input stream on a Media Foundation transform (MFT).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MftInputStreamInfo
    {
        /// <summary>
        /// Maximum amount of time between an input sample and the corresponding output sample, in 100-nanosecond units.
        /// </summary>
        public long MaxLatency;
        /// <summary>
        /// Bitwise OR of zero or more flags from the MftInputStreamInfoFlags enumeration.
        /// </summary>
        public MftInputStreamInfoFlags Flags;
        /// <summary>
        /// The minimum size of each input buffer, in bytes.
        /// </summary>
        public int Size;
        /// <summary>
        /// Maximum amount of input data, in bytes, that the MFT holds to perform lookahead.
        /// </summary>
        public int MaxLookahead;
        /// <summary>
        /// The memory alignment required for input buffers. If the MFT does not require a specific alignment, the value is zero.
        /// </summary>
        public int Alignment;
    }
}
