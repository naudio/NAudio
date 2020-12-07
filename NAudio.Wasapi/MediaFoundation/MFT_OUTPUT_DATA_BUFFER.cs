using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output buffer for a Media Foundation transform. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MFT_OUTPUT_DATA_BUFFER
    {
        /// <summary>
        /// Output stream identifier.
        /// </summary>
        public int dwStreamID;
        /// <summary>
        /// Pointer to the IMFSample interface. 
        /// </summary>
        public IMFSample pSample;
        /// <summary>
        /// Before calling ProcessOutput, set this member to zero.
        /// </summary>
        public _MFT_OUTPUT_DATA_BUFFER_FLAGS dwStatus;
        /// <summary>
        /// Before calling ProcessOutput, set this member to NULL.
        /// </summary>
        public IMFCollection pEvents;
    }
}