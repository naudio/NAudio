using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IDeviceTopology interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IDeviceTopology
    {
        [PreserveSig]
        int GetConnectorCount(out uint count);
        [PreserveSig]
        int GetConnector(uint index, out IntPtr connector);
        [PreserveSig]
        int GetSubunitCount(out uint count);
        [PreserveSig]
        int GetSubunit(uint index, out IntPtr subunit);
        [PreserveSig]
        int GetPartById(uint id, out IntPtr part);
        [PreserveSig]
        int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig]
        int GetSignalPath(IntPtr from, IntPtr to, [MarshalAs(UnmanagedType.Bool)] bool rejectMixedPaths, out IntPtr parts);
    }
}
