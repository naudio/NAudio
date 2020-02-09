using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IDeviceTopology interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("2A07407E-6497-4A18-9787-32F79BD0D98F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IDeviceTopology
    {
        int GetConnectorCount(out uint count);
        int GetConnector(uint index, out IConnector connector);
        int GetSubunitCount(out uint count);
        int GetSubunit(uint index, out ISubunit subunit);
        int GetPartById(uint id, out IPart part);
        int GetDeviceId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int GetSignalPath(IPart from, IPart to, bool rejectMixedPaths, out IPartsList parts);
    }
}
