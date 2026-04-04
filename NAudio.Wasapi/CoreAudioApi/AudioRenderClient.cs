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
        private IntPtr nativePointer;

        internal AudioRenderClient(IntPtr nativePointer)
        {
            this.nativePointer = nativePointer;
            audioRenderClientInterface = (IAudioRenderClient)Marshal.GetObjectForIUnknown(nativePointer);
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesRequested">Number of frames requested</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(int numFramesRequested)
        {
            CoreAudioException.ThrowIfFailed(audioRenderClientInterface.GetBuffer(numFramesRequested, out var bufferPointer));
            return bufferPointer;
        }

        /// <summary>
        /// Gets a writable Span over the WASAPI render buffer. The returned lease must be
        /// disposed (which calls ReleaseBuffer) before the next call to GetBuffer.
        /// Write audio data directly into <see cref="RenderBufferLease.Buffer"/> to avoid copies.
        /// </summary>
        /// <param name="numFramesRequested">Number of frames requested</param>
        /// <param name="bytesPerFrame">Bytes per frame (WaveFormat.BlockAlign)</param>
        /// <returns>A lease that provides a writable Span and releases the buffer on dispose</returns>
        public RenderBufferLease GetBufferLease(int numFramesRequested, int bytesPerFrame)
        {
            CoreAudioException.ThrowIfFailed(audioRenderClientInterface.GetBuffer(numFramesRequested, out var bufferPointer));
            return new RenderBufferLease(this, bufferPointer, numFramesRequested, bytesPerFrame);
        }

        /// <summary>
        /// Release buffer
        /// </summary>
        /// <param name="numFramesWritten">Number of frames written</param>
        /// <param name="bufferFlags">Buffer flags</param>
        public void ReleaseBuffer(int numFramesWritten, AudioClientBufferFlags bufferFlags)
        {
            CoreAudioException.ThrowIfFailed(audioRenderClientInterface.ReleaseBuffer(numFramesWritten, bufferFlags));
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioRenderClientInterface != null)
            {
                audioRenderClientInterface = null;
            }
            // Deterministic release is important: in exclusive mode the device cannot be
            // re-opened until all COM references are released.  Marshal.Release on the raw
            // IntPtr works with both classic RCWs and [GeneratedComInterface] wrappers.
            // (Never do this from a finalizer — the COM runtime may already be torn down.)
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Provides zero-copy write access to a WASAPI render buffer.
    /// The buffer pointer is only valid until Dispose/Release is called.
    /// Must be used with <c>using</c> to ensure ReleaseBuffer is called.
    /// </summary>
    public ref struct RenderBufferLease
    {
        private AudioRenderClient owner;
        private readonly int frameCount;

        /// <summary>
        /// A writable span over the WASAPI render buffer. Write audio data here directly.
        /// </summary>
        public unsafe Span<byte> Buffer { get; }

        internal unsafe RenderBufferLease(AudioRenderClient owner, IntPtr bufferPointer, int frameCount, int bytesPerFrame)
        {
            this.owner = owner;
            this.frameCount = frameCount;
            Buffer = new Span<byte>((void*)bufferPointer, frameCount * bytesPerFrame);
        }

        /// <summary>
        /// Releases the buffer back to WASAPI, telling it how many frames were actually written.
        /// </summary>
        /// <param name="framesWritten">Number of frames written. If omitted, uses the full requested frame count.</param>
        /// <param name="flags">Buffer flags (default: None)</param>
        public void Release(int? framesWritten = null, AudioClientBufferFlags flags = AudioClientBufferFlags.None)
        {
            if (owner != null)
            {
                owner.ReleaseBuffer(framesWritten ?? frameCount, flags);
                owner = null;
            }
        }

        /// <summary>
        /// Releases the buffer with the full requested frame count.
        /// </summary>
        public void Dispose()
        {
            Release();
        }
    }
}
