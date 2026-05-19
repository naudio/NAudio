using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.SoundFile
{
    /// <summary>
    /// Reads any audio file libsndfile can decode (WAV, AIFF, FLAC,
    /// Ogg/Vorbis, Opus, MP3, …) as a repositionable <see cref="WaveStream"/>.
    /// Audio is exposed as 32-bit IEEE float, so the reader is also an
    /// <see cref="ISampleProvider"/> and feeds NAudio's float pipeline
    /// without an extra conversion stage.
    /// </summary>
    /// <remarks>
    /// Requires a system libsndfile. Which codecs are available depends on
    /// how that build was configured — see <see cref="SoundFileCapabilities"/>.
    /// </remarks>
    public sealed class SoundFileReader : WaveStream, ISampleProvider
    {
        private readonly object lockObject = new();
        private readonly WaveFormat waveFormat;
        private readonly SafeSndFileHandle handle;
        private readonly StreamVirtualIo virtualIo;
        private readonly long lengthBytes;
        private readonly int blockAlign;
        private readonly bool seekable;
        private SfVirtualIo vtable;
        private long positionFrames;
        private bool disposed;

        /// <summary>
        /// Opens an audio file by path.
        /// </summary>
        /// <param name="path">Path to the audio file.</param>
        /// <exception cref="SoundFileException">The file could not be opened or decoded.</exception>
        public SoundFileReader(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            var info = new SfInfo();
            IntPtr raw = SndFileInterop.Open(path, SndFileInterop.SFM_READ, ref info);
            SoundFileException.ThrowIfOpenFailed(raw, "sf_open");
            handle = new SafeSndFileHandle(raw);
            (waveFormat, blockAlign, seekable) = Describe(info);
            lengthBytes = DetermineLength(raw, info, blockAlign, seekable);
            Tags = SoundFileTags.ReadFrom(raw);
        }

        /// <summary>
        /// Opens an audio file from a stream. The stream is <em>not</em>
        /// disposed by the reader (the caller owns it). For non-seekable
        /// streams, formats that need random access to decode may fail.
        /// </summary>
        /// <param name="inputStream">A readable stream positioned at the start of the audio file.</param>
        /// <exception cref="SoundFileException">The stream could not be opened or decoded.</exception>
        /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable.</exception>
        public SoundFileReader(Stream inputStream)
        {
            ArgumentNullException.ThrowIfNull(inputStream);
            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(inputStream));
            }

            virtualIo = new StreamVirtualIo(inputStream);
            vtable = StreamVirtualIo.CreateVtable();
            var info = new SfInfo();
            IntPtr raw = SndFileInterop.OpenVirtual(ref vtable, SndFileInterop.SFM_READ, ref info, virtualIo.UserData);
            if (raw == IntPtr.Zero)
            {
                virtualIo.Dispose();
                SoundFileException.ThrowIfOpenFailed(raw, "sf_open_virtual");
            }
            handle = new SafeSndFileHandle(raw);
            (waveFormat, blockAlign, seekable) = Describe(info);
            lengthBytes = DetermineLength(raw, info, blockAlign, seekable);
            Tags = SoundFileTags.ReadFrom(raw);
        }

        private static (WaveFormat, int, bool) Describe(SfInfo info)
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(info.SampleRate, info.Channels);
            return (format, info.Channels * sizeof(float), info.Seekable != 0);
        }

        // libsndfile reports SF_INFO.frames for most formats, but some
        // streamed Ogg/MP3 inputs report 0/-1. When the source is seekable
        // we can still recover the exact length with a seek-to-end probe.
        private static long DetermineLength(IntPtr h, SfInfo info, int blockAlign, bool seekable)
        {
            if (info.Frames > 0)
            {
                return info.Frames * blockAlign;
            }
            if (seekable)
            {
                long end = SndFileInterop.Seek(h, 0, SndFileInterop.SEEK_END);
                SndFileInterop.Seek(h, 0, SndFileInterop.SEEK_SET);
                if (end > 0)
                {
                    return end * blockAlign;
                }
            }
            return 0;
        }

        /// <summary>
        /// Embedded string metadata (title/artist/album/…), or empty fields
        /// when the format/file carries none.
        /// </summary>
        public SoundFileTags Tags { get; }

        /// <inheritdoc />
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Length of the decoded audio in bytes (32-bit float samples), or 0
        /// when the source is non-seekable and libsndfile cannot report the
        /// frame count up front (some streamed Ogg/MP3). Check
        /// <see cref="CanSeek"/> before relying on this for streamed input.
        /// </summary>
        public override long Length => lengthBytes;

        /// <inheritdoc />
        public override bool CanSeek => seekable;

        /// <summary>
        /// Position in the decoded float stream, in bytes. Setting requires a
        /// seekable source.
        /// </summary>
        /// <exception cref="InvalidOperationException">The source is not seekable.</exception>
        public override long Position
        {
            get { lock (lockObject) { return positionFrames * blockAlign; } }
            set
            {
                if (!seekable)
                {
                    throw new InvalidOperationException("This source is not seekable");
                }
                lock (lockObject)
                {
                    long frame = value / blockAlign;
                    long result = SndFileInterop.Seek(Handle, frame, SndFileInterop.SEEK_SET);
                    if (result < 0)
                    {
                        SoundFileException.ThrowIfError(Handle, "sf_seek");
                    }
                    positionFrames = result;
                }
            }
        }

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            var floats = MemoryMarshal.Cast<byte, float>(buffer);
            int produced = ReadSamples(floats);
            return produced * sizeof(float);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        /// <inheritdoc />
        public int Read(Span<float> buffer) => ReadSamples(buffer);

        private int ReadSamples(Span<float> buffer)
        {
            int channels = waveFormat.Channels;
            long frames = buffer.Length / channels;
            if (frames == 0)
            {
                return 0;
            }
            lock (lockObject)
            {
                IntPtr h = Handle;
                long got = SndFileInterop.ReadFloat(h, buffer, frames);
                // A backing-stream failure is recorded by the vio callback
                // (it cannot throw across the native boundary); surface it
                // before treating a short read as clean EOF.
                virtualIo?.ThrowIfFaulted();
                // sf_readf_* never returns negative: a short/zero count is
                // either clean EOF or a decode error flagged via sf_error.
                if (got < frames)
                {
                    SoundFileException.ThrowIfError(h, "sf_readf_float");
                }
                positionFrames += got;
                return (int)got * channels;
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
                        handle?.Dispose();
                        virtualIo?.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
