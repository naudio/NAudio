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
    [Guid("9397F121-7705-4dc9-B049-98B698188414")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMSyncReader
    {
        void Open([In, MarshalAs(UnmanagedType.LPWStr)] string pwszFilename);
        void Close();
        void SetRange([In] ulong cnsStartTime, [In] long cnsDuration);
        void SetRangeByFrame([In] ushort wStreamNum, [In] ulong qwFrameNumber, [In]long cFramesToRead);
        void GetNextSample([In] ushort wStreamNum,
                           [Out] out INSSBuffer ppSample,
                           [Out] out ulong pcnsSampleTime,
                           [Out] out ulong pcnsDuration,
                           [Out] out uint pdwFlags,
                           [Out] out uint pdwOutputNum,
                           [Out] out ushort pwStreamNum);
        void SetStreamsSelected([In] ushort cStreamCount,
                                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ushort[] pwStreamNumbers,
                                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] WMT_STREAM_SELECTION[] pSelections);
        void GetStreamSelected([In]ushort wStreamNum,
                               [Out] out WMT_STREAM_SELECTION pSelection);
        void SetReadStreamSamples([In] ushort wStreamNum,
                                  [In, MarshalAs(UnmanagedType.Bool)] bool fCompressed);
        void GetReadStreamSamples([In] ushort wStreamNum,
                                  [Out, MarshalAs(UnmanagedType.Bool)] out bool pfCompressed);
        void GetOutputSetting([In] uint dwOutputNum,
                              [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
                              [Out] out WMT_ATTR_DATATYPE pType,
            /*[out, size_is( *pcbLength )]*/ IntPtr pValue,
                              [In, Out] ref uint pcbLength);
        void SetOutputSetting([In] uint dwOutputNum,
                              [In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
                              [In] WMT_ATTR_DATATYPE Type,
                              [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pValue,
                              [In] uint cbLength);
        void GetOutputCount([Out] out uint pcOutputs);
        void GetOutputProps([In] uint dwOutputNum, [Out, MarshalAs(UnmanagedType.Interface)] out IWMOutputMediaProps ppOutput);
        void SetOutputProps([In] uint dwOutputNum, [In, MarshalAs(UnmanagedType.Interface)] IWMOutputMediaProps pOutput);
        void GetOutputFormatCount([In] uint dwOutputNum, [Out] out uint pcFormats);
        void GetOutputFormat([In] uint dwOutputNum,
                             [In] uint dwFormatNum,
                             [Out, MarshalAs(UnmanagedType.Interface)] out IWMOutputMediaProps ppProps);
        void GetOutputNumberForStream([In] ushort wStreamNum, [Out] out uint pdwOutputNum);
        void GetStreamNumberForOutput([In] uint dwOutputNum, [Out] out ushort pwStreamNum);
        void GetMaxOutputSampleSize([In] uint dwOutput, [Out] out uint pcbMax);
        void GetMaxStreamSampleSize([In] ushort wStream, [Out] out uint pcbMax);
        //    void OpenStream( [In, MarshalAs(UnmanagedType.Interface)] UCOMIStream pStream );
        // Yuval
        void OpenStream([In, MarshalAs(UnmanagedType.Interface)] IStream pStream);

    }
}
