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
    }
}
