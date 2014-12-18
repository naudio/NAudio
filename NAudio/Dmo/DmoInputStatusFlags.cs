using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum DmoInputStatusFlags
    {
        None,
        DMO_INPUT_STATUSF_ACCEPT_DATA	= 0x1
    }
}
