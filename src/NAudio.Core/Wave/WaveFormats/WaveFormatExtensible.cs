using System;
using System.Runtime.InteropServices;
using NAudio.Dmo;

namespace NAudio.Wave;

/// <summary>
/// WaveFormatExtensible
/// http://www.microsoft.com/whdc/device/audio/multichaud.mspx
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormatExtensible : WaveFormat
{
    private readonly short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
    private readonly int dwChannelMask; // which channels are present in stream
    private readonly Guid subFormat;

    /// <summary>
    /// Parameterless constructor for marshalling
    /// </summary>
    private WaveFormatExtensible()
    {
    }

    /// <summary>
    /// Creates a new WaveFormatExtensible for PCM or IEEE
    /// </summary>
    public WaveFormatExtensible(int rate, int bits, int channels, int channelMask = 0)
        : base(rate, bits, channels)
    {
        waveFormatTag = WaveFormatEncoding.Extensible;
        extraSize = 22;
        wValidBitsPerSample = (short)bits;
        if (channelMask != 0)
        {
            dwChannelMask = channelMask;
        }
        else
        {
            for (int n = 0; n < channels; n++)
            {
                dwChannelMask |= (1 << n);
            }
        }
        if (bits == 32)
        {
            // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
            subFormat = AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
        }
        else
        {
            // KSDATAFORMAT_SUBTYPE_PCM
            subFormat = AudioMediaSubtypes.MEDIASUBTYPE_PCM;
        }

    }

    /// <summary>
    /// Creates a new WaveFormatExtensible with full control over the SubFormat,
    /// valid bits per sample and channel mask. Use this overload when the convenience
    /// PCM/IEEE constructor's assumptions don't fit — for example to create 32-bit
    /// integer PCM (rather than IEEE float), to carry a container size that differs
    /// from the valid bit depth (e.g. 24 valid bits in a 32-bit container), or to
    /// preserve an explicit channel layout.
    /// </summary>
    /// <param name="rate">Sample rate</param>
    /// <param name="bits">Container bits per sample (block alignment is derived from this)</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="subFormat">SubFormat GUID (e.g. one of <see cref="AudioMediaSubtypes"/>)</param>
    /// <param name="validBitsPerSample">Number of valid bits per sample (may be less than <paramref name="bits"/>)</param>
    /// <param name="channelMask">Channel mask describing the speaker layout, or 0 if unspecified</param>
    public WaveFormatExtensible(int rate, int bits, int channels, Guid subFormat, int validBitsPerSample, int channelMask)
        : base(rate, bits, channels)
    {
        waveFormatTag = WaveFormatEncoding.Extensible;
        extraSize = 22;
        wValidBitsPerSample = (short)validBitsPerSample;
        dwChannelMask = channelMask;
        this.subFormat = subFormat;
    }

    /// <summary>
    /// Creates a new WaveFormatExtensible with full control over the SubFormat,
    /// valid bits per sample and a strongly typed channel layout. See
    /// <see cref="WaveFormatExtensible(int, int, int, Guid, int, int)"/> for details.
    /// </summary>
    /// <param name="rate">Sample rate</param>
    /// <param name="bits">Container bits per sample (block alignment is derived from this)</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="subFormat">SubFormat GUID (e.g. one of <see cref="AudioMediaSubtypes"/>)</param>
    /// <param name="validBitsPerSample">Number of valid bits per sample (may be less than <paramref name="bits"/>)</param>
    /// <param name="channelMask">Speaker positions present in the stream</param>
    public WaveFormatExtensible(int rate, int bits, int channels, Guid subFormat, int validBitsPerSample, Speakers channelMask)
        : this(rate, bits, channels, subFormat, validBitsPerSample, (int)channelMask)
    {
    }

    /// <summary>
    /// WaveFormatExtensible for PCM or floating point can be awkward to work with
    /// This creates a regular WaveFormat structure representing the same audio format
    /// Returns the WaveFormat unchanged for non PCM or IEEE float
    /// </summary>
    /// <returns></returns>
    public WaveFormat ToStandardWaveFormat()
    {
        if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT && bitsPerSample == 32)
            return CreateIeeeFloatWaveFormat(sampleRate, channels);
        if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
            return new WaveFormat(sampleRate, bitsPerSample, channels);
        return this;
        //throw new InvalidOperationException("Not a recognised PCM or IEEE float format");
    }

    /// <summary>
    /// SubFormat (may be one of AudioMediaSubtypes)
    /// </summary>
    public Guid SubFormat { get { return subFormat; } }

    /// <summary>
    /// The number of valid bits per sample. May be less than
    /// <see cref="WaveFormat.BitsPerSample"/> when samples are stored in a larger
    /// container (e.g. 24 valid bits packed into a 32-bit container).
    /// </summary>
    public int ValidBitsPerSample => wValidBitsPerSample;

    /// <summary>
    /// The channel mask describing which speaker positions are present in the
    /// stream. Compare against the <see cref="Speakers"/> flags. Zero means no
    /// explicit layout was specified.
    /// </summary>
    public int ChannelMask => dwChannelMask;

    /// <summary>
    /// Serialize
    /// </summary>
    /// <param name="writer"></param>
    public override void Serialize(System.IO.BinaryWriter writer)
    {
        base.Serialize(writer);
        writer.Write(wValidBitsPerSample);
        writer.Write(dwChannelMask);
        byte[] guid = subFormat.ToByteArray();
        writer.Write(guid, 0, guid.Length);
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        return $"WAVE_FORMAT_EXTENSIBLE {AudioMediaSubtypes.GetAudioSubtypeName(subFormat)} " +
            $"{SampleRate}Hz {Channels} channels {BitsPerSample} bit";
    }
}
