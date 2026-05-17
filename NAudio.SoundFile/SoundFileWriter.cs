using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudio.SoundFile
{
    /// <summary>
    /// Encodes audio to any format libsndfile can write (WAV, AIFF, FLAC,
    /// Ogg/Vorbis, Opus, MP3, …). Mirrors <c>WaveFileWriter</c>: it is a
    /// <see cref="Stream"/> you push source-format bytes into, with static
    /// helpers that pump an entire <see cref="IWaveProvider"/>.
    /// </summary>
    /// <remarks>
    /// The input <see cref="WaveFormat"/> must be 16-bit PCM or 32-bit IEEE
    /// float — the two container types NAudio pipelines naturally produce.
    /// libsndfile transcodes that into the chosen output subtype. Which
    /// codecs are available depends on the system libsndfile build — see
    /// <see cref="SoundFileCapabilities"/>.
    /// </remarks>
    public sealed class SoundFileWriter : Stream
    {
        private enum ItemType { Short, Float }

        private readonly object lockObject = new();
        private readonly WaveFormat sourceFormat;
        private readonly ItemType itemType;
        private readonly int channels;
        private readonly SafeSndFileHandle handle;
        private readonly StreamVirtualIo virtualIo;
        private SfVirtualIo vtable;
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
            using (var writer = new SoundFileWriter(new IgnoreDisposeStream(outStream), source.WaveFormat, major, options))
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
        public SoundFileWriter(string path, WaveFormat sourceFormat, SoundFileMajorFormat major, SoundFileWriterOptions options = null)
            : this(path, sourceFormat, BuildFormat(major, options), options)
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
        public SoundFileWriter(string path, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(path);
            (this.sourceFormat, itemType, channels) = Validate(sourceFormat);
            var info = MakeInfo(sourceFormat, rawFormat);
            IntPtr raw = SndFileInterop.Open(path, SndFileInterop.SFM_WRITE, ref info);
            SoundFileException.ThrowIfOpenFailed(raw, "sf_open");
            handle = new SafeSndFileHandle(raw);
            ApplyOptions(options);
        }

        /// <summary>
        /// Creates a writer to a stream in an explicit format. The stream is
        /// <em>not</em> disposed by the writer (the caller owns it). Streamed
        /// formats (FLAC/Ogg/Opus) work on non-seekable streams; WAV/AIFF
        /// need a seekable stream to back-patch their header.
        /// </summary>
        /// <param name="outStream">Destination stream.</param>
        /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
        /// <param name="major">The container / codec to produce.</param>
        /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
        public SoundFileWriter(Stream outStream, WaveFormat sourceFormat, SoundFileMajorFormat major, SoundFileWriterOptions options = null)
            : this(outStream, sourceFormat, BuildFormat(major, options), options)
        {
        }

        /// <summary>
        /// Creates a writer to a stream using a raw libsndfile format
        /// bitfield. Advanced escape hatch.
        /// </summary>
        /// <param name="outStream">Destination stream.</param>
        /// <param name="sourceFormat">Format of the bytes you will write (16-bit PCM or 32-bit IEEE float).</param>
        /// <param name="rawFormat">A libsndfile <c>SF_FORMAT_*</c> bitfield (major | subtype).</param>
        /// <param name="options">Encoder options, or <c>null</c> for defaults.</param>
        public SoundFileWriter(Stream outStream, WaveFormat sourceFormat, int rawFormat, SoundFileWriterOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(outStream);
            if (!outStream.CanWrite)
            {
                throw new ArgumentException("Stream must be writable", nameof(outStream));
            }
            (this.sourceFormat, itemType, channels) = Validate(sourceFormat);

            if (!outStream.CanSeek && SoundFileFormatMap.MajorNeedsSeekableStream(rawFormat))
            {
                throw new ArgumentException(
                    "This format back-patches its header at close and needs a seekable stream. " +
                    "Use a seekable stream, or choose a streamable format (FLAC/Ogg/Opus/MP3).",
                    nameof(outStream));
            }

            bool ownsStream = outStream is IgnoreDisposeStream ids && !ids.IgnoreDispose;
            virtualIo = new StreamVirtualIo(outStream, ownsStream);
            vtable = StreamVirtualIo.CreateVtable();
            var info = MakeInfo(sourceFormat, rawFormat);
            if (SndFileInterop.FormatCheck(ref info) == 0)
            {
                virtualIo.Dispose();
                throw new ArgumentException(
                    $"libsndfile rejected format 0x{rawFormat:X} at {sourceFormat.SampleRate} Hz / {sourceFormat.Channels} ch",
                    nameof(rawFormat));
            }
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

        private static (WaveFormat, ItemType, int) Validate(WaveFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);
            if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                return (format, ItemType.Float, format.Channels);
            }
            if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
            {
                return (format, ItemType.Short, format.Channels);
            }
            throw new NotSupportedException(
                "SoundFileWriter accepts 16-bit PCM or 32-bit IEEE float input. " +
                "Convert with SampleToWaveProvider16 or .ToSampleProvider() first.");
        }

        private static SfInfo MakeInfo(WaveFormat format, int rawFormat) => new()
        {
            SampleRate = format.SampleRate,
            Channels = format.Channels,
            Format = rawFormat
        };

        private void ApplyOptions(SoundFileWriterOptions options)
        {
            if (options == null)
            {
                return;
            }
            IntPtr h = handle.DangerousGetHandle();
            if (options.VbrQuality is { } q)
            {
                double v = q;
                SndFileInterop.CommandSetDouble(h, SndFileInterop.SFC_SET_VBR_ENCODING_QUALITY, ref v, sizeof(double));
            }
            if (options.CompressionLevel is { } c)
            {
                double v = c;
                SndFileInterop.CommandSetDouble(h, SndFileInterop.SFC_SET_COMPRESSION_LEVEL, ref v, sizeof(double));
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

        /// <summary>Number of source bytes written so far.</summary>
        public override long Length => bytesWritten;

        /// <inheritdoc />
        public override long Position
        {
            get => bytesWritten;
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

        /// <inheritdoc />
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            lock (lockObject)
            {
                IntPtr h = Handle;
                long frames;
                long got;
                if (itemType == ItemType.Short)
                {
                    var items = MemoryMarshal.Cast<byte, short>(buffer);
                    frames = items.Length / channels;
                    got = frames == 0 ? 0 : SndFileInterop.WriteShort(h, items, frames);
                }
                else
                {
                    var items = MemoryMarshal.Cast<byte, float>(buffer);
                    frames = items.Length / channels;
                    got = frames == 0 ? 0 : SndFileInterop.WriteFloat(h, items, frames);
                }
                if (got < frames)
                {
                    SoundFileException.ThrowIfError(h, "sf_writef");
                }
                bytesWritten += buffer.Length;
            }
        }

        /// <summary>
        /// Writes 32-bit float samples (interleaved by channel). Works
        /// regardless of the declared input format; libsndfile converts to
        /// the output subtype.
        /// </summary>
        /// <param name="samples">Interleaved float samples.</param>
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
                bytesWritten += samples.Length * sizeof(float);
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
            if (disposing && !disposed)
            {
                disposed = true;
                // sf_close finalises the encoder and (for the stream path)
                // flushes trailing bytes through the vio write callback, so
                // it must run before the GCHandle/stream are released.
                handle?.Dispose();
                virtualIo?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
