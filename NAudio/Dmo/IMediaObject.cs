using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// defined in mediaobj.h
    /// </summary>
    [Guid("d8ad0f58-5494-4102-97c5-ec798e59bcf4"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMediaObject
    {
        int GetStreamCount(out int inputStreams, out int outputStreams);
        
        int GetInputStreamInfo(int inputStreamIndex, out InputStreamInfoFlags flags);
        
        int GetOutputStreamInfo(int outputStreamIndex, out OutputStreamInfoFlags flags);
        
        int GetInputType(int inputStreamIndex, int typeIndex, out DmoMediaType mediaType);
        
        int GetOutputType(int outputStreamIndex, int typeIndex, out DmoMediaType mediatType);
        
        int SetInputType(int inputStreamIndex, [In] ref DmoMediaType mediaType, DmoSetTypeFlags flags);
        
        int SetOutputType(int outputStreamIndex, [In] ref DmoMediaType mediaType, DmoSetTypeFlags flags);
        
        int GetInputCurrentType(int inputStreamIndex, out DmoMediaType mediaType);
        
        int GetOutputCurrentType(int outputStreamIndex, out DmoMediaType mediaType);
        
        int GetInputSizeInfo(int inputStreamIndex, out int size, out int maxLookahed, out int alignment);
        
        int GetOutputSizeInfo(int outputStreamIndex, out int size, out int alignment);
        
        int GetInputMaxLatency(int inputStreamIndex, out long referenceTimeMaxLatency);
        
        int SetInputMaxLatency(int inputStreamIndex, long referenceTimeMaxLatency);
        
        int Flush();
        
        int Discontinuity(int inputStreamIndex);
        
        int AllocateStreamingResources();
        
        int FreeStreamingResources();
        
        int GetInputStatus(int inputStreamIndex, out DmoInputStatusFlags flags);
        
        int ProcessInput(int inputStreamIndex, [In] IMediaBuffer mediaBuffer, DmoInputDataBufferFlags flags,
            long referenceTimeTimestamp, long referenceTimeDuration);
        
        int ProcessOutput(DmoProcessOutputFlags flags, int outputBufferCount, DmoOutputDataBuffer[] outputBuffers,
            out int statusReserved);
        
        int Lock(bool acquireLock);
    }
}
