using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// defined in mediaobj.h
    /// </summary>
    [ComImport,
#if !WINDOWS_UWP
    System.Security.SuppressUnmanagedCodeSecurity,
#endif
    Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaObject
    {
        [PreserveSig]
        int GetStreamCount(out int inputStreams, out int outputStreams);

        [PreserveSig]
        int GetInputStreamInfo(int inputStreamIndex, out InputStreamInfoFlags flags);

        [PreserveSig]
        int GetOutputStreamInfo(int outputStreamIndex, out OutputStreamInfoFlags flags);

        [PreserveSig]
        int GetInputType(int inputStreamIndex, int typeIndex, out DmoMediaType mediaType);

        [PreserveSig]
        int GetOutputType(int outputStreamIndex, int typeIndex, out DmoMediaType mediaType);

        [PreserveSig]
        int SetInputType(int inputStreamIndex, [In] ref DmoMediaType mediaType, DmoSetTypeFlags flags);

        [PreserveSig]
        int SetOutputType(int outputStreamIndex, [In] ref DmoMediaType mediaType, DmoSetTypeFlags flags);

        [PreserveSig]
        int GetInputCurrentType(int inputStreamIndex, out DmoMediaType mediaType);

        [PreserveSig]
        int GetOutputCurrentType(int outputStreamIndex, out DmoMediaType mediaType);

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
        int GetInputStatus(int inputStreamIndex, out DmoInputStatusFlags flags);

        [PreserveSig]
        int ProcessInput(int inputStreamIndex, [In] IMediaBuffer mediaBuffer, DmoInputDataBufferFlags flags,
            long referenceTimeTimestamp, long referenceTimeDuration);

        [PreserveSig]
        int ProcessOutput(DmoProcessOutputFlags flags, 
            int outputBufferCount,
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] DmoOutputDataBuffer[] outputBuffers,
            out int statusReserved);

        [PreserveSig]
        int Lock(bool acquireLock);
    }
}
