using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IPart interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("AE2DE0E4-5BCA-4F2D-AA46-5D13F8FDB3A9"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IPart
    {
        int GetName(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string name);

        int GetLocalId(
            [Out] out uint id);

        int GetGlobalId(
            [Out, MarshalAs(UnmanagedType.LPWStr)] out string id);

        int GetPartType(
            [Out] out PartTypeEnum partType);

        int GetSubType(
            out Guid subType);

        int GetControlInterfaceCount(
            [Out] out uint count);

        int GetControlInterface(
            [In] uint index,
            [Out, MarshalAs(UnmanagedType.IUnknown)] out IControlInterface controlInterface);

        [PreserveSig]
        int EnumPartsIncoming(
            [Out] out IPartsList parts);

        [PreserveSig]
        int EnumPartsOutgoing(
            [Out] out IPartsList parts);

        int GetTopologyObject(
            [Out] out object topologyObject);

        [PreserveSig]
        int Activate(
            [In] ClsCtx dwClsContext,
            [In] ref Guid refiid,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

        int RegisterControlChangeCallback(
            [In] ref Guid refiid,
            [In] IControlChangeNotify notify);

        int UnregisterControlChangeCallback(
            [In] IControlChangeNotify notify);
    }
}
