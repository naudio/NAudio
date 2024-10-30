using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("4509F757-2D46-4637-8E62-CE7DB944F57B"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IKsJackDescription
    {
        int GetJackCount([Out] out uint jacks);
        int GetJackDescription([In] uint jack, [Out, MarshalAs(UnmanagedType.LPWStr)] out string description);
    };
}
