using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFSample providing managed access to media samples.
    /// Internal until the high-level API solidifies (see MODERNIZATION.md Phase 3).
    /// </summary>
    internal class MfSample : IDisposable
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
            var bufferInterface = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                bufferPtr, CreateObjectFlags.UniqueInstance);
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
            var bufferInterface = (Interfaces.IMFMediaBuffer)ComActivation.ComWrappers.GetOrCreateObjectForComInstance(
                bufferPtr, CreateObjectFlags.UniqueInstance);
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
        /// Finalizer — runs only if Dispose was not called. Releases the native IntPtr ref;
        /// the source-generated RCW has its own ComObject finalizer that releases its ref
        /// independently.
        /// </summary>
        ~MfSample() => Dispose(disposing: false);

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the native IntPtr ref unconditionally. When called from
        /// <see cref="Dispose()"/> (disposing=true) also calls <c>FinalRelease</c> on the
        /// RCW; the finalizer path leaves the RCW alone because <c>ComObject</c> has its own
        /// finalizer with no defined ordering relative to ours.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (nativePointer != IntPtr.Zero)
            {
                Marshal.Release(nativePointer);
                nativePointer = IntPtr.Zero;
            }
            if (disposing && sampleInterface != null)
            {
                ((ComObject)(object)sampleInterface).FinalRelease();
                sampleInterface = null;
            }
        }
    }
}
