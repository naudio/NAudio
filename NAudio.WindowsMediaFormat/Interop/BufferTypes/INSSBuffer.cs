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
    // wmsbuffer.h
    [ComImport]
    [Guid("E1CD3524-03D7-11d2-9EED-006097D2D7CF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INSSBuffer
    {
        void GetLength([Out] out uint pdwLength);

        void SetLength([In] uint dwLength);

        void GetMaxLength([Out] out uint pdwLength);

        void GetBuffer([Out] out IntPtr ppdwBuffer);

        void GetBufferAndLength([Out] out IntPtr ppdwBuffer, [Out] out uint pdwLength);
    }
}
