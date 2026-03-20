using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Contains information about an output buffer for a Media Foundation transform.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MftOutputDataBuffer
    {
        /// <summary>
        /// Output stream identifier.
        /// </summary>
        public int StreamId;
        /// <summary>
        /// Pointer to the IMFSample interface.
        /// </summary>
        public IMFSample Sample;
        /// <summary>
        /// Before calling ProcessOutput, set this member to zero.
        /// </summary>
        public MftOutputDataBufferFlags Status;
        /// <summary>
        /// Before calling ProcessOutput, set this member to null.
        /// </summary>
        public IMFCollection Events;
    }
}
