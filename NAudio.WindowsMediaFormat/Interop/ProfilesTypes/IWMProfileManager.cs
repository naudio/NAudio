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
    [Guid("d16679f2-6ca0-472d-8d31-2f5d55aee155")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMProfileManager
    {
        void CreateEmptyProfile([In] WMT_VERSION dwVersion,
                                [Out, MarshalAs(UnmanagedType.Interface)] out IWMProfile ppProfile);
        void LoadProfileByID([In] ref Guid guidProfile,
                             [Out, MarshalAs(UnmanagedType.Interface)] out IWMProfile ppProfile);
        void LoadProfileByData([In, MarshalAs(UnmanagedType.LPWStr)] string pwszProfile,
                               [Out, MarshalAs(UnmanagedType.Interface)] out IWMProfile ppProfile);
        void SaveProfile([In, MarshalAs(UnmanagedType.Interface)] IWMProfile pIWMProfile,
                         [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszProfile,
                         [In, Out] ref uint pdwLength);
        void GetSystemProfileCount([Out] out uint pcProfiles);
        void LoadSystemProfile([In] uint dwProfileIndex,
                               [Out, MarshalAs(UnmanagedType.Interface)] out IWMProfile ppProfile);
    }
}
