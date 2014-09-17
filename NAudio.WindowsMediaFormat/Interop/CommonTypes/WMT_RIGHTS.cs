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
    [Flags]
    public enum WMT_RIGHTS : uint
    {
        /// <summary>
        /// This right is not defined in the WMF SDK, I added it to
        /// play files with no DRM
        /// </summary>
        WMT_RIGHT_NO_DRM = 0x00000000,

        WMT_RIGHT_PLAYBACK = 0x00000001,

        WMT_RIGHT_COPY_TO_NON_SDMI_DEVICE = 0x00000002,

        WMT_RIGHT_COPY_TO_CD = 0x00000008,

        WMT_RIGHT_COPY_TO_SDMI_DEVICE = 0x00000010,

        WMT_RIGHT_ONE_TIME = 0x00000020,

        WMT_RIGHT_SAVE_STREAM_PROTECTED = 0x00000040,

        WMT_RIGHT_SDMI_TRIGGER = 0x00010000,

        WMT_RIGHT_SDMI_NOMORECOPIES = 0x00020000
    }
}
