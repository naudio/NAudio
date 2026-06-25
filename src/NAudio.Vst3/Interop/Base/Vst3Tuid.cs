using System;
using System.Runtime.InteropServices.Marshalling;

namespace NAudio.Vst3.Interop;

/// <summary>
/// Helpers for the 16-byte raw <c>TUID</c> values that VST 3 uses for class and interface
/// identifiers (the <c>Steinberg::TUID</c> typedef = <c>char[16]</c>).
/// </summary>
internal static class Vst3Tuid
{
    /// <summary>
    /// Formats a 16-byte TUID as a 32-character hexadecimal string. Matches the encoding used
    /// by <see cref="Vst3ClassInfo.ClassId"/>.
    /// </summary>
    public static unsafe string Format(byte* tuid)
        => Convert.ToHexString(new ReadOnlySpan<byte>(tuid, 16));

    /// <summary>
    /// Parses a 32-character hex TUID (as produced by <see cref="Format"/>) back into a
    /// 16-byte raw buffer.
    /// </summary>
    public static void Parse(string hexClassId, Span<byte> dest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexClassId);
        if (hexClassId.Length != 32)
        {
            throw new ArgumentException(
                $"Class ID must be 32 hex characters; got {hexClassId.Length}",
                nameof(hexClassId));
        }
        if (dest.Length < 16)
        {
            throw new ArgumentException("Destination buffer must be at least 16 bytes.", nameof(dest));
        }
        Convert.FromHexString(hexClassId, dest, out _, out _);
    }
}

/// <summary>
/// Shared <see cref="StrategyBasedComWrappers"/> instance for projecting VST 3 native pointers
/// onto <c>[GeneratedComInterface]</c> wrappers and exposing managed CCWs back to the plug-in.
/// </summary>
/// <remarks>
/// Re-using one instance across the library matches the NAudio.Wasapi pattern and keeps the
/// internal wrapper cache coherent for a given native object identity.
/// </remarks>
internal static class Vst3ComWrappers
{
    public static StrategyBasedComWrappers Instance { get; } = new();
}
