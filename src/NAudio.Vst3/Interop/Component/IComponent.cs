using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Bus description (<c>Vst::BusInfo</c>) returned by <c>IComponent::getBusInfo</c>.
/// </summary>
/// <remarks>
/// The C++ struct uses <c>String128</c> = <c>char16[128]</c> for the name, so the entire struct
/// is 4 + 4 + 4 + 256 + 4 + 4 = 276 bytes with natural alignment.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct BusInfo
{
    public MediaType MediaType;
    public BusDirection Direction;
    public int ChannelCount;
    /// <summary>UTF-16 name buffer (<c>String128</c>).</summary>
    public fixed char Name[128];
    public BusType BusType;
    public uint Flags;
}

/// <summary>
/// Routing info (<c>Vst::RoutingInfo</c>) — describes how an event input channel maps to an audio
/// output bus for plug-ins with multiple busses.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RoutingInfo
{
    public MediaType MediaType;
    public int BusIndex;
    /// <summary>Channel index, or <c>-1</c> for all channels.</summary>
    public int Channel;
}

/// <summary>
/// VST 3 component base (<c>Vst::IComponent</c>) — common interface every audio module class must
/// implement. Defined in <c>pluginterfaces/vst/ivstcomponent.h</c>.
/// </summary>
/// <remarks>
/// Inherits from <see cref="IPluginBase"/>; vtable order is Initialize, Terminate, then the
/// IComponent methods listed below.
/// </remarks>
[GeneratedComInterface]
[Guid("E831FF31-F2D5-4301-928E-BBEE25697802")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IComponent
{
    // ---- IPluginBase methods (must redeclare in vtable order) ----

    [PreserveSig]
    int Initialize(IntPtr context);

    [PreserveSig]
    int Terminate();

    // ---- IComponent methods ----

    /// <summary>Fills <paramref name="classId"/> with the controller's class ID (16 bytes).</summary>
    [PreserveSig]
    int GetControllerClassId(IntPtr classId);

    [PreserveSig]
    int SetIoMode(IoMode mode);

    [PreserveSig]
    int GetBusCount(MediaType type, BusDirection dir);

    [PreserveSig]
    int GetBusInfo(MediaType type, BusDirection dir, int index, out BusInfo bus);

    [PreserveSig]
    int GetRoutingInfo(ref RoutingInfo inInfo, out RoutingInfo outInfo);

    [PreserveSig]
    int ActivateBus(MediaType type, BusDirection dir, int index, byte state);

    [PreserveSig]
    int SetActive(byte state);

    [PreserveSig]
    int SetState(IntPtr state);

    [PreserveSig]
    int GetState(IntPtr state);
}
