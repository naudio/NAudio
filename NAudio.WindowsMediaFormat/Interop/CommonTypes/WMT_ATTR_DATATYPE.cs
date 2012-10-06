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

namespace NAudio.WindowsMediaFormat
{
    public enum WMT_ATTR_DATATYPE
    {
        WMT_TYPE_DWORD = 0,

        WMT_TYPE_STRING = 1,

        WMT_TYPE_BINARY = 2,

        WMT_TYPE_BOOL = 3,

        WMT_TYPE_QWORD = 4,

        WMT_TYPE_WORD = 5,

        WMT_TYPE_GUID = 6,
    }
}
