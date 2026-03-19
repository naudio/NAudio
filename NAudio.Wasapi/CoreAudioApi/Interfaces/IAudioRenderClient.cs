using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.CoreAudioApi.Interfaces
{
    [GeneratedComInterface]
    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioRenderClient
    {
        [PreserveSig]
        int GetBuffer(int numFramesRequested, out IntPtr dataBufferPointer);

        [PreserveSig]
        int ReleaseBuffer(int numFramesWritten, AudioClientBufferFlags bufferFlags);
    }
}
