using System;
using System.Collections.Generic;
using System.Text;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    public class AudioRenderClient
    {
        IAudioRenderClient audioRenderClientInterface;

        internal AudioRenderClient(IAudioRenderClient audioRenderClientInterface)
        {
            this.audioRenderClientInterface = audioRenderClientInterface;
        }

        public IntPtr GetBuffer(int numFramesRequested)
        {
            IntPtr bufferPointer;
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.GetBuffer(numFramesRequested, out bufferPointer));
            return bufferPointer;
        }

        public void ReleaseBuffer(int numFramesWritten,AudioClientBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.ReleaseBuffer(numFramesWritten, bufferFlags));
        }
    }
}
