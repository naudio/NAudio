using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClock
    {
        [PreserveSig]
        int GetFrequency(out ulong frequency);

        [PreserveSig]
        int GetPosition(out ulong devicePosition, out ulong qpcPosition);

        [PreserveSig]
        int GetCharacteristics(out uint characteristics);
    }

    /// <summary>
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("6f49ff73-6727-49AC-A008-D98CF5E70048"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClock2 : IAudioClock
    {
        [PreserveSig]
        int GetDevicePosition(out ulong devicePosition, out ulong qpcPosition);
    }
}
