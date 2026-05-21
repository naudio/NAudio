using System;
using System.IO;
using System.Text;
using NAudio.Wave;

namespace NAudioTests.Utils
{
    /// <summary>
    /// Hand-builds a RIFF/WAVE byte stream with arbitrary extra chunks. Used by chunk-reader
    /// fixtures that need to exercise edge cases NAudio's own writer won't produce — truncated
    /// chunks, malformed layouts, unusual orderings, unknown chunk ids. Happy-path tests
    /// should prefer round-tripping through <see cref="WaveFileWriter"/> for realism.
    /// </summary>
    internal static class WaveFileBuilder
    {
        public sealed class Chunk
        {
            public Chunk(string id, byte[] data, bool beforeData = false)
            {
                if (id == null || id.Length != 4)
                    throw new ArgumentException("id must be 4 characters", nameof(id));
                Id = id;
                Data = data ?? Array.Empty<byte>();
                BeforeData = beforeData;
            }

            public string Id { get; }
            public byte[] Data { get; }
            public bool BeforeData { get; }
        }

        public static byte[] Build(WaveFormat format, byte[] audioData, params Chunk[] extraChunks)
        {
            audioData ??= Array.Empty<byte>();
            extraChunks ??= Array.Empty<Chunk>();

            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(0); // placeholder for RIFF size
                w.Write(Encoding.ASCII.GetBytes("WAVE"));

                w.Write(Encoding.ASCII.GetBytes("fmt "));
                format.Serialize(w);

                foreach (var chunk in extraChunks)
                {
                    if (chunk.BeforeData) WriteChunk(w, chunk);
                }

                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(audioData.Length);
                w.Write(audioData);
                if ((audioData.Length & 1) == 1) w.Write((byte)0);

                foreach (var chunk in extraChunks)
                {
                    if (!chunk.BeforeData) WriteChunk(w, chunk);
                }

                long fileLength = ms.Length;
                ms.Position = 4;
                w.Write((uint)(fileLength - 8));
            }

            return ms.ToArray();
        }

        private static void WriteChunk(BinaryWriter w, Chunk chunk)
        {
            w.Write(Encoding.ASCII.GetBytes(chunk.Id));
            w.Write(chunk.Data.Length);
            w.Write(chunk.Data);
            if ((chunk.Data.Length & 1) == 1) w.Write((byte)0);
        }
    }
}
