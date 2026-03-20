using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output buffer for a Media Foundation transform.
    /// </summary>
    /// <remarks>
    /// Windows SDK name: MFT_OUTPUT_DATA_BUFFER (mftransform.h).
    /// See https://learn.microsoft.com/windows/win32/api/mftransform/ns-mftransform-mft_output_data_buffer
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MftOutputDataBuffer
    {
        /// <summary>
        /// Output stream identifier.
        /// </summary>
        /// <remarks>dwStreamID</remarks>
        public int StreamId;
        /// <summary>
        /// Pointer to the IMFSample interface.
        /// </summary>
        /// <remarks>pSample</remarks>
        public IMFSample Sample;
        /// <summary>
        /// Before calling ProcessOutput, set this member to zero.
        /// </summary>
        /// <remarks>dwStatus</remarks>
        public MftOutputDataBufferFlags Status;
        /// <summary>
        /// Before calling ProcessOutput, set this member to null.
        /// </summary>
        /// <remarks>pEvents</remarks>
        public IMFCollection Events;
    }
}
