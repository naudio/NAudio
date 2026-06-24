using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("DF45AEEA-B74A-4B6B-AFAD-2366B6AA012E"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IAudioMute
    {
        [PreserveSig]
        int GetMute(
            [MarshalAs(UnmanagedType.Bool)] out bool mute);

        [PreserveSig]
        int SetMute(
            [MarshalAs(UnmanagedType.Bool)] bool mute,
            ref Guid eventContext);
    }
}
