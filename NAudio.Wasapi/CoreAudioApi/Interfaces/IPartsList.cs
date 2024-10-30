using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Windows CoreAudio IPartsList interface
    /// Defined in devicetopology.h
    /// </summary>
    [Guid("6DAA848C-5EB0-45CC-AEA5-998A2CDA1FFB"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    ComImport]
    internal interface IPartsList
    {
        int GetCount(out uint count);
        int GetPart(uint index, out IPart part);
    }
}
