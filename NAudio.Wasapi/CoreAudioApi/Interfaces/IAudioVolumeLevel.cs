using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        GeneratedComInterface]
    internal partial interface IAudioVolumeLevel
    {
        // Methods from IPerChannelDbLevel repeated here since GeneratedComInterface does not support inheritance
        [PreserveSig]
        int GetChannelCount(out uint channels);
        [PreserveSig]
        int GetLevelRange(uint channel, out float minLevelDb, out float maxLevelDb, out float stepping);
        [PreserveSig]
        int GetLevel(uint channel, out float levelDb);
        [PreserveSig]
        int SetLevel(uint channel, float levelDb, ref Guid eventGuidContext);
        [PreserveSig]
        int SetLevelUniform(float levelDb, ref Guid eventGuidContext);
        [PreserveSig]
        int SetLevelAllChannel([MarshalAs(UnmanagedType.LPArray)] float[] levelsDb, uint channels, ref Guid eventGuidContext);
    }
}
