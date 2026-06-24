using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IPartsList interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("6DAA848C-5EB0-45CC-AEA5-998A2CDA1FFB"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    GeneratedComInterface]
    internal partial interface IPartsList
    {
        [PreserveSig]
        int GetCount(out uint count);
        [PreserveSig]
        int GetPart(uint index, out IntPtr part);
    }
}
