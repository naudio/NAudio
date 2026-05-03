using System;
using System.Linq;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// Chunk Identifier helpers
    /// </summary>
    public static class ChunkIdentifier
    {
        /// <summary>
        /// Provides the chunk identifier value for DS64 .WAV files.
        /// </summary>
        public const int DS64ChunkIdentifier = 875983716;

        /// <summary>
        /// Provides the chunk identifier value for the WAVE RIFF files.
        /// </summary>
        public const int WAVEChunkIdentifier = 1163280727;

        /// <summary>
        /// Provides the chunk identifier value for the data chunk in WAV RIFF files.
        /// </summary>
        public const int DataChunkIdentifier = 1635017060;

        /// <summary>
        /// Provides the chunk identifier value for the format chunk in WAV RIFF files.
        /// </summary>
        public const int FormatChunkIdentifier = 544501094;

        /// <summary>
        /// Chunk identifier to Int32 (replaces mmioStringToFOURCC)
        /// </summary>
        /// <param name="s">four character chunk identifier</param>
        /// <returns>Chunk identifier as int 32</returns>
        public static int ChunkIdentifierToInt32(string s)
        {
            if (s.Length != 4) throw new ArgumentException("Must be a four character string");
            var bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length != 4) throw new ArgumentException("Must encode to exactly four bytes");
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
