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
    [Guid("96406BE3-2B2B-11d3-B36B-00C04F6108FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMWriterAdvanced
    {
        void GetSinkCount([Out] out uint pcSinks);

        void GetSink([In] uint dwSinkNum,
                     [Out, MarshalAs(UnmanagedType.Interface)] out IWMWriterSink ppSink);
        void AddSink([In, MarshalAs(UnmanagedType.Interface)] IWMWriterSink pSink);
        void RemoveSink([In, MarshalAs(UnmanagedType.Interface)] IWMWriterSink pSink);
        void WriteStreamSample([In] ushort wStreamNum,
                               [In] ulong cnsSampleTime,
                               [In] uint msSampleSendTime,
                               [In] ulong cnsSampleDuration,
                               [In] uint dwFlags,
                               [In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pSample);
        void SetLiveSource([MarshalAs(UnmanagedType.Bool)]bool fIsLiveSource);
        void IsRealTime([Out, MarshalAs(UnmanagedType.Bool)] out bool pfRealTime);
        void GetWriterTime([Out] out ulong pcnsCurrentTime);
        void GetStatistics([In] ushort wStreamNum,
                           [Out] out WM_WRITER_STATISTICS pStats);
        void SetSyncTolerance([In] uint msWindow);
        void GetSyncTolerance([Out] out uint pmsWindow);
    }
}
