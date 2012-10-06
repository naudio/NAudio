using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// defined in Medparam.h
    /// </summary>
    [ComImport,
    System.Security.SuppressUnmanagedCodeSecurity,
    Guid("6d6cbb60-a223-44aa-842f-a2f06750be6d"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaParamInfo
    {
        [PreserveSig]
        int GetParamCount(out int paramCount);
        
        [PreserveSig] // MP_PARAMINFO
        int GetParamInfo(int paramIndex, ref MediaParamInfo paramInfo);            
        
        [PreserveSig]
        int GetParamText(int paramIndex, out IntPtr paramText); 
        
        [PreserveSig]
        int GetNumTimeFormats(out int numTimeFormats); 

        [PreserveSig]
        int GetSupportedTimeFormat(int formatIndex, out Guid guidTimeFormat); 
        
        [PreserveSig] // MP_TIMEDATA is a DWORD
        int GetCurrentTimeFormat(out Guid guidTimeFormat, out int mediaTimeData);
    }
}
