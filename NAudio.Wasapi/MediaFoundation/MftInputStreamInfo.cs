using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an input stream on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MFT_INPUT_STREAM_INFO (mftransform.h).
    /// See https://learn.microsoft.com/windows/win32/api/mftransform/ns-mftransform-mft_input_stream_info
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct MftInputStreamInfo
    {
        /// <summary>
        /// Maximum amount of time between an input sample and the corresponding output sample, in 100-nanosecond units.
        /// </summary>
        /// <remarks>hnsMaxLatency</remarks>
        public long MaxLatency;
        /// <summary>
        /// Bitwise OR of zero or more flags from the MftInputStreamInfoFlags enumeration.
        /// </summary>
        /// <remarks>dwFlags</remarks>
        public MftInputStreamInfoFlags Flags;
        /// <summary>
        /// The minimum size of each input buffer, in bytes.
        /// </summary>
        /// <remarks>cbSize</remarks>
        public int Size;
        /// <summary>
        /// Maximum amount of input data, in bytes, that the MFT holds to perform lookahead.
        /// </summary>
        /// <remarks>cbMaxLookahead</remarks>
        public int MaxLookahead;
        /// <summary>
        /// The memory alignment required for input buffers. If the MFT does not require a specific alignment, the value is zero.
        /// </summary>
        /// <remarks>cbAlignment</remarks>
        public int Alignment;
    }
}
