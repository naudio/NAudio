using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IAudioVolumeLevel : IPerChannelDbLevel
    {

    }
}
