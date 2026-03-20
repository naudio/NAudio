using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interfaces
{
    /// <summary>
    /// Sets properties on the audio resampler DSP.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IWMResamplerProps (wmcodecdsp.h).
    /// https://learn.microsoft.com/windows/win32/api/wmcodecdsp/nn-wmcodecdsp-iwmresamplerprops
    /// </remarks>
    [GeneratedComInterface]
    [Guid("E7E9984F-F09F-4da4-903F-6E2E0EFE56B5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IWMResamplerProps
    {
        [PreserveSig]
        int SetHalfFilterLength(int outputQuality);

        [PreserveSig]
        int SetUserChannelMtx(IntPtr channelConversionMatrix);
    }
}
