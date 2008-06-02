using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    /// <summary>
    /// DMO Output Data Buffer
    /// </summary>
    public struct DmoOutputDataBuffer
    {
        IMediaBuffer pBuffer;
        DmoOutputDataBufferFlags dwStatus;
        long rtTimestamp;
        long referenceTimeDuration;

        /// <summary>
        /// Media Buffer
        /// </summary>
        public IMediaBuffer MediaBuffer
        {
            get { return pBuffer; }
            internal set { pBuffer = value; }
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
    }
}
