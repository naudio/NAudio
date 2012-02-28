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
using System.Runtime.InteropServices.ComTypes;

namespace NAudio.WindowsMediaFormat
{
    [ComImport]
    [Guid("5DC0674B-F04B-4a4e-9F2A-B1AFDE2C8100")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMReaderAdvanced3 : IWMReaderAdvanced2
    {
        //IWMReaderAdvanced
        new void SetUserProvidedClock([In, MarshalAs(UnmanagedType.Bool)] bool fUserClock);
        new void GetUserProvidedClock([Out, MarshalAs(UnmanagedType.Bool)] out bool pfUserClock);
        new void DeliverTime([In] ulong cnsTime);
        new void SetManualStreamSelection([In, MarshalAs(UnmanagedType.Bool)] bool fSelection);
        new void GetManualStreamSelection([Out, MarshalAs(UnmanagedType.Bool)] out bool pfSelection);
        new void SetStreamsSelected([In] ushort cStreamCount,
         [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ushort[] pwStreamNumbers,
         [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] WMT_STREAM_SELECTION[] pSelections);
        new void GetStreamSelected([In] ushort wStreamNum, [Out] out WMT_STREAM_SELECTION pSelection);
        new void SetReceiveSelectionCallbacks([In, MarshalAs(UnmanagedType.Bool)] bool fGetCallbacks);
        new void GetReceiveSelectionCallbacks([Out, MarshalAs(UnmanagedType.Bool)] out bool pfGetCallbacks);
        new void SetReceiveStreamSamples([In] ushort wStreamNum, [In, MarshalAs(UnmanagedType.Bool)] bool fReceiveStreamSamples);
        new void GetReceiveStreamSamples([In] ushort wStreamNum, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfReceiveStreamSamples);
        new void SetAllocateForOutput([In] uint dwOutputNum, [In, MarshalAs(UnmanagedType.Bool)] bool fAllocate);
        new void GetAllocateForOutput([In] uint dwOutputNum, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfAllocate);
        new void SetAllocateForStream([In] ushort wStreamNum, [In, MarshalAs(UnmanagedType.Bool)] bool fAllocate);
        new void GetAllocateForStream([In] ushort dwSreamNum, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfAllocate);
        new void GetStatistics([In, Out] ref WM_READER_STATISTICS pStatistics);
        new void SetClientInfo([In] ref WM_READER_CLIENTINFO pClientInfo);
        new void GetMaxOutputSampleSize([In] uint dwOutput, [Out] out uint pcbMax);
        new void GetMaxStreamSampleSize([In] ushort wStream, [Out] out uint pcbMax);
        new void NotifyLateDelivery(ulong cnsLateness);
        //IWMReaderAdvanced2
        new void SetPlayMode([In] WMT_PLAY_MODE Mode);
        new void GetPlayMode([Out] out WMT_PLAY_MODE pMode);
        new void GetBufferProgress([Out] out uint pdwPercent, [Out] out ulong pcnsBuffering);
        new void GetDownloadProgress([Out] out uint pdwPercent,
         [Out] out ulong pqwBytesDownloaded,
         [Out] out ulong pcnsDownload);
        new void GetSaveAsProgress([Out] out uint pdwPercent);
        new void SaveFileAs([In, MarshalAs(UnmanagedType.LPWStr)] string pwszFilename);
        new void GetProtocolName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszProtocol,
         [In, Out] ref uint pcchProtocol);
        new void StartAtMarker([In] ushort wMarkerIndex,
         [In] ulong cnsDuration,
         [In] float fRate,
         [In] IntPtr pvContext);
        new void GetOutputSetting([In] uint dwOutputNum,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
         [Out] out WMT_ATTR_DATATYPE pType,
         [Out, MarshalAs(UnmanagedType.LPArray)] byte[] pValue,
         [In, Out] ref ushort pcbLength);
        new void SetOutputSetting([In] uint dwOutputNum,
         [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
         [In] WMT_ATTR_DATATYPE Type,
         [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pValue,
         [In] ushort cbLength);
        new void Preroll([In] ulong cnsStart,
         [In] ulong cnsDuration,
         [In] float fRate);
        new void SetLogClientID([In, MarshalAs(UnmanagedType.Bool)] bool fLogClientID);
        new void GetLogClientID([Out, MarshalAs(UnmanagedType.Bool)] out bool pfLogClientID);
        new void StopBuffering();
        //    new void OpenStream( [In, MarshalAs(UnmanagedType.Interface)] UCOMIStream pStream,
        //    [In, MarshalAs(UnmanagedType.Interface)] IWMReaderCallback pCallback,
        //  [In] IntPtr pvContext );
        // Yuval
        new void OpenStream([In, MarshalAs(UnmanagedType.Interface)] IStream pStream,
    [In, MarshalAs(UnmanagedType.Interface)] IWMReaderCallback pCallback,
    [In] IntPtr pvContext);

        //IWMReaderAdvanced3
        void StopNetStreaming();
        void StartAtPosition([In] ushort wStreamNum,
                             [In] IntPtr pvOffsetStart,
                             [In] IntPtr pvDuration,
                             [In] WMT_OFFSET_FORMAT dwOffsetFormat,
                             [In] float fRate,
                             [In] IntPtr pvContext);
    }
}
