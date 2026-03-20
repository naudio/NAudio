using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    /// <summary>
    /// Creates an instance of the source reader or sink writer.
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMFReadWriteClassFactory (mfreadwrite.h).
    /// https://learn.microsoft.com/windows/win32/api/mfreadwrite/nn-mfreadwrite-imfreadwriteclassfactory
    /// </remarks>
    [GeneratedComInterface]
    [Guid("E7FE2E12-661C-40DA-92F9-4F002AB67627")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFReadWriteClassFactory
    {
        [PreserveSig]
        int CreateInstanceFromURL(in Guid clsid, [MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
            IntPtr pAttributes, in Guid riid, out IntPtr ppvObject);

        [PreserveSig]
        int CreateInstanceFromObject(in Guid clsid, IntPtr punkObject,
            IntPtr pAttributes, in Guid riid, out IntPtr ppvObject);
    }

    /// <summary>
    /// CLSID_MFReadWriteClassFactory
    /// </summary>
    [ComImport, Guid("48e2ed0f-98c2-4a37-bed5-166312ddd83f")]
    internal class MFReadWriteClassFactory
    {
    }
}
