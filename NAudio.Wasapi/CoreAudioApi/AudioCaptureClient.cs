using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture Client
    /// </summary>
    public class AudioCaptureClient : IDisposable
    {
        IAudioCaptureClient audioCaptureClientInterface;

        internal AudioCaptureClient(IntPtr nativePointer)
        {
            try
            {
                audioCaptureClientInterface = ComActivation.WrapUnique<IAudioCaptureClient>(nativePointer);
            }
            finally
            {
                Marshal.Release(nativePointer);
            }
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioClientBufferFlags bufferFlags,
            out long devicePosition,
            out long qpcPosition)
        {
            CoreAudioException.ThrowIfFailed(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out devicePosition, out qpcPosition));
            return bufferPointer;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesToRead">Number of frames to read</param>
        /// <param name="bufferFlags">Buffer flags</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioClientBufferFlags bufferFlags)
        {
            CoreAudioException.ThrowIfFailed(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out _, out _));
            return bufferPointer;
        }

        /// <summary>
        /// Gets a read-only Span over the WASAPI capture buffer. The returned lease must be
        /// disposed (which calls ReleaseBuffer) before the next call to GetBuffer.
        /// Read audio data directly from <see cref="CaptureBufferLease.Buffer"/> to avoid copies.
        /// </summary>
        /// <param name="bytesPerFrame">Bytes per frame (WaveFormat.BlockAlign)</param>
        /// <returns>A lease that provides a read-only Span and releases the buffer on dispose</returns>
        public CaptureBufferLease GetBufferLease(int bytesPerFrame)
        {
            CoreAudioException.ThrowIfFailed(audioCaptureClientInterface.GetBuffer(
                out var bufferPointer, out var numFramesToRead, out var bufferFlags,
                out var devicePosition, out var qpcPosition));
            return new CaptureBufferLease(this, bufferPointer, numFramesToRead, bytesPerFrame, bufferFlags, devicePosition, qpcPosition);
        }

        /// <summary>
        /// Gets the size of the next packet
        /// </summary>
        public int GetNextPacketSize()
        {
            CoreAudioException.ThrowIfFailed(audioCaptureClientInterface.GetNextPacketSize(out var numFramesInNextPacket));
            return numFramesInNextPacket;
        }

        /// <summary>
        /// Release buffer
        /// </summary>
        /// <param name="numFramesRead">Number of frames read</param>
        public void ReleaseBuffer(int numFramesRead)
        {
            CoreAudioException.ThrowIfFailed(audioCaptureClientInterface.ReleaseBuffer(numFramesRead));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioCaptureClientInterface != null)
            {
                if ((object)audioCaptureClientInterface is ComObject co)
                {
                    co.FinalRelease();
                }
                audioCaptureClientInterface = null;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Provides zero-copy read access to a WASAPI capture buffer.
    /// The buffer pointer is only valid until Dispose/Release is called.
    /// Must be used with <c>using</c> to ensure ReleaseBuffer is called.
    /// </summary>
    public ref struct CaptureBufferLease
    {
        private AudioCaptureClient owner;
        private readonly int frameCount;

        /// <summary>
        /// A read-only span over the captured audio data.
        /// </summary>
        public unsafe ReadOnlySpan<byte> Buffer { get; }

        /// <summary>
        /// The number of frames in this buffer.
        /// </summary>
        public int FrameCount => frameCount;

        /// <summary>
        /// Buffer flags indicating the state of the captured data (e.g. silent).
        /// </summary>
        public AudioClientBufferFlags Flags { get; }

        /// <summary>
        /// The device position (in frames) at the time of capture.
        /// </summary>
        public long DevicePosition { get; }

        /// <summary>
        /// The QPC (QueryPerformanceCounter) position at the time of capture, in 100-nanosecond units.
        /// </summary>
        public long QPCPosition { get; }

        internal unsafe CaptureBufferLease(AudioCaptureClient owner, IntPtr bufferPointer,
            int frameCount, int bytesPerFrame, AudioClientBufferFlags flags,
            long devicePosition, long qpcPosition)
        {
            this.owner = owner;
            this.frameCount = frameCount;
            Flags = flags;
            DevicePosition = devicePosition;
            QPCPosition = qpcPosition;
            Buffer = new ReadOnlySpan<byte>((void*)bufferPointer, frameCount * bytesPerFrame);
        }

        /// <summary>
        /// Releases the buffer back to WASAPI.
        /// </summary>
        public void Release()
        {
            if (owner != null)
            {
                owner.ReleaseBuffer(frameCount);
                owner = null;
            }
        }

        /// <summary>
        /// Releases the buffer back to WASAPI.
        /// </summary>
        public void Dispose()
        {
            Release();
        }
    }
}
