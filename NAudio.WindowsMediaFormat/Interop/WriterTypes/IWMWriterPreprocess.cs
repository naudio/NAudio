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
    [Guid("fc54a285-38c4-45b5-aa23-85b9f7cb424b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMWriterPreprocess
    {
        void GetMaxPreprocessingPasses([In] uint dwInputNum,
                                       [In] uint dwFlags,
                                       [Out] out uint pdwMaxNumPasses);
        void SetNumPreprocessingPasses([In] uint dwInputNum,
                                       [In] uint dwFlags,
                                       [In] uint dwNumPasses);
        void BeginPreprocessingPass([In] uint dwInputNum, [In] uint dwFlags);
        void PreprocessSample([In] uint dwInputNum,
                              [In] ulong cnsSampleTime,
                              [In] uint dwFlags,
                              [In, MarshalAs(UnmanagedType.Interface)] INSSBuffer pSample);
        void EndPreprocessingPass([In] uint dwInputNum, [In] uint dwFlags);
    }
}
