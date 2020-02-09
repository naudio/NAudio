using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output stream on a Media Foundation transform (MFT).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_OUTPUT_STREAM_INFO 
    {
        /// <summary>
        /// Bitwise OR of zero or more flags from the _MFT_OUTPUT_STREAM_INFO_FLAGS enumeration.
        /// </summary>
        public _MFT_OUTPUT_STREAM_INFO_FLAGS dwFlags;
        /// <summary>
        /// Minimum size of each output buffer, in bytes.
        /// </summary>
        public int cbSize;
        /// <summary>
        /// The memory alignment required for output buffers.
        /// </summary>
        public int cbAlignment;
    }
}