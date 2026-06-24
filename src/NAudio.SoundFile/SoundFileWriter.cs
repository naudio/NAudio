using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.SoundFile;

/// <summary>
/// Encodes audio to any format libsndfile can write (WAV, AIFF, FLAC,
/// Ogg/Vorbis, Opus, MP3, …). Mirrors <c>WaveFileWriter</c>: it is a
/// <see cref="Stream"/> you push source-format bytes into, with static
/// helpers that pump an entire <see cref="IWaveProvider"/>.
/// </summary>
/// <remarks>
/// The input <see cref="WaveFormat"/> must be 16-bit PCM or 32-bit IEEE
/// float — the two container types NAudio pipelines naturally produce.
/// libsndfile transcodes that into the chosen output subtype (clipping
/// out-of-range float by default; see <see cref="SoundFileWriterOptions.Clipping"/>).
/// Which codecs are available depends on the system libsndfile build —
/// see <see cref="SoundFileCapabilities"/>.
/// </remarks>
public sealed class SoundFileWriter : Stream
{
    private enum ItemType { Short, Float }

    private readonly object lockObject = new();
    private readonly WaveFormat sourceFormat;
    private readonly ItemType itemType;
    private readonly int channels;
    private readonly int frameBytes;
    private readonly SafeSndFileHandle handle;
    private readonly StreamVirtualIo virtualIo;
    private SfVirtualIo vtable;
    private byte[] carry = [];
    private long bytesWritten;
    private bool disposed;

    // --- Static convenience helpers ----------------------------------

    /// <summary>
    /// Encodes an entire <see cref="IWaveProvider"/> to a file, choosing
    /// the format from the path's extension. The source must end (return
    /// 0 from Read) or the file grows indefinitely.
    /// </summary>
    /// <param name="path">Output path; extension selects the format.</param>
    /// <param name="source">The audio to encode.</param>
    public static void CreateSoundFile(string path, IWaveProvider source)
    {
        ArgumentNullException.ThrowIfNull(path);
        CreateSoundFile(path, source, SoundFileFormatMap.InferMajorFromExtension(path), null);
    }

    /// <summary>
    /// Encodes an entire <see cref="IWaveProvider"/> to a file in an
    /// explicit format. The source must end (return 0 from Read).
    /// </summary>
    /// <param name="path">Output path.</param>
    /// <param name="source">The audio to encode.</param>
    /// <param name="major">The container / codec to produce.</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    public static void CreateSoundFile(string path, IWaveProvider source, SoundFileMajorFormat major, SoundFileWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var writer = new SoundFileWriter(path, source.WaveFormat, major, options);
        Pump(source, writer);
    }

    /// <summary>
    /// Encodes a 16-bit version of an <see cref="ISampleProvider"/> to a
    /// file, choosing the format from the path's extension.
    /// </summary>
    /// <param name="path">Output path; extension selects the format.</param>
    /// <param name="source">The sample source.</param>
    public static void CreateSoundFile16(string path, ISampleProvider source)
    {
        ArgumentNullException.ThrowIfNull(source);
        CreateSoundFile(path, new SampleToWaveProvider16(source));
    }

    /// <summary>
    /// Encodes an entire <see cref="IWaveProvider"/> to a stream. The
    /// stream is left open; the source must end (return 0 from Read).
    /// </summary>
    /// <param name="outStream">Destination stream.</param>
    /// <param name="source">The audio to encode.</param>
    /// <param name="major">The container / codec to produce.</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    public static void WriteSoundFileToStream(Stream outStream, IWaveProvider source, SoundFileMajorFormat major, SoundFileWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        using (var writer = new SoundFileWriter(outStream, source.WaveFormat, major, options))
        {
            Pump(source, writer);
        }
        outStream.Flush();
    }

    private static void Pump(IWaveProvider source, SoundFileWriter writer)
    {
        var buffer = new byte[source.WaveFormat.AverageBytesPerSecond * 4];
        while (true)
        {
            int read = source.Read(buffer.AsSpan());
            if (read == 0)
            {
                break;
            }
            writer.Write(buffer.AsSpan(0, read));
        }
    }

    // --- Constructors -------------------------------------------------

    /// <summary>
    /// Creates a writer to a file, choosing the format from the path's
    /// extension (e.g. <c>.flac</c>, <c>.ogg</c>, <c>.opus</c>, <c>.wav</c>).
    /// </summary>
    /// <param name="path">Output path.</param>
    /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
    /// <exception cref="ArgumentException">The extension is unknown, or libsndfile rejects the format.</exception>
    /// <exception cref="NotSupportedException"><paramref name="sourceFormat"/> is not 16-bit PCM or 32-bit float.</exception>
    public SoundFileWriter(string path, WaveFormat sourceFormat)
        : this(path, sourceFormat, SoundFileFormatMap.InferMajorFromExtension(Guard(path)), null)
    {
    }

    /// <summary>
    /// Creates a writer to a file in an explicit format.
    /// </summary>
    /// <param name="path">Output path.</param>
    /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
    /// <param name="major">The container / codec to produce.</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    /// <exception cref="ArgumentException">libsndfile rejects the format/rate/channel combination.</exception>
    /// <exception cref="NotSupportedException"><paramref name="sourceFormat"/> is not 16-bit PCM or 32-bit float.</exception>
    public SoundFileWriter(string path, WaveFormat sourceFormat, SoundFileMajorFormat major, SoundFileWriterOptions options = null)
        : this(path, sourceFormat, BuildFormat(major, options), options)
    {
    }

    /// <summary>
    /// Creates a writer to a stream in an explicit format. The stream is
    /// <em>not</em> disposed by the writer (the caller owns it). Streamed
    /// formats (FLAC/Ogg/Opus/MP3) work on non-seekable streams; WAV/AIFF
    /// need a seekable stream to back-patch their header.
    /// </summary>
    /// <param name="outStream">Destination stream.</param>
    /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
    /// <param name="major">The container / codec to produce.</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    /// <exception cref="ArgumentException">The stream can't satisfy the format, or libsndfile rejects it.</exception>
    /// <exception cref="NotSupportedException"><paramref name="sourceFormat"/> is not 16-bit PCM or 32-bit float.</exception>
    public SoundFileWriter(Stream outStream, WaveFormat sourceFormat, SoundFileMajorFormat major, SoundFileWriterOptions options = null)
        : this(outStream, sourceFormat, BuildFormat(major, options), options)
    {
    }

    /// <summary>
    /// Creates a writer to a file using a raw libsndfile format bitfield.
    /// Advanced escape hatch for formats this library's enums don't model.
    /// </summary>
    /// <param name="path">Output path.</param>
    /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
    /// <param name="rawFormat">A libsndfile <c>SF_FORMAT_*</c> bitfield (major | subtype).</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    /// <returns>A new writer.</returns>
    public static SoundFileWriter FromRawFormat(string path, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options = null)
        => new(path, sourceFormat, rawFormat, options);

    /// <summary>
    /// Creates a writer to a stream using a raw libsndfile format
    /// bitfield. Advanced escape hatch.
    /// </summary>
    /// <param name="outStream">Destination stream.</param>
    /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
    /// <param name="rawFormat">A libsndfile <c>SF_FORMAT_*</c> bitfield (major | subtype).</param>
    /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
    /// <returns>A new writer.</returns>
    public static SoundFileWriter FromRawFormat(Stream outStream, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options = null)
        => new(outStream, sourceFormat, rawFormat, options);

    private SoundFileWriter(string path, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        (this.sourceFormat, itemType, channels, frameBytes) = Validate(sourceFormat);
        var info = MakeValidatedInfo(sourceFormat, rawFormat);
        IntPtr raw = SndFileInterop.Open(path, SndFileInterop.SFM_WRITE, ref info);
        SoundFileException.ThrowIfOpenFailed(raw, "sf_open");
        handle = new SafeSndFileHandle(raw);
        ApplyOptions(options);
    }

    private SoundFileWriter(Stream outStream, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(outStream);
        if (!outStream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable", nameof(outStream));
        }
        (this.sourceFormat, itemType, channels, frameBytes) = Validate(sourceFormat);

        if (!outStream.CanSeek && SoundFileFormatMap.MajorNeedsSeekableStream(rawFormat))
        {
            throw new ArgumentException(
                "This format back-patches its header at close and needs a seekable stream. " +
                "Use a seekable stream, or choose a streamable format (FLAC/Ogg/Opus/MP3).",
                nameof(outStream));
        }

        virtualIo = new StreamVirtualIo(outStream);
        vtable = StreamVirtualIo.CreateVtable();
        var info = MakeValidatedInfo(sourceFormat, rawFormat);
        IntPtr raw = SndFileInterop.OpenVirtual(ref vtable, SndFileInterop.SFM_WRITE, ref info, virtualIo.UserData);
        if (raw == IntPtr.Zero)
        {
            virtualIo.Dispose();
            SoundFileException.ThrowIfOpenFailed(raw, "sf_open_virtual");
        }
        handle = new SafeSndFileHandle(raw);
        ApplyOptions(options);
    }

    private static string Guard(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path;
    }

    private static int BuildFormat(SoundFileMajorFormat major, SoundFileWriterOptions options)
        => SoundFileFormatMap.BuildFormat(major, options?.Subtype ?? SoundFileSubtype.Default);

    private static (WaveFormat, ItemType, int, int) Validate(WaveFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            return (format, ItemType.Float, format.Channels, format.Channels * sizeof(float));
        }
        if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            return (format, ItemType.Short, format.Channels, format.Channels * sizeof(short));
        }
        throw new NotSupportedException(
            "SoundFileWriter accepts 16-bit PCM or 32-bit IEEE float input. " +
            "Convert with SampleToWaveProvider16 or .ToSampleProvider() first.");
    }

    private static SfInfo MakeValidatedInfo(WaveFormat format, int rawFormat)
    {
        if (SoundFileFormatMap.IsOpus(rawFormat) &&
            format.SampleRate is not (8000 or 12000 or 16000 or 24000 or 48000))
        {
            throw new ArgumentException(
                $"Opus only supports 8/12/16/24/48 kHz; got {format.SampleRate} Hz. " +
                "Resample before encoding.",
                nameof(format));
        }

        var info = new SfInfo
        {
            SampleRate = format.SampleRate,
            Channels = format.Channels,
            Format = rawFormat
        };
        if (SndFileInterop.FormatCheck(ref info) == 0)
        {
            throw new ArgumentException(
                $"libsndfile rejected format 0x{rawFormat:X} at {format.SampleRate} Hz / {format.Channels} ch " +
                "(unsupported combination or codec not built into this libsndfile)",
                nameof(rawFormat));
        }
        return info;
    }

    private void ApplyOptions(SoundFileWriterOptions options)
    {
        IntPtr h = handle.DangerousGetHandle();

        // Clip out-of-range float (default on); without it libsndfile
        // wraps a >1.0 sample into loud distortion.
        bool clip = options?.Clipping ?? true;
        SndFileInterop.Command(h, SndFileInterop.SFC_SET_CLIPPING,
            IntPtr.Zero, clip ? SndFileInterop.SF_TRUE : 0);

        if (options == null)
        {
            return;
        }

        if (options.VbrQuality is { } q)
        {
            double v = q;
            if (SndFileInterop.CommandSetDouble(h, SndFileInterop.SFC_SET_VBR_ENCODING_QUALITY, ref v, sizeof(double)) != SndFileInterop.SF_TRUE)
            {
                throw new SoundFileException(
                    $"libsndfile rejected VBR quality {q} for this codec", 0);
            }
        }
        if (options.CompressionLevel is { } c)
        {
            double v = c;
            if (SndFileInterop.CommandSetDouble(h, SndFileInterop.SFC_SET_COMPRESSION_LEVEL, ref v, sizeof(double)) != SndFileInterop.SF_TRUE)
            {
                throw new SoundFileException(
                    $"libsndfile rejected compression level {c} for this codec", 0);
            }
        }
        if (options.Tags is { IsEmpty: false } tags)
        {
            foreach (var (selector, value) in tags.NonNull())
            {
                // Codec may not support a given field; that is not fatal.
                SndFileInterop.SetString(h, selector, value);
            }
        }
    }

    /// <summary>The input format the writer was constructed with.</summary>
    public WaveFormat WaveFormat => sourceFormat;

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <summary>Number of source bytes accepted so far.</summary>
    public override long Length => bytesWritten;

    /// <inheritdoc />
    public override long Position
    {
        get { lock (lockObject) { return bytesWritten; } }
        set => throw new InvalidOperationException("Cannot reposition a SoundFileWriter");
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => throw new InvalidOperationException("Cannot read from a SoundFileWriter");

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
        => throw new InvalidOperationException("Cannot seek within a SoundFileWriter");

    /// <inheritdoc />
    public override void SetLength(long value)
        => throw new InvalidOperationException("Cannot set length of a SoundFileWriter");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => Write(buffer.AsSpan(offset, count));

    /// <summary>
    /// Appends source-format bytes. Sub-frame remainders are carried over
    /// between calls, so callers may write arbitrary chunk sizes.
    /// </summary>
    /// <param name="buffer">Bytes in the constructor's input format.</param>
    /// <exception cref="SoundFileException">The encoder failed.</exception>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        lock (lockObject)
        {
            IntPtr h = Handle;

            // Prepend any sub-frame remainder from the previous call.
            if (carry.Length > 0)
            {
                var joined = new byte[carry.Length + buffer.Length];
                carry.CopyTo(joined, 0);
                buffer.CopyTo(joined.AsSpan(carry.Length));
                WriteFrames(h, joined);
            }
            else
            {
                WriteFrames(h, buffer);
            }
            bytesWritten += buffer.Length;
        }
    }

    // Writes the whole-frame prefix of data; stashes the (< one frame)
    // tail in 'carry' for the next call. Caller holds lockObject.
    private void WriteFrames(IntPtr h, ReadOnlySpan<byte> data)
    {
        int whole = data.Length - data.Length % frameBytes;
        int remainder = data.Length - whole;

        if (whole > 0)
        {
            long frames = whole / frameBytes;
            long got = itemType == ItemType.Short
                ? SndFileInterop.WriteShort(h, MemoryMarshal.Cast<byte, short>(data[..whole]), frames)
                : SndFileInterop.WriteFloat(h, MemoryMarshal.Cast<byte, float>(data[..whole]), frames);
            virtualIo?.ThrowIfFaulted();
            if (got < frames)
            {
                SoundFileException.ThrowIfError(h, "sf_writef");
            }
        }

        carry = remainder == 0 ? [] : data[whole..].ToArray();
    }

    /// <summary>
    /// Writes 32-bit float samples (interleaved by channel). Works
    /// regardless of the declared input format; libsndfile converts to
    /// the output subtype. Sample count should be a multiple of the
    /// channel count.
    /// </summary>
    /// <param name="samples">Interleaved float samples.</param>
    /// <exception cref="SoundFileException">The encoder failed.</exception>
    public void WriteSamples(ReadOnlySpan<float> samples)
    {
        lock (lockObject)
        {
            IntPtr h = Handle;
            long frames = samples.Length / channels;
            if (frames == 0)
            {
                return;
            }
            long got = SndFileInterop.WriteFloat(h, samples, frames);
            if (got < frames)
            {
                SoundFileException.ThrowIfError(h, "sf_writef_float");
            }
            bytesWritten += frames * channels * sizeof(float);
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        lock (lockObject)
        {
            if (!disposed)
            {
                SndFileInterop.WriteSync(handle.DangerousGetHandle());
                virtualIo?.ThrowIfFaulted();
            }
        }
    }

    private IntPtr Handle
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return handle.DangerousGetHandle();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (lockObject)
            {
                if (!disposed)
                {
                    disposed = true;
                    // sf_close finalises the encoder and (for the stream
                    // path) flushes trailing bytes through the vio write
                    // callback, so it must run before the GCHandle is
                    // released. A flush failure at close (e.g. disk full)
                    // is surfaced rather than silently truncating output.
                    try
                    {
                        handle?.Dispose();
                        virtualIo?.ThrowIfFaulted();
                    }
                    finally
                    {
                        virtualIo?.Dispose();
                    }
                }
            }
        }
        base.Dispose(disposing);
    }
}
