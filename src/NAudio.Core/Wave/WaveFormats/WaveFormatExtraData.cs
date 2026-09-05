using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// A WaveFormat that keeps the format-specific extra bytes (cbSize) it was read with, without
/// interpreting them. Reading a WAV fmt chunk produces one of these, and
/// <see cref="WaveFormat.MarshalFromPtr"/> falls back to it for an encoding NAudio has no
/// dedicated subclass for.
/// </summary>
public class WaveFormatExtraData : WaveFormat
{
    private byte[] extraData = Array.Empty<byte>();

    /// <summary>
    /// The extra bytes that followed the WAVEFORMATEX header, exactly
    /// <see cref="WaveFormat.ExtraSize"/> of them.
    /// </summary>
    public byte[] ExtraData => extraData;

    /// <summary>
    /// Creates an empty instance, to be filled in by <see cref="WaveFormat.FromFormatChunk"/>
    /// </summary>
    internal WaveFormatExtraData()
    {
    }

    /// <summary>
    /// Reads this structure from a BinaryReader
    /// </summary>
    public WaveFormatExtraData(BinaryReader reader)
        : base(reader)
    {
        ReadExtraData(reader);
    }

    internal void ReadExtraData(BinaryReader reader)
    {
        if (extraSize <= 0)
        {
            return;
        }
        // Sized from cbSize. This used to be a fixed 100-byte array because
        // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)] needed a compile-time size, and
        // a format declaring more than that had all of its extra data discarded (issue #482).
        extraData = reader.ReadBytes(extraSize);
        if (extraData.Length < extraSize)
        {
            // The stream ended early. Keep what arrived and correct cbSize, so the format stays
            // self-describing and Serialize writes exactly the bytes that exist.
            extraSize = (short)extraData.Length;
        }
    }

    /// <summary>
    /// Writes this structure to a BinaryWriter
    /// </summary>
    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        if (extraSize > 0)
        {
            writer.Write(extraData, 0, extraSize);
        }
    }
}
