using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("93014887-242D-4068-8A15-CF5E93B90FE3"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     GeneratedComInterface]
    internal partial interface IAudioStreamVolume
    {
        [PreserveSig]
        int GetChannelCount(out uint dwCount);

        [PreserveSig]
        int SetChannelVolume(uint dwIndex, float fLevel);

        [PreserveSig]
        int GetChannelVolume(uint dwIndex, out float fLevel);

        [PreserveSig]
        int SetAllVoumes(
            uint dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4, SizeParamIndex=0)] float[] fVolumes);

        [PreserveSig]
        int GetAllVolumes(
            uint dwCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] float[] pfVolumes);
    }
}
