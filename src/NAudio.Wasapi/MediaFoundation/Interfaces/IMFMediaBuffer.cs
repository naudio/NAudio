using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Represents a block of memory that contains media data.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMFMediaBuffer (mfobjects.h).
    /// https://learn.microsoft.com/windows/win32/api/mfobjects/nn-mfobjects-imfmediabuffer
    /// </remarks>
    [GeneratedComInterface]
    [Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFMediaBuffer
    {
        [PreserveSig]
        int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);

        [PreserveSig]
        int Unlock();

        [PreserveSig]
        int GetCurrentLength(out int pcbCurrentLength);

        [PreserveSig]
        int SetCurrentLength(int cbCurrentLength);

        [PreserveSig]
        int GetMaxLength(out int pcbMaxLength);
    }
}
