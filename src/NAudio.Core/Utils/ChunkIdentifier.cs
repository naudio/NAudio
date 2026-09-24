using System;
using System.Text;

namespace NAudio.Utils;

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
        return ChunkIdentifierToInt32(Encoding.UTF8.GetBytes(s));
    }

    /// <summary>
    /// Chunk identifier to Int32 (replaces mmioStringToFOURCC)
    /// </summary>
    /// <param name="identifier">four character chunk identifier</param>
    /// <returns>Chunk identifier as int 32</returns>
    public static int ChunkIdentifierToInt32(ReadOnlySpan<byte> identifier)
    {
        return BitConverter.ToInt32(identifier);
    }
}
