using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("4509F757-2D46-4637-8E62-CE7DB944F57B"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IKsJackDescription
    {
        [PreserveSig]
        int GetJackCount(out uint jacks);
        [PreserveSig]
        int GetJackDescription(uint jack, [MarshalAs(UnmanagedType.LPWStr)] out string description);
    }
}
