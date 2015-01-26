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
    [Guid("B6B8FD5A-32E2-49d4-A910-C26CC85465ED")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INSSBuffer4 : INSSBuffer3
    {
        //INSSBuffer
        new void GetLength([Out] out uint pdwLength);
        new void SetLength([In] uint dwLength);
        new void GetMaxLength([Out] out uint pdwLength);
        new void GetBuffer([Out] out IntPtr ppdwBuffer);
        new void GetBufferAndLength([Out] out IntPtr ppdwBuffer, [Out] out uint pdwLength);
        //INSSBuffer2
        new void GetSampleProperties([In] uint cbProperties, [Out] out byte pbProperties);
        new void SetSampleProperties([In] uint cbProperties, [In] ref byte pbProperties);
        //INSSBuffer3
        new void SetProperty([In] Guid guidBufferProperty,
          [In] IntPtr pvBufferProperty,
          [In] uint dwBufferPropertySize);
        new void GetProperty([In] Guid guidBufferProperty,
            /*out]*/ IntPtr pvBufferProperty,
          [In, Out] ref uint pdwBufferPropertySize);
        //INSSBuffer4
        void GetPropertyCount([Out] out uint pcBufferProperties);

        void GetPropertyByIndex([In] uint dwBufferPropertyIndex,
                                [Out] out Guid pguidBufferProperty,
            /*[out]*/   IntPtr pvBufferProperty,
                                [In, Out] ref uint pdwBufferPropertySize);
    }
}
