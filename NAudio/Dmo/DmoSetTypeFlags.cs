using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum DmoSetTypeFlags
    {
        None,
        DMO_SET_TYPEF_TEST_ONLY = 0x00000001,
        DMO_SET_TYPEF_CLEAR = 0x00000002
    }
}
