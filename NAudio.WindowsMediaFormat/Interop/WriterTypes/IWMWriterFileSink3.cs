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
    [Guid("3FEA4FEB-2945-47A7-A1DD-C53A8FC4C45C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMWriterFileSink3 : IWMWriterFileSink2
    {
        //IWMWriterSink
        new void OnHeader([In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pHeader);
        new void IsRealTime([Out, MarshalAs(UnmanagedType.Bool)] out bool pfRealTime);
        new void AllocateDataUnit([In] uint cbDataUnit,
         [Out, MarshalAs(UnmanagedType.Interface)] out INSSBuffer ppDataUnit);
        new void OnDataUnit([In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pDataUnit);
        new void OnEndWriting();
        //IWMWriterFileSink
        new void Open([In, MarshalAs(UnmanagedType.LPWStr)] string pwszFilename);
        //IWMWriterFileSink2
        new void Start([In] ulong cnsStartTime);
        new void Stop([In] ulong cnsStopTime);
        new void IsStopped([Out, MarshalAs(UnmanagedType.Bool)] out bool pfStopped);
        new void GetFileDuration([Out] out ulong pcnsDuration);
        new void GetFileSize([Out] out ulong pcbFile);
        new void Close();
        new void IsClosed([Out, MarshalAs(UnmanagedType.Bool)] out bool pfClosed);
        //IWMWriterFileSink3
        void SetAutoIndexing([In, MarshalAs(UnmanagedType.Bool)] bool fDoAutoIndexing);
        void GetAutoIndexing([Out, MarshalAs(UnmanagedType.Bool)] out bool pfAutoIndexing);
        void SetControlStream([In] ushort wStreamNumber,
                              [In, MarshalAs(UnmanagedType.Bool)] bool fShouldControlStartAndStop);
        void GetMode([Out] out uint pdwFileSinkMode);
        void OnDataUnitEx([In] ref WMT_FILESINK_DATA_UNIT pFileSinkDataUnit);
        void SetUnbufferedIO([In, MarshalAs(UnmanagedType.Bool)] bool fUnbufferedIO,
                             [In, MarshalAs(UnmanagedType.Bool)] bool fRestrictMemUsage);
        void GetUnbufferedIO([Out, MarshalAs(UnmanagedType.Bool)] out bool pfUnbufferedIO);
        void CompleteOperations();
    }
}
