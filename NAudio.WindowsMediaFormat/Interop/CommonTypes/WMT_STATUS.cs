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
    public enum WMT_STATUS
    {

        WMT_ERROR = 0,

        WMT_OPENED = 1,

        WMT_BUFFERING_START = 2,

        WMT_BUFFERING_STOP = 3,

        WMT_EOF = 4,

        WMT_END_OF_FILE = 4,

        WMT_END_OF_SEGMENT = 5,

        WMT_END_OF_STREAMING = 6,

        WMT_LOCATING = 7,

        WMT_CONNECTING = 8,

        WMT_NO_RIGHTS = 9,

        WMT_MISSING_CODEC = 10,

        WMT_STARTED = 11,

        WMT_STOPPED = 12,

        WMT_CLOSED = 13,

        WMT_STRIDING = 14,

        WMT_TIMER = 15,

        WMT_INDEX_PROGRESS = 16,

        WMT_SAVEAS_START = 17,

        WMT_SAVEAS_STOP = 18,

        WMT_NEW_SOURCEFLAGS = 19,

        WMT_NEW_METADATA = 20,

        WMT_BACKUPRESTORE_BEGIN = 21,

        WMT_SOURCE_SWITCH = 22,

        WMT_ACQUIRE_LICENSE = 23,

        WMT_INDIVIDUALIZE = 24,

        WMT_NEEDS_INDIVIDUALIZATION = 25,

        WMT_NO_RIGHTS_EX = 26,

        WMT_BACKUPRESTORE_END = 27,

        WMT_BACKUPRESTORE_CONNECTING = 28,

        WMT_BACKUPRESTORE_DISCONNECTING = 29,

        WMT_ERROR_WITHURL = 30,

        WMT_RESTRICTED_LICENSE = 31,

        WMT_CLIENT_CONNECT = 32,

        WMT_CLIENT_DISCONNECT = 33,

        WMT_NATIVE_OUTPUT_PROPS_CHANGED = 34,

        WMT_RECONNECT_START = 35,

        WMT_RECONNECT_END = 36,

        WMT_CLIENT_CONNECT_EX = 37,

        WMT_CLIENT_DISCONNECT_EX = 38,

        WMT_SET_FEC_SPAN = 39,

        WMT_PREROLL_READY = 40,

        WMT_PREROLL_COMPLETE = 41,

        WMT_CLIENT_PROPERTIES = 42,

        WMT_LICENSEURL_SIGNATURE_STATE = 43
    }
}
