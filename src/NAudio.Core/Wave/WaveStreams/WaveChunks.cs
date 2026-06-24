using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// A read-only view of the non-essential RIFF chunks found in a WAV file
    /// (everything except the <c>fmt</c> and <c>data</c> chunks, which are handled
    /// directly by <see cref="WaveFileReader"/>).
    ///
    /// Chunk metadata (id, length, stream position) is materialised when the file is opened;
    /// chunk contents are read lazily via <see cref="GetData"/> or an
    /// <see cref="IWaveChunkInterpreter{T}"/>.
    /// </summary>
    public sealed class WaveChunks : IReadOnlyList<RiffChunk>
    {
        private readonly Stream stream;
        private readonly IReadOnlyList<RiffChunk> chunks;

        internal WaveChunks(Stream stream, IReadOnlyList<RiffChunk> chunks)
        {
            this.stream = stream;
            this.chunks = chunks;
        }

        /// <summary>
        /// Number of chunks.
        /// </summary>
        public int Count => chunks.Count;

        /// <summary>
        /// Gets the chunk metadata at the given index.
        /// </summary>
        public RiffChunk this[int index] => chunks[index];

        /// <inheritdoc />
        public IEnumerator<RiffChunk> GetEnumerator() => chunks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns true if a chunk with the given four-character identifier is present.
        /// Comparison is case-insensitive (RIFF treats <c>LIST</c> and <c>list</c> as the same chunk).
        /// </summary>
        public bool Contains(string chunkId) => Find(chunkId) != null;

        /// <summary>
        /// Returns the first chunk matching the given four-character identifier, or null if none is present.
        /// Comparison is case-insensitive.
        /// </summary>
        public RiffChunk Find(string chunkId)
        {
            if (chunkId == null) throw new ArgumentNullException(nameof(chunkId));
            foreach (var chunk in chunks)
            {
                if (string.Equals(chunk.IdentifierAsString, chunkId, StringComparison.OrdinalIgnoreCase))
                {
                    return chunk;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns every chunk matching the given four-character identifier.
        /// Comparison is case-insensitive.
        /// </summary>
        public IEnumerable<RiffChunk> FindAll(string chunkId)
        {
            if (chunkId == null) throw new ArgumentNullException(nameof(chunkId));
            foreach (var chunk in chunks)
            {
                if (string.Equals(chunk.IdentifierAsString, chunkId, StringComparison.OrdinalIgnoreCase))
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>
        /// Reads the raw bytes for the given chunk from the underlying stream.
        /// The stream position is preserved across the call.
        /// </summary>
        public byte[] GetData(RiffChunk chunk)
        {
            if (chunk == null) throw new ArgumentNullException(nameof(chunk));
            long oldPosition = stream.Position;
            stream.Position = chunk.StreamPosition;
            byte[] data = new byte[chunk.Length];
            int read = 0;
            while (read < data.Length)
            {
                int n = stream.Read(data, read, data.Length - read);
                if (n <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not read chunk data: expected {data.Length} bytes, got {read}");
                }
                read += n;
            }
            stream.Position = oldPosition;
            return data;
        }

        /// <summary>
        /// Runs the given interpreter over this chunk collection. Returns <c>default</c>
        /// if the interpreter's required chunks are not present.
        /// </summary>
        public T Read<T>(IWaveChunkInterpreter<T> interpreter)
        {
            if (interpreter == null) throw new ArgumentNullException(nameof(interpreter));
            return interpreter.Interpret(this);
        }
    }
}
