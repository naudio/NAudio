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
    [Guid("6d7cdc70-9888-11d3-8edc-00c04f6109cf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMStatusCallback
    {

        void OnStatus([In] WMT_STATUS Status,
                      [In] IntPtr hr,
                      [In] WMT_ATTR_DATATYPE dwType,
                      [In] IntPtr pValue,
                      [In] IntPtr pvContext);
    }
}
