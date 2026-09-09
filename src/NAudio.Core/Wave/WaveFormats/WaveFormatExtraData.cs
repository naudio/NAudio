using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// A WaveFormat that keeps the format-specific extra bytes (cbSize) it was read with, without
/// interpreting them. <see cref="WaveFormat.FromFormatChunk"/> and
/// <see cref="WaveFormat.MarshalFromPtr"/> fall back to it for an encoding NAudio has no
/// dedicated subclass for, and for one that has a subclass but arrived with less extra data
/// than that subclass describes.
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
    /// Reads this structure from a BinaryReader
    /// </summary>
    public WaveFormatExtraData(BinaryReader reader)
        : base(reader)
    {
        ReadExtraData(reader);
    }

    private void ReadExtraData(BinaryReader reader)
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
