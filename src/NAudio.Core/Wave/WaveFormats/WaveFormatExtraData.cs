using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// This class used for marshalling from unmanaged code
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormatExtraData : WaveFormat
{
    // try with 100 bytes for now, increase if necessary
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    private readonly byte[] extraData = new byte[100];

    /// <summary>
    /// Allows the extra data to be read
    /// </summary>
    public byte[] ExtraData => extraData;

    /// <summary>
    /// parameterless constructor for marshalling
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
        if (extraSize > extraData.Length)
        {
            // The fmt chunk declares more extra bytes than our fixed buffer can hold.
            // Consume them so the stream stays aligned for the next chunk, then discard.
            Debug.WriteLine($"Discarding {extraSize} bytes of fmt extra data exceeding the {extraData.Length}-byte buffer");
            reader.ReadBytes(extraSize);
            extraSize = 0;
        }
        if (extraSize > 0)
        {
            reader.Read(extraData, 0, extraSize);
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
