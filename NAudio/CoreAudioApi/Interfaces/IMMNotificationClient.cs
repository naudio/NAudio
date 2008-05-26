using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMNotificationClient
    {
        int OnDeviceStateChanged(string deviceId, int newState);
        
        int OnDeviceAdded(string pwstrDeviceId);
        
        int OnDeviceRemoved(string deviceId);
        
        int OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId);
        
        int OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key);

    }
}
