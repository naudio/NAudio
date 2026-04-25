using System;
using NAudio.Wave.Asio;

namespace NAudio.Wave
{
    /// <summary>
    /// Zero-copy writable view of a single ASIO output channel in the driver's native format.
    /// Obtained via <see cref="AsioProcessBuffers.RawOutput"/>.
    /// </summary>
    /// <remarks>
    /// The underlying memory is owned by the ASIO driver and is valid only for the duration of the
    /// buffer-switch callback. Writing through <see cref="Bytes"/> bypasses the library's float-to-native
    /// conversion and the automatic unwritten-output clearing — the caller is responsible for writing
    /// every frame in the buffer.
    /// </remarks>
    public readonly ref struct AsioRawOutputBuffer
    {
        /// <summary>
        /// Raw bytes of the channel, interpreted according to <see cref="Format"/>. Writes land directly in the driver buffer.
        /// </summary>
        public Span<byte> Bytes { get; }

        /// <summary>
        /// Native ASIO sample format expected by the driver for this channel.
        /// </summary>
        public AsioSampleType Format { get; }

        /// <summary>
        /// Number of audio frames represented by <see cref="Bytes"/>.
        /// </summary>
        public int Frames { get; }

        internal AsioRawOutputBuffer(Span<byte> bytes, AsioSampleType format, int frames)
        {
            Bytes = bytes;
            Format = format;
            Frames = frames;
        }
    }
}
