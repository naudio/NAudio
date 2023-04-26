using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("7FB7B48F-531D-44A2-BCB3-5AD5A134B3DC"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IAudioVolumeLevel : IPerChannelDbLevel
    {

    }
}
