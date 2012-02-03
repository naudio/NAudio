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
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WM_READER_CLIENTINFO
    {
        public uint cbSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string wszLang;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string wszBrowserUserAgent;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string wszBrowserWebPage;
        ulong qwReserved;
        public IntPtr pReserved;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string wszHostExe;
        public ulong qwHostVersion;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string wszPlayerUserAgent;
    }
}
