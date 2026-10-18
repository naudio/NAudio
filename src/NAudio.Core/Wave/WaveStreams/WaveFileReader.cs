using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace NAudio.Wave;

/// <summary>This class supports the reading of WAV files,
/// providing a repositionable WaveStream that returns the raw data
/// contained in the WAV file
/// </summary>
public class WaveFileReader : WaveStream
{
    private readonly WaveFormat waveFormat;
    private bool disposed = false;
    private readonly bool ownInput;
    private readonly long dataPosition;
    private readonly long dataChunkLength;
    private readonly Lock lockObject = new();
    private readonly Stream waveStream;

    /// <summary>Supports opening a WAV file</summary>
    /// <remarks>The WAV file format is a real mess, but we will only
    /// support the basic WAV file format which actually covers the vast
    /// majority of WAV files out there. For more WAV file format information
    /// visit www.wotsit.org. If you have a WAV file that can't be read by
    /// this class, email it to the NAudio project and we will probably
    /// fix this reader to support it
    /// </remarks>
    public WaveFileReader(string waveFile) :
        this(File.OpenRead(waveFile), true)
    {
    }

    /// <summary>
    /// Creates a Wave File Reader based on an input stream
    /// </summary>
    /// <param name="inputStream">The input stream containing a WAV file including header</param>
    public WaveFileReader(Stream inputStream) :
       this(inputStream, false)
    {
    }

    private WaveFileReader(Stream inputStream, bool ownInput)
    {
        this.waveStream = inputStream;
        try
        {
            var chunkReader = WaveFileChunkReader.Create(inputStream);
            waveFormat = chunkReader.WaveFormat;
            if (waveFormat.BlockAlign <= 0)
            {
                throw new InvalidDataException(
                    $"Invalid WAV file - block align is {waveFormat.BlockAlign}.");
            }
            dataPosition = chunkReader.DataChunkPosition;
            dataChunkLength = chunkReader.DataChunkLength;
            Chunks = new WaveChunks(inputStream, chunkReader.RiffChunks);
        }
        catch
        {
            if (ownInput)
            {
                inputStream.Dispose();
            }

            throw;
        }

        Position = 0;
        this.ownInput = ownInput;
    }

    /// <summary>
    /// The non-essential RIFF chunks found in this file (everything except <c>fmt</c> and <c>data</c>).
    /// Use the returned <see cref="WaveChunks"/> to enumerate chunk metadata, fetch raw bytes via
    /// <see cref="WaveChunks.GetData"/>, or run an <see cref="IWaveChunkInterpreter{T}"/> —
    /// the built-in interpreters for cue lists, BWF, and LIST/INFO metadata are exposed as
    /// extension methods in <see cref="WaveChunksExtensions"/>.
    /// </summary>
    public WaveChunks Chunks { get; }

    /// <summary>
    /// Cleans up the resources associated with this WaveFileReader
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
            System.Diagnostics.Debug.Assert(false, "WaveFileReader was not disposed");
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
    /// This is the length of audio data contained in this WAV file, in bytes
    /// (i.e. the byte length of the data chunk, not the length of the WAV file itself)
    /// <see cref="WaveStream.WaveFormat"/>
    /// </summary>
    public override long Length => dataChunkLength;

    /// <summary>
    /// Number of Sample Frames  (if possible to calculate)
    /// This currently does not take into account number of channels
    /// Multiply number of channels if you want the total number of samples
    /// </summary>
    public long SampleCount
    {
        get
        {
            return waveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.Extensible or WaveFormatEncoding.IeeeFloat
                ? dataChunkLength / BlockAlign
                : throw new InvalidOperationException("Sample count is calculated only for the standard encodings.");
        }
    }

    /// <summary>
    /// Position in the WAV data chunk.
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
    /// Reads bytes from the Wave File into the provided span.
    /// <see cref="Stream.Read(Span{byte})"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The buffer length must be a whole number of sample frames (a multiple of
    /// <see cref="WaveFormat.BlockAlign"/>). The buffer is filled completely until the end of the
    /// data chunk, which gives one short read and then 0, and the count returned is always a whole
    /// number of sample frames.
    /// </para>
    /// <para>
    /// A single <see cref="Stream.Read(Span{byte})"/> on the underlying stream may return fewer bytes
    /// than requested (network, deflate, crypto and pipe streams all do this), so this method keeps
    /// reading from it until the buffer is full or the stream ends. On such a stream a call can
    /// therefore wait for the rest of the requested data rather than returning early. For real-time
    /// playback from a source that may stall, fill a <see cref="BufferedWaveProvider"/> from another
    /// thread and play from that instead.
    /// </para>
    /// </remarks>
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length % waveFormat.BlockAlign != 0)
        {
            throw new ArgumentException(
                $"Must read complete blocks: requested {buffer.Length}, block align is {WaveFormat.BlockAlign}");
        }
        lock (lockObject)
        {
            // sometimes there is more junk at the end of the file past the data chunk
            int count = buffer.Length;
            if (Position + count > dataChunkLength)
            {
                count = (int)(dataChunkLength - Position);
                // dataChunkLength itself may not be a whole number of blocks
                // (truncated/malformed data chunk), so round down to the nearest block.
                count -= count % waveFormat.BlockAlign;
            }
            // A single Read on the source may legitimately return fewer bytes than asked for
            // (network, deflate and crypto streams all do this), which would look like the end
            // of the stream to a consumer and could split a sample frame, so keep asking until
            // the buffer is full or the source runs out.
            int read = waveStream.ReadAtLeast(buffer.Slice(0, count), count, throwOnEndOfStream: false);
            return read - read % waveFormat.BlockAlign;
        }
    }

    /// <summary>
    /// Reads bytes from the Wave File.
    /// <see cref="Stream.Read(byte[], int, int)"/>
    /// </summary>
    public override int Read(byte[] array, int offset, int count)
        => Read(array.AsSpan(offset, count));

    /// <summary>
    /// Attempts to read the next sample or group of samples as floating point normalised into the range -1.0f to 1.0f
    /// </summary>
    /// <returns>An array of samples, 1 for mono, 2 for stereo etc. Null indicates end of file reached
    /// </returns>
    /// <remarks>
    /// Obsolete: use <see cref="WaveExtensionMethods.ToSampleProvider"/> and read into a reusable
    /// <see cref="float"/> buffer instead. That supports every format this reader can produce, including
    /// 8-bit PCM and 32-bit extensible float, and doesn't allocate a new array for every sample frame.
    /// </remarks>
    [Obsolete("Use ToSampleProvider() and read samples into a reusable float buffer instead. ReadNextSampleFrame will be removed in a future major version.")]
    public float[]? ReadNextSampleFrame()
    {
        if (waveFormat.Encoding is not WaveFormatEncoding.Pcm and not WaveFormatEncoding.IeeeFloat and not WaveFormatEncoding.Extensible)
        {
            // RE: Extensible - n.b. not necessarily PCM, should probably write more code to handle this case
            throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
        }

        // Channels and BitsPerSample come from the file header, so guard against a malformed one
        int bytesToRead = waveFormat.Channels * (waveFormat.BitsPerSample / 8);
        if (bytesToRead <= 0)
        {
            throw new InvalidDataException($"Invalid sample frame size ({waveFormat.Channels} channels, {waveFormat.BitsPerSample} bits per sample)");
        }

        var sampleFrame = new float[waveFormat.Channels];
        var frameBytes = new byte[bytesToRead];
        int bytesRead = Read(frameBytes, 0, bytesToRead);
        if (bytesRead == 0)
        {
            return null; // end of file
        }
        if (bytesRead < bytesToRead)
        {
            throw new InvalidDataException("Unexpected end of file");
        }

        TransformSampleFrame(frameBytes, sampleFrame, waveFormat);
        return sampleFrame;
    }

    private static void TransformSampleFrame(ReadOnlySpan<byte> data, Span<float> sampleFrame, WaveFormat waveFormat)
    {
        if (waveFormat.BitsPerSample == 16)
        {
            ReadOnlySpan<short> shorts = MemoryMarshal.Cast<byte, short>(data);
            for (int i = 0; i < sampleFrame.Length; i++)
            {
                sampleFrame[i] = shorts[i] / 32768f;
            }
        }
        else if (waveFormat.BitsPerSample == 24)
        {
            for (int i = 0; i < sampleFrame.Length; i++)
            {
                int o = i * 3;
                sampleFrame[i] = (((sbyte)data[o + 2] << 16) | (data[o + 1] << 8) | data[o]) / 8388608f;
            }
        }
        else if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            ReadOnlySpan<float> floats = MemoryMarshal.Cast<byte, float>(data);
            floats.CopyTo(sampleFrame);
        }
        else if (waveFormat.BitsPerSample == 32)
        {
            ReadOnlySpan<int> ints = MemoryMarshal.Cast<byte, int>(data);
            for (int i = 0; i < sampleFrame.Length; i++)
            {
                sampleFrame[i] = ints[i] / (int.MaxValue + 1f);
            }
        }
        else
        {
            throw new InvalidOperationException("Unsupported bit depth");
        }
    }
}
