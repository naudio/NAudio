using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Convenience extension methods over <see cref="WaveFileWriter"/> for NAudio's built-in
    /// chunk writers. Each extension serialises a strongly-typed object and calls
    /// <see cref="WaveFileWriter.AddChunk(string, byte[], ChunkPosition)"/>. Users can write
    /// their own similar extensions for additional chunk types.
    /// </summary>
    public static class WaveFileWriterExtensions
    {
        /// <summary>
        /// Writes a <see cref="CueList"/> as a pair of <c>cue </c> and <c>LIST/adtl</c> chunks.
        /// By convention these are placed after the data chunk; override <paramref name="position"/>
        /// if you need them before.
        /// </summary>
        public static void WriteCueList(this WaveFileWriter writer, CueList cues, ChunkPosition position = ChunkPosition.AfterData)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (cues == null || cues.Count == 0) return;
            writer.AddChunk("cue ", cues.SerializeCueChunkData(), position);
            writer.AddChunk("LIST", cues.SerializeAdtlListChunkData(), position);
        }

        /// <summary>
        /// Writes a <see cref="BroadcastExtension"/> as a <c>bext</c> chunk.
        /// By convention the <c>bext</c> chunk sits before the data chunk (per EBU Tech 3285),
        /// so this must be called before any audio is written.
        /// </summary>
        public static void WriteBroadcastExtension(this WaveFileWriter writer, BroadcastExtension bext, ChunkPosition position = ChunkPosition.BeforeData)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (bext == null) throw new ArgumentNullException(nameof(bext));
            writer.AddChunk("bext", bext.ToChunkData(), position);
        }

        /// <summary>
        /// Writes an <see cref="InfoMetadata"/> collection as a <c>LIST/INFO</c> chunk.
        /// Most tools place this after the data chunk; override <paramref name="position"/> if desired.
        /// Does nothing if the collection is null or empty.
        /// </summary>
        public static void WriteInfoMetadata(this WaveFileWriter writer, InfoMetadata info, ChunkPosition position = ChunkPosition.AfterData)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (info == null || info.Count == 0) return;
            writer.AddChunk("LIST", info.ToInfoListChunkData(), position);
        }
    }
}
