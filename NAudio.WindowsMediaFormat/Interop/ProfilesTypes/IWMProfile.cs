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
    [Guid("96406BDB-2B2B-11d3-B36B-00C04F6108FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWMProfile
    {
        void GetVersion([Out] out WMT_VERSION pdwVersion);
        void GetName([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszName,
                     [In, Out] ref uint pcchName);
        void SetName([In, MarshalAs(UnmanagedType.LPWStr)] string pwszName);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszDescription,
                            [In, Out] ref uint pcchDescription);
        void SetDescription([In, MarshalAs(UnmanagedType.LPWStr)] string pwszDescription);
        void GetStreamCount([Out] out uint pcStreams);
        void GetStream([In] uint dwStreamIndex, [Out, MarshalAs(UnmanagedType.Interface)] out IWMStreamConfig ppConfig);
        void GetStreamByNumber([In] ushort wStreamNum, [Out, MarshalAs(UnmanagedType.Interface)] out IWMStreamConfig ppConfig);
        void RemoveStream([In, MarshalAs(UnmanagedType.Interface)] IWMStreamConfig pConfig);
        void RemoveStreamByNumber([In] ushort wStreamNum);
        void AddStream([In, MarshalAs(UnmanagedType.Interface)] IWMStreamConfig pConfig);
        void ReconfigStream([In, MarshalAs(UnmanagedType.Interface)] IWMStreamConfig pConfig);
        void CreateNewStream([In] ref Guid guidStreamType,
                             [Out, MarshalAs(UnmanagedType.Interface)] out IWMStreamConfig ppConfig);
        void GetMutualExclusionCount([Out] out uint pcME);
        void GetMutualExclusion([In] uint dwMEIndex,
                                [Out, MarshalAs(UnmanagedType.Interface)] out IWMMutualExclusion ppME);
        void RemoveMutualExclusion([In, MarshalAs(UnmanagedType.Interface)] IWMMutualExclusion pME);
        void AddMutualExclusion([In, MarshalAs(UnmanagedType.Interface)] IWMMutualExclusion pME);
        void CreateNewMutualExclusion([Out, MarshalAs(UnmanagedType.Interface)] out IWMMutualExclusion ppME);
    }
}
