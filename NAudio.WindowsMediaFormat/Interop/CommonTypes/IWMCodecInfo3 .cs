using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace NAudio.WindowsMediaFormat
{
    [ComImport, SuppressUnmanagedCodeSecurity,
    Guid("7E51F487-4D93-4F98-8AB4-27D0565ADC51"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMCodecInfo3 : IWMCodecInfo2
    {
        #region IWMCodecInfo Methods

        new void GetCodecInfoCount(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            out int pcCodecs
            );

        new void GetCodecFormatCount(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            out int pcFormat
            );

        new void GetCodecFormat(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In] int dwFormatIndex,
            out IWMStreamConfig ppIStreamConfig
            );

        #endregion

        #region IWMCodecInfo2 Methods

        new void GetCodecName(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [Out] StringBuilder wszName,
            ref int pcchName
            );

        new void GetCodecFormatDesc(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In] int dwFormatIndex,
            out IWMStreamConfig ppIStreamConfig,
            [Out] StringBuilder wszDesc,
            ref int pcchDesc
            );

        #endregion

        void GetCodecFormatProp(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In] int dwFormatIndex,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
            out WMT_ATTR_DATATYPE pType,
            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
            ref int pdwSize
            );

        void GetCodecProp(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
            out WMT_ATTR_DATATYPE pType,
            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
            ref int pdwSize
            );

        void SetCodecEnumerationSetting(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [In] WMT_ATTR_DATATYPE Type,
            [In, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
            [In] int dwSize
            );

        void GetCodecEnumerationSetting(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidType,
            [In] int dwCodecIndex,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
            out WMT_ATTR_DATATYPE pType,
            [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
            ref int pdwSize
            );
    }
}
