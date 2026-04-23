using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// Event args for a recording callback. Carries one chunk of captured PCM data.
    /// </summary>
    /// <remarks>
    /// The data exposed by this type aliases a producer-owned buffer that will be reused on the
    /// next recording callback. Consumers must process the data (or copy it) before the event
    /// handler returns — retaining <see cref="Buffer"/> or <see cref="BufferSpan"/> past the
    /// handler is not supported and will see stale or torn data.
    /// </remarks>
    public class WaveInEventArgs : EventArgs
    {
        // Exactly one of these is the canonical storage:
        //  - arrayBacking non-null: legacy (byte[], int bytes) ctor. bytesInArray is the valid length.
        //  - arrayBacking null:      memoryBacking is the canonical storage. Memory length == BytesRecorded.
        private readonly byte[] arrayBacking;
        private readonly int bytesInArray;
        private readonly ReadOnlyMemory<byte> memoryBacking;
        private byte[] lazyMaterializedArray;

        /// <summary>
        /// Creates new event args backed by a byte array. The first <paramref name="bytes"/>
        /// bytes of <paramref name="buffer"/> hold the recorded data; bytes beyond that are not
        /// meaningful. Kept for backward compatibility with callers that already own a byte[]
        /// (most notably WinMM <c>WaveIn</c>, whose pinned buffers are native-addressable).
        /// </summary>
        public WaveInEventArgs(byte[] buffer, int bytes)
        {
            arrayBacking = buffer ?? throw new ArgumentNullException(nameof(buffer));
            bytesInArray = bytes;
        }

        /// <summary>
        /// Creates new event args backed directly by a <see cref="ReadOnlyMemory{Byte}"/>.
        /// Intended for producers that can hand over captured audio without first copying into
        /// a managed byte array — for example, WASAPI capture wrapping a native buffer pointer
        /// in a <see cref="System.Buffers.MemoryManager{T}"/>.
        /// </summary>
        /// <remarks>
        /// The length of <paramref name="buffer"/> must equal the number of valid recorded bytes.
        /// <see cref="BytesRecorded"/> returns <c>buffer.Length</c>.
        /// </remarks>
        public WaveInEventArgs(ReadOnlyMemory<byte> buffer)
        {
            memoryBacking = buffer;
        }

        /// <summary>
        /// Buffer containing recorded data. Length of the returned array may exceed
        /// <see cref="BytesRecorded"/> — only the first <see cref="BytesRecorded"/> bytes are valid.
        /// </summary>
        /// <remarks>
        /// Kept for backward compatibility. Prefer <see cref="BufferSpan"/> — it is sliced to
        /// the recorded length and avoids the array-materialisation fallback described below.
        ///
        /// When this event is backed by a non-array <see cref="ReadOnlyMemory{Byte}"/> (e.g. a
        /// native WASAPI buffer wrapped in a <see cref="System.Buffers.MemoryManager{T}"/>), reading this
        /// property allocates a fresh byte[] and copies the data into it (cached on the event
        /// for subsequent reads). Consumers on a zero-copy capture path should read
        /// <see cref="BufferSpan"/> instead to avoid that allocation.
        /// </remarks>
        public byte[] Buffer
        {
            get
            {
                if (arrayBacking != null) return arrayBacking;
                var cached = lazyMaterializedArray;
                if (cached != null) return cached;
                if (MemoryMarshal.TryGetArray(memoryBacking, out var segment)
                    && segment.Array != null
                    && segment.Offset == 0
                    && segment.Count == segment.Array.Length)
                {
                    lazyMaterializedArray = segment.Array;
                    return segment.Array;
                }
                var copy = memoryBacking.ToArray();
                lazyMaterializedArray = copy;
                return copy;
            }
        }

        /// <summary>
        /// Recorded data as a read-only span, sliced to <see cref="BytesRecorded"/>.
        /// </summary>
        /// <remarks>
        /// Same aliasing contract as <see cref="Buffer"/> — the span points at a producer-owned
        /// buffer that is reused on the next recording callback. Do not retain past the end of
        /// the event handler; copy the data if you need it to outlive the call.
        /// </remarks>
        public ReadOnlySpan<byte> BufferSpan => arrayBacking != null
            ? arrayBacking.AsSpan(0, bytesInArray)
            : memoryBacking.Span;

        /// <summary>
        /// The number of recorded bytes. See <see cref="Buffer"/>, <see cref="BufferSpan"/>.
        /// </summary>
        public int BytesRecorded => arrayBacking != null ? bytesInArray : memoryBacking.Length;
    }
}
