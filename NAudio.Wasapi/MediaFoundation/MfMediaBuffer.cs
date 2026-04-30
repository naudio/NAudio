using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Wrapper for IMFMediaBuffer providing Span-based buffer access.
    /// Internal until the high-level API solidifies (see MODERNIZATION.md Phase 3).
    /// </summary>
    internal class MfMediaBuffer : IDisposable
    {
        private Interfaces.IMFMediaBuffer bufferInterface;
        private IntPtr nativePointer;

        internal MfMediaBuffer(Interfaces.IMFMediaBuffer bufferInterface, IntPtr nativePointer)
        {
            this.bufferInterface = bufferInterface;
            this.nativePointer = nativePointer;
        }

        /// <summary>
        /// Gets the native COM pointer for this buffer (for passing to other COM methods).
        /// </summary>
        internal IntPtr NativePointer => nativePointer;

        /// <summary>
        /// Gets the native COM interface for this buffer.
        /// </summary>
        internal Interfaces.IMFMediaBuffer NativeInterface => bufferInterface;

        /// <summary>
        /// Locks the buffer and returns a lease providing Span access.
        /// Must be disposed to unlock the buffer.
        /// </summary>
        public MediaBufferLease Lock()
        {
            MediaFoundationException.ThrowIfFailed(bufferInterface.Lock(out var pBuffer, out var maxLength, out var currentLength));
            return new MediaBufferLease(this, pBuffer, maxLength, currentLength);
        }

        internal void Unlock()
        {
            MediaFoundationException.ThrowIfFailed(bufferInterface.Unlock());
        }

        /// <summary>
        /// Gets or sets the length of valid data in the buffer.
        /// </summary>
        public int CurrentLength
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(bufferInterface.GetCurrentLength(out var length));
                return length;
            }
            set => MediaFoundationException.ThrowIfFailed(bufferInterface.SetCurrentLength(value));
        }

        /// <summary>
        /// Gets the allocated size of the buffer.
        /// </summary>
        public int MaxLength
        {
            get
            {
                MediaFoundationException.ThrowIfFailed(bufferInterface.GetMaxLength(out var length));
                return length;
            }
        }

        /// <summary>
        /// Finalizer — runs only if Dispose was not called. Releases the native IntPtr ref;
        /// the source-generated RCW has its own ComObject finalizer that releases its ref
        /// independently.
        /// </summary>
        ~MfMediaBuffer() => Dispose(disposing: false);

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
            if (disposing && bufferInterface != null)
            {
                ((ComObject)(object)bufferInterface).FinalRelease();
                bufferInterface = null;
            }
        }
    }

    /// <summary>
    /// Provides Span-based access to a locked IMFMediaBuffer.
    /// Must be disposed to unlock the buffer.
    /// Internal until the high-level API solidifies (see MODERNIZATION.md Phase 3).
    /// </summary>
    internal ref struct MediaBufferLease
    {
        private MfMediaBuffer owner;

        /// <summary>
        /// A writable span over the buffer memory.
        /// </summary>
        public unsafe Span<byte> Buffer { get; }

        /// <summary>
        /// The length of valid data in the buffer when it was locked.
        /// </summary>
        public int CurrentLength { get; }

        /// <summary>
        /// The maximum size of the buffer.
        /// </summary>
        public int MaxLength { get; }

        internal unsafe MediaBufferLease(MfMediaBuffer owner, IntPtr bufferPointer, int maxLength, int currentLength)
        {
            this.owner = owner;
            CurrentLength = currentLength;
            MaxLength = maxLength;
            Buffer = new Span<byte>((void*)bufferPointer, maxLength);
        }

        /// <summary>
        /// Unlocks the buffer.
        /// </summary>
        public void Dispose()
        {
            if (owner != null)
            {
                owner.Unlock();
                owner = null;
            }
        }
    }
}
