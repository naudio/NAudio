using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an input stream on a Media Foundation transform (MFT)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_INPUT_STREAM_INFO
    {
        /// <summary>
        /// Maximum amount of time between an input sample and the corresponding output sample, in 100-nanosecond units.
        /// </summary>
        public long hnsMaxLatency;
        /// <summary>
        /// Bitwise OR of zero or more flags from the _MFT_INPUT_STREAM_INFO_FLAGS enumeration.
        /// </summary>
        public _MFT_INPUT_STREAM_INFO_FLAGS dwFlags;
        /// <summary>
        /// The minimum size of each input buffer, in bytes.
        /// </summary>
        public int cbSize;
        /// <summary>
        /// Maximum amount of input data, in bytes, that the MFT holds to perform lookahead.
        /// </summary>
        public int cbMaxLookahead;
        /// <summary>
        /// The memory alignment required for input buffers. If the MFT does not require a specific alignment, the value is zero.
        /// </summary>
        public int cbAlignment;
    }
}
