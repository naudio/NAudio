using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Creates an instance of either the sink writer or the source reader.
    /// </summary>
    [ComImport,Guid("E7FE2E12-661C-40DA-92F9-4F002AB67627"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMFReadWriteClassFactory
    {
        /// <summary>
        /// Creates an instance of the sink writer or source reader, given a URL.
        /// </summary>
        void CreateInstanceFromURL([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppvObject);

        /// <summary>
        /// Creates an instance of the sink writer or source reader, given an IUnknown pointer. 
        /// </summary>
        void CreateInstanceFromObject([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [In, MarshalAs(UnmanagedType.IUnknown)] object punkObject, [In, MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    /// <summary>
    /// CLSID_MFReadWriteClassFactory
    /// </summary>
    [ComImport, Guid("48e2ed0f-98c2-4a37-bed5-166312ddd83f")]
    public class MFReadWriteClassFactory
    {
    }
}
