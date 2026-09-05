using System.IO;

namespace NAudio.Wave;

/// <summary>
/// DSP Group TrueSpeech
/// </summary>
public class TrueSpeechWaveFormat : WaveFormat
{
    private readonly short[] unknown;

    /// <summary>
    /// DSP Group TrueSpeech WaveFormat
    /// </summary>
    public TrueSpeechWaveFormat()
    {
        this.waveFormatTag = WaveFormatEncoding.DspGroupTrueSpeech;
        this.channels = 1;
        this.averageBytesPerSecond = 1067;
        this.bitsPerSample = 1;
        this.blockAlign = 32;
        this.sampleRate = 8000;

        this.extraSize = 32;
        this.unknown = new short[16];
        this.unknown[0] = 1;
        this.unknown[1] = 0xF0;
    }

    /// <summary>
    /// Writes this structure to a BinaryWriter
    /// </summary>
    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        foreach (short val in unknown)
        {
            writer.Write(val);
        }
    }
}
