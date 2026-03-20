using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    [GeneratedComInterface]
    [Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFSourceReader
    {
        [PreserveSig]
        int GetStreamSelection(int dwStreamIndex, out int pSelected);

        [PreserveSig]
        int SetStreamSelection(int dwStreamIndex, int fSelected);

        [PreserveSig]
        int GetNativeMediaType(int dwStreamIndex, int dwMediaTypeIndex, out IntPtr ppMediaType);

        [PreserveSig]
        int GetCurrentMediaType(int dwStreamIndex, out IntPtr ppMediaType);

        [PreserveSig]
        int SetCurrentMediaType(int dwStreamIndex, IntPtr pdwReserved, IntPtr pMediaType);

        [PreserveSig]
        int SetCurrentPosition(in Guid guidTimeFormat, IntPtr varPosition);

        [PreserveSig]
        int ReadSample(int dwStreamIndex, int dwControlFlags, out int pdwActualStreamIndex, out int pdwStreamFlags, out long pllTimestamp, out IntPtr ppSample);

        [PreserveSig]
        int Flush(int dwStreamIndex);

        [PreserveSig]
        int GetServiceForStream(int dwStreamIndex, in Guid guidService, in Guid riid, out IntPtr ppvObject);

        [PreserveSig]
        int GetPresentationAttribute(int dwStreamIndex, in Guid guidAttribute, IntPtr pvarAttribute);
    }
}
