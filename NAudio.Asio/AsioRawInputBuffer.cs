using System;
using NAudio.Wave.Asio;

namespace NAudio.Wave
{
    /// <summary>
    /// Zero-copy read-only view of a single ASIO input channel in the driver's native format.
    /// Obtained via <see cref="AsioProcessBuffers.RawInput"/> or <see cref="AsioAudioCapturedEventArgs.RawInput"/>.
    /// </summary>
    /// <remarks>
    /// The underlying memory is owned by the ASIO driver and is valid only for the duration of the
    /// buffer-switch callback that produced this value. Do not store this struct, pass it to another
    /// thread, or access it after the callback / event handler returns.
    /// </remarks>
    public readonly ref struct AsioRawInputBuffer
    {
        /// <summary>
        /// Raw bytes of the channel, interpreted according to <see cref="Format"/>.
        /// </summary>
        public ReadOnlySpan<byte> Bytes { get; }

        /// <summary>
        /// Native ASIO sample format of the bytes in this buffer.
        /// </summary>
        public AsioSampleType Format { get; }

        /// <summary>
        /// Number of audio frames represented by <see cref="Bytes"/>.
        /// </summary>
        public int Frames { get; }

        internal AsioRawInputBuffer(ReadOnlySpan<byte> bytes, AsioSampleType format, int frames)
        {
            Bytes = bytes;
            Format = format;
            Frames = frames;
        }
    }
}
