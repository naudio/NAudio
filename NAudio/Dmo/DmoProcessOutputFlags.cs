using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum DmoProcessOutputFlags
    {
        None,
        DMO_PROCESS_OUTPUT_DISCARD_WHEN_NO_BUFFER = 0x00000001
    }
}
