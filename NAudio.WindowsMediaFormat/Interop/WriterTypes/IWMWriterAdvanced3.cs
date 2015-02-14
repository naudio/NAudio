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
    [Guid("2cd6492d-7c37-4e76-9d3b-59261183a22e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMWriterAdvanced3 : IWMWriterAdvanced2
    {
        //IWMWriterAdvanced
        new void GetSinkCount([Out] out uint pcSinks);
        new void GetSink([In] uint dwSinkNum,
         [Out, MarshalAs(UnmanagedType.Interface)] out IWMWriterSink ppSink);
        new void AddSink([In, MarshalAs(UnmanagedType.Interface)] IWMWriterSink pSink);
        new void RemoveSink([In, MarshalAs(UnmanagedType.Interface)] IWMWriterSink pSink);
        new void WriteStreamSample([In] ushort wStreamNum,
         [In] ulong cnsSampleTime,
         [In] uint msSampleSendTime,
         [In] ulong cnsSampleDuration,
         [In] uint dwFlags,
         [In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pSample);
        new void SetLiveSource([MarshalAs(UnmanagedType.Bool)]bool fIsLiveSource);
        new void IsRealTime([Out, MarshalAs(UnmanagedType.Bool)] out bool pfRealTime);
        new void GetWriterTime([Out] out ulong pcnsCurrentTime);
        new void GetStatistics([In] ushort wStreamNum,
         [Out] out WM_WRITER_STATISTICS pStats);
        new void SetSyncTolerance([In] uint msWindow);
        new void GetSyncTolerance([Out] out uint pmsWindow);
        //IWMWriterAdvanced2
        new void GetInputSetting([In] uint dwInputNum,
          [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
          [Out] out WMT_ATTR_DATATYPE pType,
          [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
          [In, Out] ref ushort pcbLength);
        new void SetInputSetting([In] uint dwInputNum,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
         [In] WMT_ATTR_DATATYPE Type,
         [In, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] byte[] pValue,
         [In] ushort cbLength);
        //IWMWriterAdvanced3
        void GetStatisticsEx([In] ushort wStreamNum,
                             [Out] out WM_WRITER_STATISTICS_EX pStats);
        void SetNonBlocking();
    }
}
