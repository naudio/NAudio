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
    [Guid("96406BD6-2B2B-11d3-B36B-00C04F6108FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMReader
    {
        void Open([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
                  [In, MarshalAs(UnmanagedType.Interface)] IWMReaderCallback pCallback,
                  [In] IntPtr pvContext);
        void Close();
        void GetOutputCount([Out] out uint pcOutputs);
        void GetOutputProps([In] uint dwOutputNum,
                            [Out, MarshalAs(UnmanagedType.Interface)] out IWMOutputMediaProps ppOutput);
        void SetOutputProps([In] uint dwOutputNum,
                            [In, MarshalAs(UnmanagedType.Interface)] IWMOutputMediaProps pOutput);
        void GetOutputFormatCount([In] uint dwOutputNumber, [Out] out uint pcFormats);
        void GetOutputFormat([In] uint dwOutputNumber,
                             [In] uint dwFormatNumber,
                             [Out, MarshalAs(UnmanagedType.Interface)] out IWMOutputMediaProps ppProps);
        void Start([In] ulong cnsStart,
                   [In] ulong cnsDuration,
                   [In] float fRate,
                   [In] IntPtr pvContext);
        void Stop();
        void Pause();
        void Resume();
    }
}
