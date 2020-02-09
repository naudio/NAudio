using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IConnector interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("9C2C4058-23F5-41DE-877A-DF3AF236A09E"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IConnector
    {
        int GetType(out ConnectorType type);
        int GetDataFlow(out DataFlow flow);
        int ConnectTo([In] IConnector connectTo);
        int Disconnect();
        int IsConnected(out bool connected);
        int GetConnectedTo(out IConnector conTo);
        int GetConnectorIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int GetDeviceIdConnectedTo([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }
}
