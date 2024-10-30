using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
       InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
       ComImport]
    internal interface IPerChannelDbLevel
    {
        int GetChannelCount(out uint channels);
        int GetLevelRange(uint channel, out float minLevelDb, out float maxLevelDb, out float stepping);
        int GetLevel(uint channel, out float levelDb);
        int SetLevel(uint channel, float levelDb, ref Guid eventGuidContext);
        int SetLevelUniform(float levelDb, ref Guid eventGuidContext);
        int SetLevelAllChannel(float[] levelsDb, uint channels, ref Guid eventGuidContext);
    }
}
