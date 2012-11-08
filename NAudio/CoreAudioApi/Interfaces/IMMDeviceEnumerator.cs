using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(DataFlow dataFlow, DeviceState stateMask,
            out IMMDeviceCollection devices);
        
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice endpoint);
        
        int GetDevice(string id, out IMMDevice deviceName);
        
        int RegisterEndpointNotificationCallback(IMMNotificationClient client);
        
        int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }
}
