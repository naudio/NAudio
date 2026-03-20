using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation.Interfaces
{
    [GeneratedComInterface]
    [Guid("bf94c121-5b05-4e6f-8000-ba598961414d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMFTransform
    {
        [PreserveSig]
        int GetStreamLimits(out int pdwInputMinimum, out int pdwInputMaximum, out int pdwOutputMinimum, out int pdwOutputMaximum);

        [PreserveSig]
        int GetStreamCount(out int pcInputStreams, out int pcOutputStreams);

        [PreserveSig]
        int GetStreamIds(int dwInputIdArraySize, IntPtr pdwInputIDs, int dwOutputIdArraySize, IntPtr pdwOutputIDs);

        [PreserveSig]
        int GetInputStreamInfo(int dwInputStreamId, out MftInputStreamInfo pStreamInfo);

        [PreserveSig]
        int GetOutputStreamInfo(int dwOutputStreamId, out MftOutputStreamInfo pStreamInfo);

        [PreserveSig]
        int GetAttributes(out IntPtr pAttributes);

        [PreserveSig]
        int GetInputStreamAttributes(int dwInputStreamId, out IntPtr pAttributes);

        [PreserveSig]
        int GetOutputStreamAttributes(int dwOutputStreamId, out IntPtr pAttributes);

        [PreserveSig]
        int DeleteInputStream(int dwStreamId);

        [PreserveSig]
        int AddInputStreams(int cStreams, IntPtr adwStreamIDs);

        [PreserveSig]
        int GetInputAvailableType(int dwInputStreamId, int dwTypeIndex, out IntPtr ppType);

        [PreserveSig]
        int GetOutputAvailableType(int dwOutputStreamId, int dwTypeIndex, out IntPtr ppType);

        [PreserveSig]
        int SetInputType(int dwInputStreamId, IntPtr pType, int dwFlags);

        [PreserveSig]
        int SetOutputType(int dwOutputStreamId, IntPtr pType, int dwFlags);

        [PreserveSig]
        int GetInputCurrentType(int dwInputStreamId, out IntPtr ppType);

        [PreserveSig]
        int GetOutputCurrentType(int dwOutputStreamId, out IntPtr ppType);

        [PreserveSig]
        int GetInputStatus(int dwInputStreamId, out int pdwFlags);

        [PreserveSig]
        int GetOutputStatus(out int pdwFlags);

        [PreserveSig]
        int SetOutputBounds(long hnsLowerBound, long hnsUpperBound);

        [PreserveSig]
        int ProcessEvent(int dwInputStreamId, IntPtr pEvent);

        [PreserveSig]
        int ProcessMessage(int eMessage, IntPtr ulParam);

        [PreserveSig]
        int ProcessInput(int dwInputStreamId, IntPtr pSample, int dwFlags);

        [PreserveSig]
        int ProcessOutput(int dwFlags, int cOutputBufferCount, IntPtr pOutputSamples, out int pdwStatus);
    }
}
