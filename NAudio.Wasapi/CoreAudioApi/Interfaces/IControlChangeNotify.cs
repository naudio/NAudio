using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.Wasapi.CoreAudioApi.Interfaces
{
    [Guid("9c2c4058-23f5-41de-877a-df3af236a09e"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    interface IControlChangeNotify
    {
        [PreserveSig]
        int OnNotify(
            [In] uint controlId,
            [In] IntPtr context);
    }
}
