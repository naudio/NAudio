using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Render Client
    /// </summary>
    public class AudioRenderClient : IDisposable
    {
        IAudioRenderClient audioRenderClientInterface;

        internal AudioRenderClient(IAudioRenderClient audioRenderClientInterface)
        {
            this.audioRenderClientInterface = audioRenderClientInterface;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesRequested">Number of frames requested</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(int numFramesRequested)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.GetBuffer(numFramesRequested, out var bufferPointer));
            return bufferPointer;
        }

        /// <summary>
        /// Release buffer
        /// </summary>
        /// <param name="numFramesWritten">Number of frames written</param>
        /// <param name="bufferFlags">Buffer flags</param>
        public void ReleaseBuffer(int numFramesWritten,AudioClientBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.ReleaseBuffer(numFramesWritten, bufferFlags));
        }

        /// <summary>
        /// Release the COM object
        /// </summary>
        public void Dispose()
        {
            if (audioRenderClientInterface != null)
            {
                // althugh GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioRenderClientInterface);
                audioRenderClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
