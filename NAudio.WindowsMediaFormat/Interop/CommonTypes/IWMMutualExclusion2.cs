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
    [Guid("0302B57D-89D1-4ba2-85C9-166F2C53EB91")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMMutualExclusion2 : IWMMutualExclusion
    {
        //IWMStreamList
        new void GetStreams([Out, MarshalAs(UnmanagedType.LPArray)] ushort[] pwStreamNumArray,
         [In, Out] ref ushort pcStreams);
        new void AddStream([In] ushort wStreamNum);
        new void RemoveStream([In] ushort wStreamNum);
        //IWMMutualExclusion
        new void GetType([Out] out Guid pguidType);
        new void SetType([In] ref Guid guidType);
        //IWMMutualExclusion2
        void GetName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
                     [In, Out] ref ushort pcchName);

        void SetName([In, MarshalAs(UnmanagedType.LPWStr)] string pwszName);

        void GetRecordCount([Out] out ushort pwRecordCount);

        void AddRecord();

        void RemoveRecord([In] ushort wRecordNumber);

        void GetRecordName([In] ushort wRecordNumber,
                           [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszRecordName,
                           [In, Out] ref ushort pcchRecordName);

        void SetRecordName([In] ushort wRecordNumber,
                           [In, MarshalAs(UnmanagedType.LPWStr)] string pwszRecordName);

        void GetStreamsForRecord([In] ushort wRecordNumber,
                                 [Out, MarshalAs(UnmanagedType.LPArray)] ushort[] pwStreamNumArray,
                                 [In, Out] ref ushort pcStreams);

        void AddStreamForRecord([In] ushort wRecordNumber, [In] ushort wStreamNumber);

        void RemoveStreamForRecord([In] ushort wRecordNumber, [In] ushort wStreamNumber);
    }
}
