using System;
using System.Linq;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// Chunk Identifier helpers
    /// </summary>
    public class ChunkIdentifier
    {
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
