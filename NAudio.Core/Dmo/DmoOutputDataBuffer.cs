using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Output Data Buffer
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct DmoOutputDataBuffer : IDisposable
    {
        [MarshalAs(UnmanagedType.Interface)]
        IMediaBuffer pBuffer;
        DmoOutputDataBufferFlags dwStatus;
        long rtTimestamp;
        long referenceTimeDuration;

        /// <summary>
        /// Creates a new DMO Output Data Buffer structure
        /// </summary>
        /// <param name="maxBufferSize">Maximum buffer size</param>
        public DmoOutputDataBuffer(int maxBufferSize)
        {
            pBuffer = new MediaBuffer(maxBufferSize);
            dwStatus = DmoOutputDataBufferFlags.None;
            rtTimestamp = 0;
            referenceTimeDuration = 0;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (pBuffer != null)
            {
                ((MediaBuffer)pBuffer).Dispose();
                pBuffer = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Media Buffer
        /// </summary>
        public IMediaBuffer MediaBuffer
        {
            get { return pBuffer; }
            internal set { pBuffer = value; }
        }

        /// <summary>
        /// Length of data in buffer
        /// </summary>
        public int Length
        {
            get { return ((MediaBuffer)pBuffer).Length; }
        }

        /// <summary>
        /// Status Flags
        /// </summary>
        public DmoOutputDataBufferFlags StatusFlags
        {
            get { return dwStatus; }
            internal set { dwStatus = value; }
        }

        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp
        {
            get { return rtTimestamp; }
            internal set { rtTimestamp = value; }
        }

        /// <summary>
        /// Duration
        /// </summary>
        public long Duration
        {
            get { return referenceTimeDuration; }
            internal set { referenceTimeDuration = value; }
        }

        /// <summary>
        /// Retrives the data in this buffer
        /// </summary>
        /// <param name="data">Buffer to receive data</param>
        /// <param name="offset">Offset into buffer</param>
        public void RetrieveData(byte[] data, int offset)
        {
            ((MediaBuffer)pBuffer).RetrieveData(data, offset);
        }

        /// <summary>
        /// Is more data available
        /// If true, ProcessOuput should be called again
        /// </summary>
        public bool MoreDataAvailable
        {
            get
            {
                return (StatusFlags & DmoOutputDataBufferFlags.Incomplete) == DmoOutputDataBufferFlags.Incomplete;
            }
        }
    }
}
