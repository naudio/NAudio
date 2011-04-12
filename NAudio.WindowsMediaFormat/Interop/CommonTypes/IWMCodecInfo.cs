using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WindowsMediaFormat
{
    [ComVisible(true), ComImport,
Guid("A970F41E-34DE-4a98-B3BA-E4B3CA7528F0"),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMCodecInfo
    {
        void GetCodecInfoCount(
            [In] ref Guid guidType,
            [Out] out uint pcCodecs);

        void GetCodecFormatCount(
            [In] ref Guid guidType,
            [In] uint dwCodecIndex,
            [Out] out uint pcFormat);

        void GetCodecFormat(
            [In] ref Guid guidType,
            [In] uint dwCodecIndex,
            [In] uint dwFormatIndex,
            [Out, MarshalAs(UnmanagedType.Interface)] out IWMStreamConfig
    ppIStreamConfig);
    };

}
