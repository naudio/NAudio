using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.WindowsMediaFormat
{
    public class Functions
    {
        [DllImport("WMVCore.dll", EntryPoint = "WMCreateEditor", SetLastError = true,
             CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern uint WMCreateEditor(
            [Out, MarshalAs(UnmanagedType.Interface)]	out IWMMetadataEditor ppMetadataEditor);

        [DllImport("WMVCore.dll", EntryPoint = "WMCreateSyncReader", SetLastError = true,
             CharSet = CharSet.Unicode, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        public static extern uint WMCreateSyncReader(
            [In] IntPtr pUnkCert, // must be set to null
            [In] WMT_RIGHTS dwRights,
            [Out] out IWMSyncReader ppSyncReader);

    }
}
