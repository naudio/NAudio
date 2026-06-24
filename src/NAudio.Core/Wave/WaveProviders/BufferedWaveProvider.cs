using System;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Provides a buffered store of samples.
    /// Read method will return queued samples or fill buffer with zeroes.
    /// Backed by a circular buffer.
    /// </summary>
    public class BufferedWaveProvider : IWaveProvider
    {
        private readonly CircularBuffer circularBuffer;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        /// <param name="bufferDuration">Buffer duration (defaults to 5 seconds)</param>
        public BufferedWaveProvider(WaveFormat waveFormat, TimeSpan? bufferDuration = null)
        {
            this.waveFormat = waveFormat;
            var bufferLength = (int)((bufferDuration?.TotalSeconds ?? 5.0) * waveFormat.AverageBytesPerSecond);
            circularBuffer = new CircularBuffer(bufferLength);
            ReadFully = true;
        }

        /// <summary>
        /// If true, always read the amount of data requested, padding with zeroes if necessary.
        /// By default is set to true.
        /// </summary>
        public bool ReadFully { get; set; }

        /// <summary>
        /// Buffer length in bytes
        /// </summary>
        public int BufferLength => circularBuffer.MaxLength;

        /// <summary>
        /// Buffer duration
        /// </summary>
        public TimeSpan BufferDuration =>
            TimeSpan.FromSeconds((double)circularBuffer.MaxLength / WaveFormat.AverageBytesPerSecond);

        /// <summary>
        /// If true, when the buffer is full, start throwing away data.
        /// If false, AddSamples will throw an exception when buffer is full.
        /// </summary>
        public bool DiscardOnBufferOverflow { get; set; }

        /// <summary>
        /// The number of buffered bytes
        /// </summary>
        public int BufferedBytes => circularBuffer.Count;

        /// <summary>
        /// Buffered Duration
        /// </summary>
        public TimeSpan BufferedDuration =>
            TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond);

        /// <summary>
        /// Gets the WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Adds samples from a span. Takes a copy of the data.
        /// </summary>
        public void AddSamples(ReadOnlySpan<byte> data)
        {
            var written = circularBuffer.Write(data);
            if (written < data.Length && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
        }

        /// <summary>
        /// Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary.
        /// </summary>
        public void AddSamples(byte[] buffer, int offset, int count)
        {
            AddSamples(buffer.AsSpan(offset, count));
        }

        /// <summary>
        /// Reads from this audio source.
        /// Will always return the full span if ReadFully is set, zero-filling if not enough data available.
        /// </summary>
        public int Read(Span<byte> buffer)
        {
            int read = circularBuffer.Read(buffer);
            if (ReadFully && read < buffer.Length)
            {
                buffer.Slice(read).Clear();
                read = buffer.Length;
            }
            return read;
        }

        /// <summary>
        /// Discards all audio from the buffer
        /// </summary>
        public void ClearBuffer()
        {
            circularBuffer.Reset();
        }
    }
}
