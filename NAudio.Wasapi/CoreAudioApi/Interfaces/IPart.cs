using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IPart interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IPart
    {
        [PreserveSig]
        int GetName(
            [MarshalAs(UnmanagedType.LPWStr)] out string name);

        [PreserveSig]
        int GetLocalId(
            out uint id);

        [PreserveSig]
        int GetGlobalId(
            [MarshalAs(UnmanagedType.LPWStr)] out string id);

        [PreserveSig]
        int GetPartType(
            out PartTypeEnum partType);

        [PreserveSig]
        int GetSubType(
            out Guid subType);

        [PreserveSig]
        int GetControlInterfaceCount(
            out uint count);

        [PreserveSig]
        int GetControlInterface(
            uint index,
            out IntPtr controlInterface);

        [PreserveSig]
        int EnumPartsIncoming(
            out IntPtr parts);

        [PreserveSig]
        int EnumPartsOutgoing(
            out IntPtr parts);

        [PreserveSig]
        int GetTopologyObject(
            out IntPtr topologyObject);

        [PreserveSig]
        int Activate(
            ClsCtx dwClsContext,
            ref Guid refiid,
            out IntPtr interfacePointer);

        [PreserveSig]
        int RegisterControlChangeCallback(
            ref Guid refiid,
            IntPtr notify);

        [PreserveSig]
        int UnregisterControlChangeCallback(
            IntPtr notify);
    }
}
