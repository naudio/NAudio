using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("85401FD4-6DE4-4b9d-9869-2D6753A82F3C"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IAudioAutoGainControl
    {
        [PreserveSig]
        int GetEnabled(
            [MarshalAs(UnmanagedType.Bool)] out bool enabled);

        [PreserveSig]
        int SetEnabled(
            [MarshalAs(UnmanagedType.Bool)] bool enabled);
    }
}
