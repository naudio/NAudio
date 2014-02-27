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
    [Guid("B70F1E42-6255-4df0-A6B9-02B212D9E2BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMIndexer2 : IWMIndexer
    {
        //IWMIndexer
        new void StartIndexing([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
         [In, MarshalAs(UnmanagedType.Interface)] IWMStatusCallback pCallback,
         [In] IntPtr pvContext);
        new void Cancel();
        //IWMIndexer2
        void Configure([In] ushort wStreamNum,
                          [In] WMT_INDEXER_TYPE nIndexerType,
                          [In] IntPtr pvInterval,
                          [In] IntPtr pvIndexType);
    }
}
