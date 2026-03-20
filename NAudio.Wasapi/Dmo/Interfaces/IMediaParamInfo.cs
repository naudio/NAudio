using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interfaces
{
    /// <summary>
    /// Retrieves information about the parameters that a DMO supports.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMediaParamInfo (medparam.h).
    /// https://learn.microsoft.com/windows/win32/api/medparam/nn-medparam-imediaparaminfo
    /// </remarks>
    [GeneratedComInterface]
    [Guid("6d6cbb60-a223-44aa-842f-a2f06750be6d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMediaParamInfo
    {
        [PreserveSig]
        int GetParamCount(out int paramCount);

        [PreserveSig]
        int GetParamInfo(int paramIndex, IntPtr paramInfo);

        [PreserveSig]
        int GetParamText(int paramIndex, out IntPtr paramText);

        [PreserveSig]
        int GetNumTimeFormats(out int numTimeFormats);

        [PreserveSig]
        int GetSupportedTimeFormat(int formatIndex, out Guid guidTimeFormat);

        [PreserveSig]
        int GetCurrentTimeFormat(out Guid guidTimeFormat, out int mediaTimeData);
    }
}
