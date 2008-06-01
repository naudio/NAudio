using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    struct DmoOutputDataBuffer
    {
        IMediaBuffer pBuffer;
        DmoOutputDataBufferFlags dwStatus;
        long rtTimestamp;
        long referenceTimeDuration;

        public IMediaBuffer MediaBuffer
        {
            get { return pBuffer; }
            internal set { pBuffer = value; }
        }

        public DmoOutputDataBufferFlags StatusFlags
        {
            get { return dwStatus; }
            internal set { dwStatus = value; }
        }

        public long Timestamp
        {
            get { return rtTimestamp; }
            internal set { rtTimestamp = value; }
        }

        public long Duration
        {
            get { return referenceTimeDuration; }
            internal set { referenceTimeDuration = value; }
        }
    }
}
