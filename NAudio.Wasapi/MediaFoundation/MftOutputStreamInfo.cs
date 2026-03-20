using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output stream on a Media Foundation transform (MFT).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MftOutputStreamInfo
    {
        /// <summary>
        /// Bitwise OR of zero or more flags from the MftOutputStreamInfoFlags enumeration.
        /// </summary>
        public MftOutputStreamInfoFlags Flags;
        /// <summary>
        /// Minimum size of each output buffer, in bytes.
        /// </summary>
        public int Size;
        /// <summary>
        /// The memory alignment required for output buffers.
        /// </summary>
        public int Alignment;
    }
}
