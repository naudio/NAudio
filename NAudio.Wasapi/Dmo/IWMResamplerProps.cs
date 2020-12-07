using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// Windows Media Resampler Props
    /// wmcodecdsp.h
    /// </summary>
    [Guid("E7E9984F-F09F-4da4-903F-6E2E0EFE56B5"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IWMResamplerProps
    {
        /// <summary>
        /// Range is 1 to 60
        /// </summary>
        int SetHalfFilterLength(int outputQuality);

        /// <summary>
        ///  Specifies the channel matrix.
        /// </summary>
        int SetUserChannelMtx([In] float[] channelConversionMatrix);
    }
}
