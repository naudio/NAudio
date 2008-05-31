using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum DmoOutputDataBufferFlags
    {
        None,
        DMO_OUTPUT_DATA_BUFFERF_SYNCPOINT = 0x00000001,
        DMO_OUTPUT_DATA_BUFFERF_TIME = 0x00000002,
        DMO_OUTPUT_DATA_BUFFERF_TIMELENGTH = 0x00000004,
        DMO_OUTPUT_DATA_BUFFERF_INCOMPLETE = 0x01000000
    }
}
