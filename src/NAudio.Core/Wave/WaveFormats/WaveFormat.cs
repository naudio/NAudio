using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace NAudio.Wave;

/// <summary>
/// Represents a Wave file format
/// </summary>
/// <remarks>
/// Not an interop type. It once carried <c>[StructLayout]</c> and was marshalled directly as a
/// WAVEFORMATEX, but a class hierarchy cannot be marshalled correctly under Native AOT — the
/// inherited fields are dropped, so a subclass decodes from offset 0 with its base fields left at
/// their defaults. Use <see cref="ToWaveFormatExBytes"/>, <see cref="MarshalToPtr"/> or
/// <see cref="MarshalFromPtr"/> to cross a native boundary.
/// See https://github.com/naudio/NAudio/issues/1432.
/// </remarks>
public class WaveFormat
{
    /// <summary>format type</summary>
    protected WaveFormatEncoding waveFormatTag;
    /// <summary>number of channels</summary>
    protected short channels;
    /// <summary>sample rate</summary>
    protected int sampleRate;
    /// <summary>for buffer estimation</summary>
    protected int averageBytesPerSecond;
    /// <summary>block size of data</summary>
    protected short blockAlign;
    /// <summary>number of bits per sample of mono data</summary>
    protected short bitsPerSample;
    /// <summary>number of following bytes</summary>
    protected short extraSize;

    /// <summary>
    /// Creates a new PCM 44.1Khz stereo 16 bit format
    /// </summary>
    public WaveFormat() : this(44100, 16, 2)
    {

    }

    /// <summary>
    /// Creates a new 16 bit wave format with the specified sample
    /// rate and channel count
    /// </summary>
    /// <param name="sampleRate">Sample Rate</param>
    /// <param name="channels">Number of channels</param>
    public WaveFormat(int sampleRate, int channels)
        : this(sampleRate, 16, channels)
    {
    }

    /// <summary>
    /// Gets the size of a wave buffer equivalent to the latency in milliseconds.
    /// </summary>
    /// <param name="milliseconds">The milliseconds.</param>
    /// <returns></returns>
    public int ConvertLatencyToByteSize(int milliseconds)
    {
        int bytes = (int)((AverageBytesPerSecond / 1000.0) * milliseconds);
        if ((bytes % BlockAlign) != 0)
        {
            // Return the upper BlockAligned
            bytes = bytes + BlockAlign - (bytes % BlockAlign);
        }
        return bytes;
    }

    /// <summary>
    /// Creates a WaveFormat with custom members
    /// </summary>
    /// <param name="tag">The encoding</param>
    /// <param name="sampleRate">Sample Rate</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="averageBytesPerSecond">Average Bytes Per Second</param>
    /// <param name="blockAlign">Block Align</param>
    /// <param name="bitsPerSample">Bits Per Sample</param>
    /// <returns></returns>
    public static WaveFormat CreateCustomFormat(WaveFormatEncoding tag, int sampleRate, int channels, int averageBytesPerSecond, int blockAlign, int bitsPerSample)
    {
        WaveFormat waveFormat = new WaveFormat();
        waveFormat.waveFormatTag = tag;
        waveFormat.channels = (short)channels;
        waveFormat.sampleRate = sampleRate;
        waveFormat.averageBytesPerSecond = averageBytesPerSecond;
        waveFormat.blockAlign = (short)blockAlign;
        waveFormat.bitsPerSample = (short)bitsPerSample;
        waveFormat.extraSize = 0;
        return waveFormat;
    }

    /// <summary>
    /// Creates an A-law wave format
    /// </summary>
    /// <param name="sampleRate">Sample Rate</param>
    /// <param name="channels">Number of Channels</param>
    /// <returns>Wave Format</returns>
    public static WaveFormat CreateALawFormat(int sampleRate, int channels)
    {
        return CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, channels, 8);
    }

    /// <summary>
    /// Creates a Mu-law wave format
    /// </summary>
    /// <param name="sampleRate">Sample Rate</param>
    /// <param name="channels">Number of Channels</param>
    /// <returns>Wave Format</returns>
    public static WaveFormat CreateMuLawFormat(int sampleRate, int channels)
    {
        return CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, channels, 8);
    }

    /// <summary>
    /// Creates a new PCM format with the specified sample rate, bit depth and channels
    /// </summary>
    public WaveFormat(int rate, int bits, int channels)
    {
        if (channels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be 1 or greater");
        }
        // minimum 16 bytes, sometimes 18 for PCM
        waveFormatTag = WaveFormatEncoding.Pcm;
        this.channels = (short)channels;
        sampleRate = rate;
        bitsPerSample = (short)bits;
        extraSize = 0;

        blockAlign = (short)(channels * (bits / 8));
        averageBytesPerSecond = this.sampleRate * this.blockAlign;
    }

    /// <summary>
    /// Creates a new 32 bit IEEE floating point wave format
    /// </summary>
    /// <param name="sampleRate">sample rate</param>
    /// <param name="channels">number of channels</param>
    public static WaveFormat CreateIeeeFloatWaveFormat(int sampleRate, int channels)
    {
        var wf = new WaveFormat();
        wf.waveFormatTag = WaveFormatEncoding.IeeeFloat;
        wf.channels = (short)channels;
        wf.bitsPerSample = 32;
        wf.sampleRate = sampleRate;
        wf.blockAlign = (short)(4 * channels);
        wf.averageBytesPerSecond = sampleRate * wf.blockAlign;
        wf.extraSize = 0;
        return wf;
    }

    /// <summary>
    /// Helper function to retrieve a WaveFormat structure from a pointer
    /// </summary>
    /// <param name="pointer">WaveFormat structure</param>
    /// <returns></returns>
    public static WaveFormat MarshalFromPtr(IntPtr pointer)
    {
        // Decoded by hand rather than with Marshal.PtrToStructure<T>. The WaveFormat
        // subclasses express their native layout through inheritance, and NativeAOT ignores
        // base-class fields when it builds the marshalling stub for such a class — so
        // PtrToStructure<WaveFormatExtensible> reads wValidBitsPerSample/dwChannelMask/
        // subFormat from offset 0 and silently returns a format with a garbage sample rate.
        // See https://github.com/naudio/NAudio/issues/1425.
        byte[] blob = ReadFormatBlob(pointer);
        var encoding = (WaveFormatEncoding)BitConverter.ToUInt16(blob, FormatChunkPrefixLength);
        switch (encoding)
        {
            case WaveFormatEncoding.Extensible:
                return new WaveFormatExtensible(OpenBlob(blob));
            case WaveFormatEncoding.Adpcm:
                return new AdpcmWaveFormat(OpenBlob(blob));
            case WaveFormatEncoding.Gsm610:
                return new Gsm610WaveFormat(OpenBlob(blob));
        }

        var waveFormat = new WaveFormat(OpenBlob(blob));
        if (encoding == WaveFormatEncoding.Pcm)
        {
            // can't rely on extra size even being there for PCM so blank it to avoid reading
            // corrupt data
            waveFormat.extraSize = 0;
        }
        else if (waveFormat.ExtraSize > 0)
        {
            waveFormat = new WaveFormatExtraData(OpenBlob(blob));
        }
        return waveFormat;
    }

    /// <summary>
    /// Number of bytes the <see cref="Serialize"/> / <see cref="WaveFormat(BinaryReader)"/>
    /// pair spends on the fmt chunk length that precedes the WAVEFORMATEX itself.
    /// </summary>
    private const int FormatChunkPrefixLength = 4;

    /// <summary>
    /// Size of a native WAVEFORMATEX: the 16-byte PCMWAVEFORMAT plus the cbSize field.
    /// </summary>
    private const int WaveFormatExLength = 18;

    /// <summary>
    /// Size of a canonical PCMWAVEFORMAT, which carries no cbSize field.
    /// </summary>
    private const int PcmWaveFormatLength = 16;

    private static BinaryReader OpenBlob(byte[] blob) => new(new MemoryStream(blob, writable: false));

    /// <summary>
    /// Extra bytes the corresponding WaveFormat subclass always occupies, i.e. how many
    /// <see cref="Marshal.PtrToStructure{T}(IntPtr)"/> used to read regardless of cbSize.
    /// </summary>
    private static int MinimumExtraSize(WaveFormatEncoding encoding) => encoding switch
    {
        WaveFormatEncoding.Extensible => 22, // wValidBitsPerSample + dwChannelMask + SubFormat
        WaveFormatEncoding.Adpcm => 32,      // samplesPerBlock + numCoeff + 14 coefficients
        WaveFormatEncoding.Gsm610 => 2,      // samplesPerBlock
        _ => 0,
    };

    /// <summary>
    /// Copies a native WAVEFORMATEX block into the byte layout the BinaryReader constructors
    /// expect: a 4-byte fmt chunk length followed by the WAVEFORMATEX and its extra bytes.
    /// </summary>
    private static byte[] ReadFormatBlob(IntPtr pointer)
    {
        // Reading all 18 bytes means overreading a canonical 16-byte PCMWAVEFORMAT by the
        // width of cbSize. Marshal.PtrToStructure<WaveFormat> did the same, and callers rely
        // on it, so keep the behaviour — but don't trust those two bytes for PCM.
        var header = new byte[WaveFormatExLength];
        Marshal.Copy(pointer, header, 0, header.Length);
        var encoding = (WaveFormatEncoding)BitConverter.ToUInt16(header, 0);
        int extraSize = encoding == WaveFormatEncoding.Pcm ? 0 : BitConverter.ToInt16(header, 16);
        if (extraSize < 0)
        {
            extraSize = 0;
        }
        // Marshal.PtrToStructure<T> read a fixed number of extra bytes for these encodings
        // whatever cbSize said, and drivers do under-report it. Keep reading at least that
        // much so an under-reporting driver still yields populated subclass fields rather
        // than a zeroed SubFormat — this is never a larger overread than the old code's.
        extraSize = Math.Max(extraSize, MinimumExtraSize(encoding));

        var blob = new byte[FormatChunkPrefixLength + WaveFormatExLength + extraSize];
        BitConverter.TryWriteBytes(blob.AsSpan(), WaveFormatExLength + extraSize);
        Buffer.BlockCopy(header, 0, blob, FormatChunkPrefixLength, header.Length);
        if (extraSize > 0)
        {
            Marshal.Copy(pointer + WaveFormatExLength, blob, FormatChunkPrefixLength + WaveFormatExLength, extraSize);
        }
        return blob;
    }

    /// <summary>
    /// Helper function to marshal WaveFormat to an IntPtr
    /// </summary>
    /// <param name="format">WaveFormat</param>
    /// <returns>IntPtr to WaveFormat structure (needs to be freed by callee)</returns>
    public static IntPtr MarshalToPtr(WaveFormat format)
    {
        // Built from Serialize() rather than Marshal.StructureToPtr for the reason given on
        // MarshalFromPtr: under NativeAOT the marshaller drops the base-class fields of a
        // WaveFormat subclass, writing (for example) a WaveFormatExtensible's subformat GUID
        // over the sample rate. See https://github.com/naudio/NAudio/issues/1425.
        byte[] blob = format.ToWaveFormatExBytes();
        IntPtr formatPointer = Marshal.AllocHGlobal(blob.Length);
        Marshal.Copy(blob, 0, formatPointer, blob.Length);
        return formatPointer;
    }

    /// <summary>
    /// Renders this WaveFormat as a native WAVEFORMATEX block — the fixed 18-byte header
    /// (cbSize always present) followed by cbSize bytes of format-specific extra data.
    /// Use this when writing into a buffer you already own; <see cref="MarshalToPtr"/>
    /// wraps it for the common case of needing a freshly allocated unmanaged block.
    /// </summary>
    /// <returns>The WAVEFORMATEX bytes, always at least 18 long.</returns>
    public byte[] ToWaveFormatExBytes()
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new BinaryWriter(memoryStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            Serialize(writer);
        }
        byte[] serialized = memoryStream.ToArray();

        // Serialize() writes a fmt chunk: a 4-byte length, then the WAVEFORMATEX. Drop the
        // length prefix, and restore the cbSize field that the canonical 16-byte PCM form
        // omits — native callers always read a full 18-byte WAVEFORMATEX. Math.Max leaves
        // the two extra bytes zeroed, which is the cbSize = 0 those callers expect.
        //
        // The block is also never shorter than the cbSize it advertises. A native consumer
        // reads 18 + cbSize bytes, so a subclass that declares extraSize but doesn't write
        // it in Serialize (as Mp3WaveFormat itself did until recently, and as a third-party
        // subclass laying its fields out via [StructLayout] still might) would otherwise
        // have the consumer read off the end of the allocation.
        int length = serialized.Length - FormatChunkPrefixLength;
        int declared = WaveFormatExLength + Math.Max((int)extraSize, 0);
        // length == PcmWaveFormatLength is the canonical PCM shape, where Serialize omits
        // cbSize entirely and the Math.Max below pads it back to 18 with a zero.
        Debug.Assert(length >= declared || length == PcmWaveFormatLength,
            $"{GetType().Name}.Serialize wrote {length} bytes but cbSize advertises {declared}");
        var blob = new byte[Math.Max(length, Math.Max(declared, WaveFormatExLength))];
        Buffer.BlockCopy(serialized, FormatChunkPrefixLength, blob, 0, length);
        return blob;
    }

    /// <summary>
    /// Reads in a WaveFormat (with extra data) from a fmt chunk (chunk identifier and
    /// length should already have been read)
    /// </summary>
    /// <param name="br">Binary reader</param>
    /// <param name="formatChunkLength">Format chunk length</param>
    /// <returns>A WaveFormatExtraData</returns>
    public static WaveFormat FromFormatChunk(BinaryReader br, int formatChunkLength)
    {
        var waveFormat = new WaveFormatExtraData();
        waveFormat.ReadWaveFormat(br, formatChunkLength);
        waveFormat.ReadExtraData(br);
        return waveFormat;
    }

    private void ReadWaveFormat(BinaryReader br, int formatChunkLength)
    {
        if (formatChunkLength < 16)
            throw new InvalidDataException("Invalid WaveFormat Structure");
        waveFormatTag = (WaveFormatEncoding)br.ReadUInt16();
        channels = br.ReadInt16();
        sampleRate = br.ReadInt32();
        averageBytesPerSecond = br.ReadInt32();
        blockAlign = br.ReadInt16();
        bitsPerSample = br.ReadInt16();
        if (formatChunkLength > 16)
        {
            extraSize = br.ReadInt16();
            if (extraSize != formatChunkLength - 18)
            {
                Debug.WriteLine("Format chunk mismatch");
                extraSize = (short)(formatChunkLength - 18);
            }
        }
    }

    /// <summary>
    /// Reads a new WaveFormat object from a stream
    /// </summary>
    /// <param name="br">A binary reader that wraps the stream</param>
    public WaveFormat(BinaryReader br)
    {
        int formatChunkLength = br.ReadInt32();
        ReadWaveFormat(br, formatChunkLength);
    }

    /// <summary>
    /// Reports this WaveFormat as a string
    /// </summary>
    /// <returns>String describing the wave format</returns>
    public override string ToString()
    {
        switch (waveFormatTag)
        {
            case WaveFormatEncoding.Pcm:
            case WaveFormatEncoding.Extensible:
                // extensible just has some extra bits after the PCM header
                return $"{bitsPerSample} bit PCM: {sampleRate}Hz {channels} channels";
            case WaveFormatEncoding.IeeeFloat:
                return $"{bitsPerSample} bit IEEEFloat: {sampleRate}Hz {channels} channels";
            default:
                return waveFormatTag.ToString();
        }
    }

    /// <summary>
    /// Compares with another WaveFormat object
    /// </summary>
    /// <param name="obj">Object to compare to</param>
    /// <returns>True if the objects are the same</returns>
    public override bool Equals(object obj)
    {
        var other = obj as WaveFormat;
        if (other != null)
        {
            return waveFormatTag == other.waveFormatTag &&
                channels == other.channels &&
                sampleRate == other.sampleRate &&
                averageBytesPerSecond == other.averageBytesPerSecond &&
                blockAlign == other.blockAlign &&
                bitsPerSample == other.bitsPerSample;
        }
        return false;
    }

    /// <summary>
    /// Provides a Hashcode for this WaveFormat
    /// </summary>
    /// <returns>A hashcode</returns>
    public override int GetHashCode()
    {
        return (int)waveFormatTag ^
            channels ^
            sampleRate ^
            averageBytesPerSecond ^
            blockAlign ^
            bitsPerSample;
    }

    /// <summary>
    /// Returns the encoding type used
    /// </summary>
    public WaveFormatEncoding Encoding => waveFormatTag;

    /// <summary>
    /// Writes this WaveFormat object to a stream
    /// </summary>
    /// <param name="writer">the output stream</param>
    public virtual void Serialize(BinaryWriter writer)
    {
        // Canonical PCM uses the 16-byte PCMWAVEFORMAT layout with no cbSize field.
        // The cbSize field only belongs to WAVEFORMATEX (non-PCM). We still guard on
        // extraSize so a PCM-tagged subclass that carries extra data stays well-formed.
        bool writeExtraSize = !(Encoding == WaveFormatEncoding.Pcm && extraSize == 0);
        writer.Write(writeExtraSize ? 18 + extraSize : 16); // wave format length
        writer.Write((short)Encoding);
        writer.Write((short)Channels);
        writer.Write(SampleRate);
        writer.Write(AverageBytesPerSecond);
        writer.Write((short)BlockAlign);
        writer.Write((short)BitsPerSample);
        if (writeExtraSize)
        {
            writer.Write(extraSize);
        }
    }

    /// <summary>
    /// Returns the number of channels (1=mono,2=stereo etc)
    /// </summary>
    public int Channels => channels;

    /// <summary>
    /// Returns the sample rate (samples per second)
    /// </summary>
    public int SampleRate => sampleRate;

    /// <summary>
    /// Returns the average number of bytes used per second
    /// </summary>
    public int AverageBytesPerSecond => averageBytesPerSecond;

    /// <summary>
    /// Returns the block alignment
    /// </summary>
    public virtual int BlockAlign => blockAlign;

    /// <summary>
    /// Returns the number of bits per sample (usually 16 or 32, sometimes 24 or 8)
    /// Can be 0 for some codecs
    /// </summary>
    public int BitsPerSample => bitsPerSample;

    /// <summary>
    /// Returns the number of extra bytes used by this waveformat. Often 0,
    /// except for compressed formats which store extra data after the WAVEFORMATEX header
    /// </summary>
    public int ExtraSize => extraSize;
}
