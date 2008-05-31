using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum DmoInputDataBufferFlags
    {
        None,
        DMO_INPUT_DATA_BUFFERF_SYNCPOINT = 0x00000001,
        DMO_INPUT_DATA_BUFFERF_TIME = 0x00000002,
        DMO_INPUT_DATA_BUFFERF_TIMELENGTH = 0x00000004
    }
}
