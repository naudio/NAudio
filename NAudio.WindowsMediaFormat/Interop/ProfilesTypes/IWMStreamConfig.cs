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
    [Guid("96406BDC-2B2B-11d3-B36B-00C04F6108FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMStreamConfig
    {
        void GetStreamType([Out] out Guid pguidStreamType);
        void GetStreamNumber([Out] out ushort pwStreamNum);
        void SetStreamNumber([In] ushort wStreamNum);
        void GetStreamName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszStreamName,
                           [In, Out] ref ushort pcchStreamName);
        void SetStreamName([In, MarshalAs(UnmanagedType.LPWStr)] string pwszStreamName);
        void GetConnectionName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszInputName,
                               [In, Out] ref ushort pcchInputName);
        void SetConnectionName([In, MarshalAs(UnmanagedType.LPWStr)] string pwszInputName);
        void GetBitrate([Out] out uint pdwBitrate);
        void SetBitrate([In] uint pdwBitrate);
        void GetBufferWindow([Out] out uint pmsBufferWindow);
        void SetBufferWindow([In] uint msBufferWindow);
    };
}
