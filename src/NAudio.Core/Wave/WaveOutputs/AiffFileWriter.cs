using NAudio.Utils;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace NAudio.Wave;

/// <summary>
/// This class writes audio data to a .aif file on disk
/// </summary>
public class AiffFileWriter : Stream
{
    private Stream outStream;
    private readonly BinaryWriter writer;
    private long dataSizePos;
    private long commSampleCountPos;
    private long dataChunkSize = 8;
    private readonly WaveFormat format;
    private readonly string filename;
    private readonly bool ownsStream;

    /// <summary>
    /// Creates an Aiff file by reading all the data from a WaveProvider
    /// BEWARE: the WaveProvider MUST return 0 from its Read method when it is finished,
    /// or the Aiff File will grow indefinitely.
    /// </summary>
    /// <param name="filename">The filename to use</param>
    /// <param name="sourceProvider">The source WaveProvider</param>
    public static void CreateAiffFile(string filename, WaveStream sourceProvider)
    {
        using var writer = new AiffFileWriter(filename, sourceProvider.WaveFormat);
        byte[] buffer = new byte[16384];

        while (sourceProvider.Position < sourceProvider.Length)
        {
            int count = Math.Min((int)(sourceProvider.Length - sourceProvider.Position), buffer.Length);
            int bytesRead = sourceProvider.Read(buffer, 0, count);

            if (bytesRead == 0)
            {
                // end of source provider
                break;
            }

            writer.Write(buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// AiffFileWriter that actually writes to a stream
    /// </summary>
    /// <param name="outStream">Stream to be written to</param>
    /// <param name="format">Wave format to use</param>
    /// <remarks>
    /// The supplied stream is <b>not</b> owned by the writer: disposing the writer finalizes
    /// the AIFF header and flushes the stream, but leaves it open for the caller to dispose.
    /// Use the filename constructor if you want the writer to own and close the underlying file.
    /// </remarks>
    public AiffFileWriter(Stream outStream, WaveFormat format)
        : this(outStream, format, ownsStream: false)
    {
    }

    private AiffFileWriter(Stream outStream, WaveFormat format, bool ownsStream)
    {
        this.outStream = outStream;
        this.ownsStream = ownsStream;
        this.format = format;
        this.writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
        this.writer.Write("FORM"u8);
        this.writer.Write(0); // placeholder
        this.writer.Write("AIFF"u8);

        CreateCommChunk();
        WriteSsndChunkHeader();
    }

    /// <summary>
    /// Creates a new AiffFileWriter
    /// </summary>
    /// <param name="filename">The filename to write to</param>
    /// <param name="format">The Wave Format of the output data</param>
    public AiffFileWriter(string filename, WaveFormat format)
        : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format, ownsStream: true)
    {
        this.filename = filename;
    }

    private void WriteSsndChunkHeader()
    {
        this.writer.Write("SSND"u8);
        dataSizePos = this.outStream.Position;
        this.writer.Write(0);  // placeholder
        this.writer.Write(0);  // zero offset
        this.writer.Write(BinaryPrimitives.ReverseEndianness(format.BlockAlign));
    }

    private void CreateCommChunk()
    {
        this.writer.Write("COMM"u8);
        this.writer.Write(BinaryPrimitives.ReverseEndianness(18));
        this.writer.Write(BinaryPrimitives.ReverseEndianness((short)format.Channels));
        commSampleCountPos = this.outStream.Position;
        this.writer.Write(0);  // placeholder for total number of samples
        this.writer.Write(BinaryPrimitives.ReverseEndianness((short)format.BitsPerSample));
        this.writer.Write(IEEE.ConvertToIeeeExtended(format.SampleRate));
    }

    /// <summary>
    /// The aiff file name or null if not applicable
    /// </summary>
    public string Filename
    {
        get { return filename; }
    }

    /// <summary>
    /// Number of bytes of audio in the data chunk
    /// </summary>
    public override long Length
    {
        get { return dataChunkSize; }
    }

    /// <summary>
    /// WaveFormat of this aiff file
    /// </summary>
    public WaveFormat WaveFormat
    {
        get { return format; }
    }

    /// <summary>
    /// Returns false: Cannot read from a AiffFileWriter
    /// </summary>
    public override bool CanRead
    {
        get { return false; }
    }

    /// <summary>
    /// Returns true: Can write to a AiffFileWriter
    /// </summary>
    public override bool CanWrite
    {
        get { return true; }
    }

    /// <summary>
    /// Returns false: Cannot seek within a AiffFileWriter
    /// </summary>
    public override bool CanSeek
    {
        get { return false; }
    }

    /// <summary>
    /// Read is not supported for a AiffFileWriter
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("Cannot read from an AiffFileWriter");
    }

    /// <summary>
    /// Seek is not supported for a AiffFileWriter
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new InvalidOperationException("Cannot seek within an AiffFileWriter");
    }

    /// <summary>
    /// SetLength is not supported for AiffFileWriter
    /// </summary>
    /// <param name="value"></param>
    public override void SetLength(long value)
    {
        throw new InvalidOperationException("Cannot set length of an AiffFileWriter");
    }

    /// <summary>
    /// Gets the Position in the AiffFile (i.e. number of bytes written so far)
    /// </summary>
    public override long Position
    {
        get { return dataChunkSize; }
        set { throw new InvalidOperationException("Repositioning an AiffFileWriter is not supported"); }
    }

    /// <summary>
    /// Appends bytes to the AiffFile (assumes they are already in the correct format)
    /// </summary>
    /// <param name="data">the buffer containing the wave data</param>
    /// <param name="offset">the offset from which to start writing</param>
    /// <param name="count">the number of bytes to write</param>
    public override void Write(byte[] data, int offset, int count)
    {
        Write(data.AsSpan(offset, count));
    }

    /// <summary>
    /// Appends bytes to the AiffFile (assumes they are already in the correct format)
    /// </summary>
    /// <param name="buffer">the buffer containing the wave data</param>
    [SkipLocalsInit]
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Span<byte> modified = stackalloc byte[512];
        byte[] rented = null;
        if (buffer.Length > 512)
        {
            rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            modified = rented;
        }
        modified = modified[..buffer.Length];

        try
        {
            int bytesPerSample = format.BitsPerSample / 8;
            if (bytesPerSample <= 1)
            {
                // AIFF 8-bit PCM is signed two's-complement, but the incoming bytes are unsigned
                // (WAV-style, like every other path through Write, which converts WAV layout to
                // AIFF layout). Flip the sign bit on the way out so the file is valid signed AIFF.
                // Copy into a scratch buffer so the caller's array is not mutated. See issue #1178.
                for (int i = 0; i < buffer.Length; i++)
                {
                    modified[i] = (byte)(buffer[i] ^ 0x80);
                }
            }
            else
            {
                buffer.CopyTo(modified);

                int completeSampleBytes = buffer.Length - (buffer.Length % bytesPerSample);
                for (int sampleStart = 0; sampleStart < completeSampleBytes; sampleStart += bytesPerSample)
                {
                    modified.Slice(sampleStart, bytesPerSample).Reverse();
                }
            }

            outStream.Write(modified);
            dataChunkSize += buffer.Length;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    /// <summary>
    /// Writes a single sample to the Aiff file
    /// </summary>
    /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
    private static int ConvertFloatTo32BitPcm(float sample)
    {
        return sample switch
        {
            >= 1.0f => int.MaxValue,
            <= -1.0f => int.MinValue,
            _ => (int)(sample * 2147483647.0)
        };
    }

    /// <summary>
    /// Writes a single sample to the AIFF file.
    /// </summary>
    /// <param name="sample">The sample value, between -1.0f and 1.0f.</param>
    public void WriteSample(float sample)
    {
        if (WaveFormat.BitsPerSample == 16)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness((short)(short.MaxValue * sample)));
            dataChunkSize += 2;
        }
        else if (WaveFormat.BitsPerSample == 24)
        {
            Span<byte> bytes = stackalloc byte[4];
            Unsafe.WriteUnaligned(ref bytes[0], ConvertFloatTo32BitPcm(sample));

            Span<byte> value24 = stackalloc byte[3];
            value24[2] = bytes[1];
            value24[1] = bytes[2];
            value24[0] = bytes[3];
            writer.Write(value24);
            dataChunkSize += 3;
        }
        else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness(ConvertFloatTo32BitPcm(sample)));
            dataChunkSize += 4;
        }
        else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            int value = BitConverter.SingleToInt32Bits(sample);
            if (BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            writer.Write(value);
            dataChunkSize += 4;
        }
        else
        {
            throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
        }
    }

    /// <summary>
    /// Writes 32 bit floating point samples to the Aiff file
    /// They will be converted to the appropriate bit depth depending on the WaveFormat of the AIF file
    /// </summary>
    /// <param name="samples">The buffer containing the floating point samples</param>
    /// <param name="offset">The offset from which to start writing</param>
    /// <param name="count">The number of floating point samples to write</param>
    public void WriteSamples(float[] samples, int offset, int count)
    {
        for (int n = 0; n < count; n++)
        {
            WriteSample(samples[offset + n]);
        }
    }

    /// <summary>
    /// Writes 16 bit samples to the Aiff file
    /// </summary>
    /// <param name="samples">The buffer containing the 16 bit samples</param>
    /// <param name="offset">The offset from which to start writing</param>
    /// <param name="count">The number of 16 bit samples to write</param>
    public void WriteSamples(short[] samples, int offset, int count)
    {
        WriteSamples(samples.AsSpan(offset, count));
    }

    /// <summary>
    /// Writes 16 bit samples to the Aiff file
    /// </summary>
    /// <param name="samples">The buffer containing the 16 bit samples</param>
    public void WriteSamples(ReadOnlySpan<short> samples)
    {
        if (WaveFormat.BitsPerSample == 16) // 16 bit PCM data
        {
            for (int i = 0; i < samples.Length; i++)
            {
                writer.Write(BinaryPrimitives.ReverseEndianness(samples[i]));
            }
            dataChunkSize += (samples.Length * 2);
        }
        else if (WaveFormat.BitsPerSample == 24) // 24 bit PCM data
        {
            Span<byte> value24 = stackalloc byte[3];
            for (int i = 0; i < samples.Length; i++)
            {
                int value = samples[i] << 8;
                value24[0] = (byte)((value >> 16) & 0xFF);
                value24[1] = (byte)((value >> 8) & 0xFF);
                value24[2] = (byte)(value & 0xFF);
                writer.Write(value24);
            }
            dataChunkSize += (samples.Length * 3);
        }
        else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible) // 32 bit PCM data
        {
            for (int i = 0; i < samples.Length; i++)
            {
                int value = samples[i] << 16;
                writer.Write(BinaryPrimitives.ReverseEndianness(value));
            }
            dataChunkSize += (samples.Length * 4);
        }
        else
        {
            throw new InvalidOperationException("Only 16, 24 or 32 bit PCM audio data supported");
        }
    }

    /// <summary>
    /// Ensures data is written to disk
    /// </summary>
    public override void Flush()
    {
        writer.Flush();
    }

    #region IDisposable Members

    /// <summary>
    /// Actually performs the close,making sure the header contains the correct data
    /// </summary>
    /// <param name="disposing">True if called from <see>Dispose</see></param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (outStream != null)
            {
                try
                {
                    UpdateHeader(writer);
                }
                finally
                {
                    // in a finally block as we don't want the FileStream to run its disposer in
                    // the GC thread if the code above caused an IOException (e.g. due to disk full)
                    if (ownsStream)
                    {
                        // We opened the file (filename constructor), so we close it.
                        outStream.Dispose(); // will close the underlying base stream
                    }
                    else
                    {
                        // The caller handed us the stream; finalize the file by flushing,
                        // but leave the stream open for them to dispose.
                        outStream.Flush();
                    }
                    outStream = null;
                }
            }
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Updates the header with file size information
    /// </summary>
    protected virtual void UpdateHeader(BinaryWriter writer)
    {
        this.Flush();
        writer.Seek(4, SeekOrigin.Begin);
        writer.Write(BinaryPrimitives.ReverseEndianness((int)(outStream.Length - 8)));
        UpdateCommChunk(writer);
        UpdateSsndChunk(writer);
    }

    private void UpdateCommChunk(BinaryWriter writer)
    {
        writer.Seek((int)commSampleCountPos, SeekOrigin.Begin);
        writer.Write(BinaryPrimitives.ReverseEndianness((int)(dataChunkSize * 8 / format.BitsPerSample / format.Channels)));
    }

    private void UpdateSsndChunk(BinaryWriter writer)
    {
        writer.Seek((int)dataSizePos, SeekOrigin.Begin);
        writer.Write(BinaryPrimitives.ReverseEndianness((int)dataChunkSize));
    }

    /// <summary>
    /// Finaliser - should only be called if the user forgot to close this AiffFileWriter
    /// </summary>
    ~AiffFileWriter()
    {
        System.Diagnostics.Debug.Assert(false, "AiffFileWriter was not disposed");
        Dispose(false);
    }

    #endregion
}
