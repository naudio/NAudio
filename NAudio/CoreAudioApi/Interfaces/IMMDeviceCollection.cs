using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        int GetCount(out int numDevices);
        int Item(int deviceNumber, out IMMDevice device);
    }
}
