using System;
using System.Buffers;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// A <see cref="MemoryManager{T}"/> that exposes a native audio buffer (e.g. one obtained
    /// from <c>IAudioCaptureClient::GetBuffer</c>) as <see cref="Memory{T}"/> / <see cref="Span{T}"/>
    /// without copying.
    /// </summary>
    /// <remarks>
    /// <para>Lifetime: the underlying native pointer is NOT owned by this class. The caller is
    /// responsible for keeping the buffer valid for as long as any <c>Memory</c> view
    /// exposed by this manager is in use — typically, the <c>GetBuffer</c>/<c>ReleaseBuffer</c>
    /// window on an <c>IAudioCaptureClient</c>.</para>
    /// <para>Callers must <see cref="IDisposable.Dispose"/> (via <c>using</c>) after use and
    /// before the native buffer is released; the <c>Memory</c> view becomes undefined after that.</para>
    /// </remarks>
    internal sealed unsafe class NativeAudioBufferMemoryManager : MemoryManager<byte>
    {
        private readonly byte* pointer;
        private readonly int length;

        public NativeAudioBufferMemoryManager(IntPtr buffer, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            pointer = (byte*)buffer;
            this.length = length;
        }

        public override Span<byte> GetSpan() => new Span<byte>(pointer, length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if ((uint)elementIndex > (uint)length)
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            return new MemoryHandle(pointer + elementIndex);
        }

        public override void Unpin()
        {
            // The native audio buffer is implicitly fixed for the GetBuffer/ReleaseBuffer window;
            // there is nothing to unpin here.
        }

        protected override void Dispose(bool disposing)
        {
            // Native buffer lifetime is controlled by the caller (via ReleaseBuffer).
            // Dispose exists so the MemoryManager contract is complete and callers can
            // use it with `using`.
        }
    }
}
