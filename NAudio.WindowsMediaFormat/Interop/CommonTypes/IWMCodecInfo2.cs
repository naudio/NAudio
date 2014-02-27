using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace NAudio.WindowsMediaFormat
{
    [ComImport, SuppressUnmanagedCodeSecurity,
    Guid("AA65E273-B686-4056-91EC-DD768D4DF710"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMCodecInfo2 : IWMCodecInfo
    {
        #region IWMCodecInfo Methods

        new void GetCodecInfoCount(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [Out] out int pcCodecs
            );

        new void GetCodecFormatCount(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [Out] out int pcFormat
            );

        new void GetCodecFormat(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In] int dwFormatIndex,
            out IWMStreamConfig ppIStreamConfig
            );

        #endregion

        void GetCodecName(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [Out] StringBuilder wszName,
            ref int pcchName
            );

        void GetCodecFormatDesc(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In] int dwFormatIndex,
            out IWMStreamConfig ppIStreamConfig,
            [Out] StringBuilder wszDesc,
            ref int pcchDesc
            );
    }

}
