using System;
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
        /// Pointer to the IMFSample interface (raw COM IUnknown*).
        /// </summary>
        /// <remarks>pSample</remarks>
        public IntPtr Sample;
        /// <summary>
        /// Before calling ProcessOutput, set this member to zero.
        /// </summary>
        /// <remarks>dwStatus</remarks>
        public MftOutputDataBufferFlags Status;
        /// <summary>
        /// Pointer to an IMFCollection of MF_EVENT objects (raw COM IUnknown*).
        /// Before calling ProcessOutput, set to IntPtr.Zero.
        /// </summary>
        /// <remarks>pEvents</remarks>
        public IntPtr Events;
    }
}
