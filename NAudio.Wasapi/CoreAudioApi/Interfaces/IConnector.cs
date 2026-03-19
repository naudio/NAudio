using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IConnector interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IConnector
    {
        [PreserveSig]
        int GetType(out ConnectorType type);
        [PreserveSig]
        int GetDataFlow(out DataFlow flow);
        [PreserveSig]
        int ConnectTo(IntPtr connectTo);
        [PreserveSig]
        int Disconnect();
        [PreserveSig]
        int IsConnected([MarshalAs(UnmanagedType.Bool)] out bool connected);
        [PreserveSig]
        int GetConnectedTo(out IntPtr conTo);
        [PreserveSig]
        int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig]
        int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }
}
