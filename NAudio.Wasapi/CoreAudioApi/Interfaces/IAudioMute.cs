using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IAudioMute
    {
        [PreserveSig]
        int GetMute(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool mute);

        [PreserveSig]
        int SetMute(
            [In, MarshalAs(UnmanagedType.Bool)] bool mute,
            [In] ref Guid eventContext);
    }
}
