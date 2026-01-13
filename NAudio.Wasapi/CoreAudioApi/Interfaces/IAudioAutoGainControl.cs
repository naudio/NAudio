using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("85401FD4-6DE4-4b9d-9869-2D6753A82F3C"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IAudioAutoGainControl
    {
        [PreserveSig]
        int GetEnabled(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool enabled);

        [PreserveSig]
        int SetEnabled(
            [In, MarshalAs(UnmanagedType.Bool)] bool enabled);
    }
}
