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
            (waveFormat, blockAlign, lengthBytes, seekable) = Describe(info);
        }

        /// <summary>
        /// Opens an audio file from a stream. The stream is <em>not</em>
        /// disposed by the reader (the caller owns it). For non-seekable
        /// streams, formats that need random access to decode may fail.
        /// </summary>
        /// <param name="inputStream">A readable stream positioned at the start of the audio file.</param>
        /// <exception cref="SoundFileException">The stream could not be opened or decoded.</exception>
        public SoundFileReader(Stream inputStream)
        {
            ArgumentNullException.ThrowIfNull(inputStream);
            if (!inputStream.CanRead)
            {
                throw new ArgumentException("Stream must be readable", nameof(inputStream));
            }

            virtualIo = new StreamVirtualIo(inputStream, ownsStream: false);
            vtable = StreamVirtualIo.CreateVtable();
            var info = new SfInfo();
            IntPtr raw = SndFileInterop.OpenVirtual(ref vtable, SndFileInterop.SFM_READ, ref info, virtualIo.UserData);
            if (raw == IntPtr.Zero)
            {
                virtualIo.Dispose();
                SoundFileException.ThrowIfOpenFailed(raw, "sf_open_virtual");
            }
            handle = new SafeSndFileHandle(raw);
            (waveFormat, blockAlign, lengthBytes, seekable) = Describe(info);
        }

        private static (WaveFormat, int, long, bool) Describe(SfInfo info)
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(info.SampleRate, info.Channels);
            int align = info.Channels * sizeof(float);
            long bytes = info.Frames > 0 ? info.Frames * align : 0;
            return (format, align, bytes, info.Seekable != 0);
        }

        /// <inheritdoc />
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Length of the decoded audio in bytes (32-bit float samples), or 0
        /// when libsndfile cannot report the frame count up front.
        /// </summary>
        public override long Length => lengthBytes;

        /// <inheritdoc />
        public override bool CanSeek => seekable;

        /// <summary>
        /// Position in the decoded float stream, in bytes. Setting requires a
        /// seekable source.
        /// </summary>
        public override long Position
        {
            get => positionFrames * blockAlign;
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
                    positionFrames = result < 0 ? frame : result;
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
                long got = SndFileInterop.ReadFloat(Handle, buffer, frames);
                if (got < 0)
                {
                    SoundFileException.ThrowIfError(Handle, "sf_readf_float");
                    return 0;
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
            if (disposing && !disposed)
            {
                disposed = true;
                handle?.Dispose();
                virtualIo?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
