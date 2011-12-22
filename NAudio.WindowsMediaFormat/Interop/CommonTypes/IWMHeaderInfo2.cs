#region Original License
//Widows Media Format Interfaces
//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER
//  REMAINS UNCHANGED.
//
//  Email:  yetiicb@hotmail.com
//
//  Copyright (C) 2002-2004 Idael Cardoso.
//
#endregion

#region Code Modifications Note
// Yuval Naveh, 2010
// Note - The code below has been changed and fixed from its original form.
// Changes include - Formatting, Layout, Coding standards and removal of compilation warnings

// Mark Heath, 2010 - modified for inclusion in NAudio
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WindowsMediaFormat
{
    [ComImport]
    [Guid("15CF9781-454E-482e-B393-85FAE487A810")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMHeaderInfo2 : IWMHeaderInfo
    {
        //IWMHeaderInfo
        new void GetAttributeCount([In] ushort wStreamNum, [Out] out ushort pcAttributes);
        new void GetAttributeByIndex([In] ushort wIndex,
         [In, Out] ref ushort pwStreamNum,
         [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
         [In, Out] ref ushort pcchNameLen,
         [Out] out WMT_ATTR_DATATYPE pType,
         IntPtr pValue,
         [In, Out] ref ushort pcbLength);
        new void GetAttributeByName([In, Out] ref ushort pwStreamNum,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
         [Out] out WMT_ATTR_DATATYPE pType,
         IntPtr pValue,
         [In, Out] ref ushort pcbLength);
        new void SetAttribute([In] ushort wStreamNum,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
         [In] WMT_ATTR_DATATYPE Type,
         IntPtr pValue,
         [In] ushort cbLength);
        new void GetMarkerCount([Out] out ushort pcMarkers);
        new void GetMarker([In] ushort wIndex,
         [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszMarkerName,
         [In, Out] ref ushort pcchMarkerNameLen,
         [Out] out ulong pcnsMarkerTime);
        new void AddMarker([In, MarshalAs(UnmanagedType.LPWStr)] string pwszMarkerName,
         [In] ulong cnsMarkerTime);
        new void RemoveMarker([In] ushort wIndex);
        new void GetScriptCount([Out] out ushort pcScripts);
        new void GetScript([In] ushort wIndex,
         [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszType,
         [In, Out] ref ushort pcchTypeLen,
         [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszCommand,
         [In, Out] ref ushort pcchCommandLen,
         [Out] out ulong pcnsScriptTime);
        new void AddScript([In, MarshalAs(UnmanagedType.LPWStr)] string pwszType,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pwszCommand,
         [In] ulong cnsScriptTime);
        new void RemoveScript([In] ushort wIndex);
        //IWMHeaderInfo2
        void GetCodecInfoCount([Out] out uint pcCodecInfos);

        void GetCodecInfo([In] uint wIndex,
                          [In, Out] ref ushort pcchName,
                          [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
                          [In, Out] ref ushort pcchDescription,
                          [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszDescription,
                          [Out] out WMT_CODEC_INFO_TYPE pCodecType,
                          [In, Out] ref ushort pcbCodecInfo,
                          [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbCodecInfo);
    }
}
