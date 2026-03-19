using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    GeneratedComInterface]
    internal partial interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int sessionCount);

        [PreserveSig]
        int GetSession(int sessionCount, out IntPtr session);
    }
}
