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
    // wmsdkidl.h
    [ComImport]
    [Guid("96406BD5-2B2B-11d3-B36B-00C04F6108FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMInputMediaProps : IWMMediaProps
    {
        //IWMMediaProps
        new void GetType([Out] out Guid pguidType);
        new void GetMediaType( /*[out] WM_MEDIA_TYPE* */ IntPtr pType,
          [In, Out] ref int pcbType);
        new void SetMediaType([In] ref WM_MEDIA_TYPE pType);
        //IWMInputMediaProps  
        void GetConnectionName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
                               [In, Out] ref ushort pcchName);

        void GetGroupName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
                          [In, Out] ref ushort pcchName);
    }
}
