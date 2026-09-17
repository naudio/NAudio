using NAudio.Utils;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>A read-only stream of AIFF data based on an aiff file
/// with an associated WaveFormat
/// originally contributed to NAudio by Giawa
/// </summary>
public class AiffFileReader : WaveStream
{
    private readonly WaveFormat waveFormat;
    private readonly bool ownInput;
    private readonly long dataPosition;
    private readonly int dataChunkLength;
    private readonly List<AiffChunk> chunks = [];
    private readonly Stream waveStream;
    private bool disposed = false;
    private readonly Lock lockObject = new();

    /// <summary>Supports opening a AIF file</summary>
    /// <remarks>The AIF is of similar nastiness to the WAV format.
    /// This supports basic reading of uncompressed PCM AIF files,
    /// with 8, 16, 24 and 32 bit PCM data.
    /// </remarks>
    public AiffFileReader(string aiffFile) :
        this(File.OpenRead(aiffFile))
    {
        ownInput = true;
    }

    /// <summary>
    /// Creates an Aiff File Reader based on an input stream
    /// </summary>
    /// <param name="inputStream">The input stream containing a AIF file including header</param>
    public AiffFileReader(Stream inputStream)
    {
        // The caller owns a stream they passed in; only the filename constructor sets ownInput = true.
        ownInput = false;
        waveStream = inputStream;
        ReadAiffHeader(waveStream, out waveFormat, out dataPosition, out dataChunkLength, chunks);
        if (waveFormat.BlockAlign <= 0)
        {
            throw new InvalidDataException(
                $"Invalid AIFF file - block align is {waveFormat.BlockAlign} (channels={waveFormat.Channels}, bitsPerSample={waveFormat.BitsPerSample}).");
        }
        Position = 0;
    }

    /// <summary>
    /// Ensures valid AIFF header and then finds data offset.
    /// </summary>
    /// <param name="stream">The stream, positioned at the start of audio data</param>
    /// <param name="format">The format found</param>
    /// <param name="dataChunkPosition">The position of the data chunk</param>
    /// <param name="dataChunkLength">The length of the data chunk</param>
    /// <param name="chunks">Additional chunks found</param>
    public static void ReadAiffHeader(Stream stream, out WaveFormat format, out long dataChunkPosition, out int dataChunkLength, List<AiffChunk> chunks)
    {
        WaveFormat? formatFound = null;
        dataChunkPosition = -1;
        dataChunkLength = 0;
        chunks.Clear();

        if (ReadChunkName(stream) != "FORM"u8)
        {
            throw new FormatException("Not an AIFF file - no FORM header.");
        }

        _ = ReadUInt(stream); // File size, not used here
        ChunkName formType = ReadChunkName(stream);
        if (formType != "AIFC"u8 && formType != "AIFF"u8)
        {
            throw new FormatException("Not an AIFF file - no AIFF/AIFC header.");
        }

        while (stream.Position < stream.Length)
        {
            AiffChunk nextChunk = ReadChunkHeader(stream);
            if (nextChunk.ChunkName == "\0\0\0\0") break;

            if (stream.Position + nextChunk.ChunkLength > stream.Length)
            {
                break;
            }
            if (nextChunk.ChunkName == "COMM")
            {
                short numChannels = ReadShort(stream);
                uint numSampleFrames = ReadUInt(stream);
                short sampleSize = ReadShort(stream);
                double sampleRate = ReadIeeeExtended(stream);

                formatFound = new WaveFormat((int)sampleRate, sampleSize, numChannels);

                if (nextChunk.ChunkLength > 18 && formType == "AIFC"u8)
                {
                    // In an AIFC file, the compression format is tacked on to the COMM chunk
                    ChunkName compress = ReadChunkName(stream);
                    if (!compress.EqualsIgnoreCase("none"u8)) throw new FormatException("Compressed AIFC is not supported.");
                    stream.Position += (nextChunk.ChunkLength - 22);
                }
                else
                {
                    stream.Position += (nextChunk.ChunkLength - 18);
                }
            }
            else if (nextChunk.ChunkName == "SSND")
            {
                uint offset = ReadUInt(stream);
                uint blockSize = ReadUInt(stream);
                // The offset field is a run of pad bytes sitting between the SSND header and
                // the first sample frame (used to block-align the sound data), so it counts
                // against the chunk length as well as advancing the start. A file declaring a
                // bigger offset than the chunk holds has no readable sound data at all.
                long soundDataLength = (long)nextChunk.ChunkLength - 8 - offset;
                if (soundDataLength > int.MaxValue)
                {
                    // ckSize is a signed 32-bit long in the AIFF spec, so sound data cannot
                    // legally exceed 2GB - a larger value means the size field is garbage.
                    throw new FormatException(
                        $"Invalid AIFF file - SSND sound data length {soundDataLength} exceeds the 2GB AIFF limit.");
                }
                dataChunkPosition = nextChunk.ChunkStart + 16 + offset;
                dataChunkLength = soundDataLength > 0 ? (int)soundDataLength : 0;
                stream.Position += (nextChunk.ChunkLength - 8);
            }
            else
            {
                chunks.Add(nextChunk);
                stream.Position += nextChunk.ChunkLength;
            }
        }

        if (formatFound == null)
        {
            throw new FormatException("Invalid AIFF file - No COMM chunk found.");
        }
        if (dataChunkPosition == -1)
        {
            throw new FormatException("Invalid AIFF file - No SSND chunk found.");
        }

        format = formatFound;
    }

    /// <summary>
    /// Cleans up the resources associated with this AiffFileReader
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Release managed resources.
            if (!disposed)
            {
                // only dispose our source if we created it
                if (ownInput)
                {
                    waveStream.Dispose();
                }
                disposed = true;
            }
        }
        else
        {
            System.Diagnostics.Debug.Assert(false, "AiffFileReader was not disposed");
        }
        // Release unmanaged resources.
        // Set large fields to null.
        // Call Dispose on your base class.
        base.Dispose(disposing);
    }

    /// <summary>
    /// <see cref="WaveStream.WaveFormat"/>
    /// </summary>
    public override WaveFormat WaveFormat => waveFormat;

    /// <summary>
    /// <see cref="WaveStream.WaveFormat"/>
    /// </summary>
    public override long Length => dataChunkLength;

    /// <summary>
    /// Number of Samples (if possible to calculate)
    /// </summary>
    public long SampleCount
    {
        get
        {
            return waveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.Extensible or WaveFormatEncoding.IeeeFloat
                ? dataChunkLength / BlockAlign
                : throw new FormatException("Sample count is calculated only for the standard encodings.");
        }
    }

    /// <summary>
    /// Position in the AIFF file
    /// <see cref="Stream.Position"/>
    /// </summary>
    public override long Position
    {
        get
        {
            return waveStream.Position - dataPosition;
        }
        set
        {
            lock (lockObject)
            {
                value = Math.Min(value, Length);
                // make sure we don't get out of sync
                value -= (value % waveFormat.BlockAlign);
                waveStream.Position = value + dataPosition;
            }
        }
    }


    /// <summary>
    /// Reads bytes from the AIFF File into the provided span.
    /// AIFF sample data is big-endian on disk; this method swaps to little-endian.
    /// <see cref="Stream.Read(Span{byte})"/>
    /// </summary>
    public override int Read(Span<byte> buffer)
    {
        int count = buffer.Length;
        if (count % waveFormat.BlockAlign != 0)
        {
            throw new ArgumentException(
                $"Must read complete blocks: requested {count}, block align is {WaveFormat.BlockAlign}");
        }

        lock (lockObject)
        {
            // sometimes there is more junk at the end of the file past the data chunk
            if (Position + count > dataChunkLength)
            {
                count = (int)(dataChunkLength - Position);
                // dataChunkLength itself may not be a whole number of blocks
                // (truncated/malformed SSND); the byte-swap loops below assume
                // complete samples, so round down to the nearest block.
                count -= count % waveFormat.BlockAlign;
            }

            // Read big-endian source bytes into the caller's span, then swap in place.
            // A single Read on the source may legitimately return fewer bytes than asked for
            // (network, deflate and crypto streams all do this), so keep asking until the
            // buffer is full or the source runs out - the byte-swap loops below step a whole
            // sample at a time and would run off the end of a partial frame.
            buffer = buffer.Slice(0, count);
            int length = waveStream.ReadAtLeast(buffer, count, throwOnEndOfStream: false);
            length -= length % waveFormat.BlockAlign;
            buffer = buffer.Slice(0, length);
        }

        switch (WaveFormat.BitsPerSample)
        {
            case 8:
                // AIFF 8-bit PCM is signed two's-complement, whereas the shared
                // Pcm8BitToSampleProvider (and WAV) treat 8-bit as unsigned. There is no
                // endianness to swap, but flipping the sign bit converts the signed source
                // byte to the unsigned value the downstream converter expects. See issue #1178.
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] ^= 0x80;
                }
                break;
            case 16:
                for (int i = 0; i < buffer.Length; i += 2)
                {
                    (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
                }
                break;
            case 24:
                for (int i = 0; i < buffer.Length; i += 3)
                {
                    (buffer[i], buffer[i + 2]) = (buffer[i + 2], buffer[i]);
                }
                break;
            case 32:
                for (int i = 0; i < buffer.Length; i += 4)
                {
                    (buffer[i], buffer[i + 3]) = (buffer[i + 3], buffer[i]);
                    (buffer[i + 1], buffer[i + 2]) = (buffer[i + 2], buffer[i + 1]);
                }
                break;
            default:
                throw new FormatException("Unsupported PCM format.");
        }

        return buffer.Length;
    }

    /// <summary>
    /// Reads bytes from the AIFF File.
    /// <see cref="Stream.Read(byte[], int, int)"/>
    /// </summary>
    public override int Read(byte[] array, int offset, int count)
        => Read(array.AsSpan(offset, count));

    #region AiffChunk
    /// <summary>
    /// AIFF Chunk
    /// </summary>
    public struct AiffChunk
    {
        /// <summary>
        /// Chunk Name
        /// </summary>
        public string ChunkName;

        /// <summary>
        /// Chunk Length
        /// </summary>
        public uint ChunkLength;

        /// <summary>
        /// Chunk start
        /// </summary>
        public uint ChunkStart;

        /// <summary>
        /// Creates a new AIFF Chunk
        /// </summary>
        public AiffChunk(uint start, string name, uint length)
        {
            ChunkStart = start;
            ChunkName = name;
            ChunkLength = length + (uint)(length % 2 == 1 ? 1 : 0);
        }
    }

    [InlineArray(Length)]
    private struct ChunkName
    {
        public const int Length = 4;
        public int Value => Unsafe.As<byte, int>(ref first);
        public Span<byte> Span => MemoryMarshal.CreateSpan(ref first, Length);

        private byte first;

        public static bool operator ==(ChunkName name, ReadOnlySpan<byte> text)
        {
            return name.Span.SequenceEqual(text);
        }

        public static bool operator !=(ChunkName name, ReadOnlySpan<byte> text)
        {
            return !(name == text);
        }

        public override string ToString()
        {
            return string.Create(Length, this, (span, name) =>
            {
                ReadOnlySpan<byte> bytes = name.Span;
                for (int i = 0; i < bytes.Length; i++)
                {
                    span[i] = (char)bytes[i];
                }
            });
        }

        public bool EqualsIgnoreCase(ReadOnlySpan<byte> text)
        {
            for (int i = 0; i < Length; i++)
            {
                if (char.ToLowerInvariant((char)Span[i]) != char.ToLowerInvariant((char)text[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is ChunkName name && this.Value == name.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }

    private static AiffChunk ReadChunkHeader(Stream stream)
    {
        return new AiffChunk((uint)stream.Position, ReadChunkName(stream).ToString(), ReadUInt(stream));
    }

    private static double ReadIeeeExtended(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[10];
        if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
        {
            throw new InvalidDataException("Incorrect length for IEEE extended.");
        }
        return IEEE.ConvertFromIeeeExtended(buffer);
    }

    private static uint ReadUInt(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
        {
            throw new InvalidDataException("Incorrect length for int.");
        }
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    private static short ReadShort(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
        {
            throw new InvalidDataException("Incorrect length for short.");
        }
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    private static ChunkName ReadChunkName(Stream stream)
    {
        ChunkName name = default;
        if (stream.ReadAtLeast(name, ChunkName.Length, throwOnEndOfStream: false) != ChunkName.Length)
        {
            throw new InvalidDataException("Incorrect length for chunk name.");
        }
        return name;
    }
    #endregion
}
