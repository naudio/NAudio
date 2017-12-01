using System;
using System.Collections.Generic;
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

        private static readonly string[] KnownWAVChunkIds = new string[] { "fmt ", "data", "fact", "cue ", "plst", "list", "labl", "ltxt", "note", "smpl", "inst" };

        /// <summary>
        /// List of all standard chunk identifiers
        /// </summary>
        /// <returns>IEnumerable:int of known chunk identifiers</returns>
        public static IEnumerable<int> KnownChunkIdentifiers()
        {
            foreach (var sId in KnownWAVChunkIds)
            {
                yield return ChunkIdentifierToInt32(sId);
            }
        }
    }
}
