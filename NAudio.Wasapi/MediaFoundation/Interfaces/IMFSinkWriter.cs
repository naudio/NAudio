using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    [GeneratedComInterface]
    [Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFSinkWriter
    {
        [PreserveSig]
        int AddStream(IntPtr pTargetMediaType, out int pdwStreamIndex);

        [PreserveSig]
        int SetInputMediaType(int dwStreamIndex, IntPtr pInputMediaType, IntPtr pEncodingParameters);

        [PreserveSig]
        int BeginWriting();

        [PreserveSig]
        int WriteSample(int dwStreamIndex, IntPtr pSample);

        [PreserveSig]
        int SendStreamTick(int dwStreamIndex, long llTimestamp);

        [PreserveSig]
        int PlaceMarker(int dwStreamIndex, IntPtr pvContext);

        [PreserveSig]
        int NotifyEndOfSegment(int dwStreamIndex);

        [PreserveSig]
        int Flush(int dwStreamIndex);

        [PreserveSig]
        int DoFinalize();

        [PreserveSig]
        int GetServiceForStream(int dwStreamIndex, in Guid guidService, in Guid riid, out IntPtr ppvObject);

        [PreserveSig]
        int GetStatistics(int dwStreamIndex, IntPtr pStats);
    }
}
