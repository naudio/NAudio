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
    /// <param name="identifier">four character chunk identifier</param>
    /// <returns>Chunk identifier as int 32</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="identifier"/> is not four characters, or does not encode to exactly four bytes.</exception>
    public static int ChunkIdentifierToInt32(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentOutOfRangeException.ThrowIfNotEqual(identifier.Length, 4, nameof(identifier));
        Span<byte> bytes = stackalloc byte[4];
        if (!Encoding.UTF8.TryGetBytes(identifier, bytes, out int bytesWritten) || bytesWritten != 4)
        {
            throw new ArgumentOutOfRangeException(nameof(identifier), identifier, "Chunk identifier must encode to exactly four bytes");
        }
        return ChunkIdentifierToInt32(bytes);
    }

    /// <summary>
    /// Chunk identifier to Int32 (replaces mmioStringToFOURCC)
    /// </summary>
    /// <param name="identifier">four character chunk identifier</param>
    /// <returns>Chunk identifier as int 32</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="identifier"/> is not exactly four bytes long.</exception>
    public static int ChunkIdentifierToInt32(ReadOnlySpan<byte> identifier)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(identifier.Length, 4, nameof(identifier));
        return BitConverter.ToInt32(identifier);
    }
}
