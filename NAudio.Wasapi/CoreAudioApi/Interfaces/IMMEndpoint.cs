using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// defined in MMDeviceAPI.h
    /// </summary>
    [Guid("1BE09788-6894-4089-8586-9A2A6C265AC5"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMEndpoint
    {
        int GetDataFlow(out DataFlow dataFlow);
    }
}
