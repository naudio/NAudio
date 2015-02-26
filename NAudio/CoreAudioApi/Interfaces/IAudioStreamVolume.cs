using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("93014887-242D-4068-8A15-CF5E93B90FE3"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioStreamVolume
    {
        [PreserveSig]
        int GetChannelCount(
            [Out] out uint dwCount);

        [PreserveSig]
        int SetChannelVolume(
            [In] uint dwIndex,
            [In] float fLevel);

        [PreserveSig]
        int GetChannelVolume(
            [In] uint dwIndex,
            [Out] out float fLevel);

        [PreserveSig]
        int SetAllVoumes(
            [In] uint dwCount,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4, SizeParamIndex=0)] float[] fVolumes);

        [PreserveSig]
        int GetAllVolumes(
          [In]   uint dwCount,
          [MarshalAs(UnmanagedType.LPArray)]  float []pfVolumes);
    }
}
