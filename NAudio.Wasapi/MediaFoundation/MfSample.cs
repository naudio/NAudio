using System;
using System.Runtime.InteropServices;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFSample providing managed access to media samples.
    /// </summary>
    public class MfSample : IDisposable
    {
        private Interfaces.IMFSample sampleInterface;
        private IntPtr nativePointer;

        internal MfSample(Interfaces.IMFSample sampleInterface, IntPtr nativePointer)
        {
            this.sampleInterface = sampleInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Gets the native COM pointer for this sample (for passing to other COM methods).
        /// </summary>
        internal IntPtr NativePointer => nativePointer;

        /// <summary>
        /// Gets or sets the presentation time of the sample, in 100-nanosecond units.
        /// </summary>
        public long SampleTime
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(sampleInterface.GetSampleTime(out var time));
                return time;
            }
            set => MediaFoundationException.ThrowIfFailed(sampleInterface.SetSampleTime(value));
        }

        /// <summary>
        /// Gets or sets the duration of the sample, in 100-nanosecond units.
        /// </summary>
        public long SampleDuration
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(sampleInterface.GetSampleDuration(out var duration));
                return duration;
            }
            set => MediaFoundationException.ThrowIfFailed(sampleInterface.SetSampleDuration(value));
        }

        /// <summary>
        /// Gets or sets flags associated with the sample.
        /// </summary>
        public int SampleFlags
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(sampleInterface.GetSampleFlags(out var flags));
                return flags;
            }
            set => MediaFoundationException.ThrowIfFailed(sampleInterface.SetSampleFlags(value));
        }

        /// <summary>
        /// Gets the number of buffers in the sample.
        /// </summary>
        public int BufferCount
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(sampleInterface.GetBufferCount(out var count));
                return count;
            }
        }

        /// <summary>
        /// Gets the total length of valid data in all buffers.
        /// </summary>
        public int TotalLength
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(sampleInterface.GetTotalLength(out var length));
                return length;
            }
        }

        /// <summary>
        /// Gets a buffer by index.
        /// </summary>
        /// <param name="index">Zero-based index of the buffer.</param>
        /// <returns>The buffer at the specified index.</returns>
        public MfMediaBuffer GetBufferByIndex(int index)
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.GetBufferByIndex(index, out var bufferPtr));
            var bufferInterface = (Interfaces.IMFMediaBuffer)Marshal.GetObjectForIUnknown(bufferPtr);
            return new MfMediaBuffer(bufferInterface, bufferPtr);
        }

        /// <summary>
        /// Adds a buffer to the sample.
        /// </summary>
        /// <param name="buffer">The buffer to add.</param>
        public void AddBuffer(MfMediaBuffer buffer)
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.AddBuffer(buffer.NativePointer));
        }

        /// <summary>
        /// Converts the sample to a single contiguous buffer.
        /// </summary>
        /// <returns>A contiguous buffer containing all sample data.</returns>
        public MfMediaBuffer ConvertToContiguousBuffer()
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.ConvertToContiguousBuffer(out var bufferPtr));
            var bufferInterface = (Interfaces.IMFMediaBuffer)Marshal.GetObjectForIUnknown(bufferPtr);
            return new MfMediaBuffer(bufferInterface, bufferPtr);
        }

        /// <summary>
        /// Copies the sample data to a buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        public void CopyToBuffer(MfMediaBuffer buffer)
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.CopyToBuffer(buffer.NativePointer));
        }

        /// <summary>
        /// Removes a buffer by index.
        /// </summary>
        /// <param name="index">Zero-based index of the buffer to remove.</param>
        public void RemoveBufferByIndex(int index)
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.RemoveBufferByIndex(index));
        }

        /// <summary>
        /// Removes all buffers from the sample.
        /// </summary>
        public void RemoveAllBuffers()
        {
            MediaFoundationException.ThrowIfFailed(sampleInterface.RemoveAllBuffers());
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            sampleInterface = null;
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
