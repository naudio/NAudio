using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    /// <summary>
    /// http://msdn.microsoft.com/en-us/library/aa929922.aspx
    /// DMO_MEDIA_TYPE 
    /// </summary>
    struct DmoMediaType
    {
        Guid majortype;
        Guid subtype;
        bool bFixedSizeSamples;
        bool bTemporalCompression;
        int lSampleSize;
        Guid formattype;
        IntPtr pUnk;
        int cbFormat;
        IntPtr pbFormat; // not used
        //[size_is(cbFormat)] BYTE* pbFormat;
    }
}
