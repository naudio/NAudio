using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Dmo.Interfaces
{
    /// <summary>
    /// Provides methods for manipulating a DirectX Media Object (DMO).
    /// </summary>
    /// <remarks>
    /// Windows SDK: IMediaObject (mediaobj.h).
    /// https://learn.microsoft.com/windows/win32/api/mediaobj/nn-mediaobj-imediaobject
    /// </remarks>
    [GeneratedComInterface]
    [Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMediaObject
    {
        [PreserveSig]
        int GetStreamCount(out int inputStreams, out int outputStreams);

        [PreserveSig]
        int GetInputStreamInfo(int inputStreamIndex, out int flags);

        [PreserveSig]
        int GetOutputStreamInfo(int outputStreamIndex, out int flags);

        [PreserveSig]
        int GetInputType(int inputStreamIndex, int typeIndex, IntPtr mediaType);

        [PreserveSig]
        int GetOutputType(int outputStreamIndex, int typeIndex, IntPtr mediaType);

        [PreserveSig]
        int SetInputType(int inputStreamIndex, IntPtr mediaType, int flags);

        [PreserveSig]
        int SetOutputType(int outputStreamIndex, IntPtr mediaType, int flags);

        [PreserveSig]
        int GetInputCurrentType(int inputStreamIndex, IntPtr mediaType);

        [PreserveSig]
        int GetOutputCurrentType(int outputStreamIndex, IntPtr mediaType);

        [PreserveSig]
        int GetInputSizeInfo(int inputStreamIndex, out int size, out int maxLookahead, out int alignment);

        [PreserveSig]
        int GetOutputSizeInfo(int outputStreamIndex, out int size, out int alignment);

        [PreserveSig]
        int GetInputMaxLatency(int inputStreamIndex, out long referenceTimeMaxLatency);

        [PreserveSig]
        int SetInputMaxLatency(int inputStreamIndex, long referenceTimeMaxLatency);

        [PreserveSig]
        int Flush();

        [PreserveSig]
        int Discontinuity(int inputStreamIndex);

        [PreserveSig]
        int AllocateStreamingResources();

        [PreserveSig]
        int FreeStreamingResources();

        [PreserveSig]
        int GetInputStatus(int inputStreamIndex, out int flags);

        [PreserveSig]
        int ProcessInput(int inputStreamIndex, IntPtr mediaBuffer, int flags,
            long referenceTimeTimestamp, long referenceTimeDuration);

        [PreserveSig]
        int ProcessOutput(int flags,
            int outputBufferCount,
            IntPtr outputBuffers,
            out int statusReserved);

        [PreserveSig]
        int Lock(int acquireLock);
    }
}
