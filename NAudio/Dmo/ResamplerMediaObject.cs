using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// From wmcodecsdp.h
    /// Implements:
    /// - IMediaObject 
    /// - IMFTransform (Media foundation - we will leave this for now as there is loads of MF stuff)
    /// - IPropertyStore 
    /// - IWMResamplerProps 
    /// Can resample PCM or IEEE
    /// </summary>
    [ComImport, Guid("f447b69e-1884-4a7e-8055-346f74d6edb3")]
    class ResamplerMediaObject
    {
    }
}
