using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// defined in mediaobj.h
    /// </summary>
    [ComImport,
#if !NETFX_CORE
     System.Security.SuppressUnmanagedCodeSecurity,
#endif
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("651B9AD0-0FC7-4AA9-9538-D89931010741")]
    internal interface IMediaObjectInPlace
    {
        [PreserveSig]
        int Process(
            [In] int size,
            [In] IntPtr data,
            [In] long refTimeStart,
            [In] DmoInPlaceProcessFlags dwFlags);

        [PreserveSig]
        int Clone([MarshalAs(UnmanagedType.Interface)] out IMediaObjectInPlace mediaObjectInPlace);

        [PreserveSig]
        int GetLatency(out long latencyTime);
    }
}