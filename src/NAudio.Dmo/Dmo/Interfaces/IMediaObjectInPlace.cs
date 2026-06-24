using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interfaces
{
    /// <summary>
    /// Provides methods for processing data in place on a DirectX Media Object (DMO).
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMediaObjectInPlace (mediaobj.h).
    /// https://learn.microsoft.com/windows/win32/api/mediaobj/nn-mediaobj-imediaobjectinplace
    /// </remarks>
    [GeneratedComInterface]
    [Guid("651B9AD0-0FC7-4AA9-9538-D89931010741")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMediaObjectInPlace
    {
        [PreserveSig]
        int Process(int size, IntPtr data, long refTimeStart, int dwFlags);

        [PreserveSig]
        int Clone(out IntPtr mediaObjectInPlace);

        [PreserveSig]
        int GetLatency(out long latencyTime);
    }
}
