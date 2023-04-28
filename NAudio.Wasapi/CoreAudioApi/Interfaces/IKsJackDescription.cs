using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
