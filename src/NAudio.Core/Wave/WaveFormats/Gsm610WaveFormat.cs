using System.Runtime.InteropServices;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// GSM 610
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public class Gsm610WaveFormat : WaveFormat
{
    private readonly short samplesPerBlock;

    /// <summary>
    /// Creates a GSM 610 WaveFormat
    /// For now hardcoded to 13kbps
    /// </summary>
    public Gsm610WaveFormat()
    {
        waveFormatTag = WaveFormatEncoding.Gsm610;
        channels = 1;
        averageBytesPerSecond = 1625;
        bitsPerSample = 0; // must be zero
        blockAlign = 65;
        sampleRate = 8000;

        extraSize = 2;
        samplesPerBlock = 320;
    }

    /// <summary>
    /// Reads a Gsm610WaveFormat from a fmt chunk (a 4-byte length followed by the
    /// WAVEFORMATEX and its 2 extra bytes).
    /// </summary>
    internal Gsm610WaveFormat(BinaryReader reader) : base(reader)
    {
        if (extraSize >= 2)
        {
            samplesPerBlock = reader.ReadInt16();
        }
    }

    /// <summary>
    /// Samples per block
    /// </summary>
    public short SamplesPerBlock { get { return samplesPerBlock; } }

    /// <summary>
    /// Writes this structure to a BinaryWriter
    /// </summary>
    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        writer.Write(samplesPerBlock);
    }
}
