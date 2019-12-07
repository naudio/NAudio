using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IPart interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IPart
    {
        // Stub, Not implemented
    }
}
