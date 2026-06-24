using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output stream on a Media Foundation transform (MFT).
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MFT_OUTPUT_STREAM_INFO (mftransform.h).
    /// See https://learn.microsoft.com/windows/win32/api/mftransform/ns-mftransform-mft_output_stream_info
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct MftOutputStreamInfo
    {
        /// <summary>
        /// Bitwise OR of zero or more flags from the MftOutputStreamInfoFlags enumeration.
        /// </summary>
        /// <remarks>dwFlags</remarks>
        public MftOutputStreamInfoFlags Flags;
        /// <summary>
        /// Minimum size of each output buffer, in bytes.
        /// </summary>
        /// <remarks>cbSize</remarks>
        public int Size;
        /// <summary>
        /// The memory alignment required for output buffers.
        /// </summary>
        /// <remarks>cbAlignment</remarks>
        public int Alignment;
    }
}
