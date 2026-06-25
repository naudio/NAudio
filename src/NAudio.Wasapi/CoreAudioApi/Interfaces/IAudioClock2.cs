using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces;

/// <summary>
/// Defined in AudioClient.h
/// </summary>
[Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    GeneratedComInterface]
internal partial interface IAudioClock
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
/// <remarks>
/// IAudioClock2 inherits from IUnknown (NOT from <see cref="IAudioClock"/>), so its vtable holds a
/// single method, GetDevicePosition, right after the IUnknown slots. Do not redeclare IAudioClock's
/// methods here: they belong to a different interface, and prepending them would push GetDevicePosition
/// to the wrong vtable slot, dispatching the call to the wrong native function (access violation).
/// </remarks>
[Guid("6f49ff73-6727-49AC-A008-D98CF5E70048"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    GeneratedComInterface]
internal partial interface IAudioClock2
{
    [PreserveSig]
    int GetDevicePosition(out ulong devicePosition, out ulong qpcPosition);
}
