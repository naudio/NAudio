using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// defined in MMDeviceAPI.h
    /// </summary>
    [GeneratedComInterface]
    [Guid("1BE09788-6894-4089-8586-9A2A6C265AC5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMMEndpoint
    {
        [PreserveSig]
        int GetDataFlow(out DataFlow dataFlow);
    }
}
