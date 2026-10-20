using System;
using System.Threading;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave;

/// <summary>
/// Provides a buffered store of samples.
/// Read method will return queued samples or fill buffer with zeroes.
/// Backed by a circular buffer.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BufferedWaveProvider"/> sits between a real-time producer (a capture callback,
/// a network thread) calling <see cref="AddSamples(ReadOnlySpan{byte})"/> and a pull-based
/// pipeline calling <see cref="Read"/>. With <see cref="ReadFully"/> set (the default) it never
/// ends: an underrun is padded with silence so playback keeps going. Call
/// <see cref="CompleteAdding"/> when the producer has no more audio, so that <see cref="Read"/>
/// drains the buffer and then ends the stream.
/// </para>
/// <para>
/// <see cref="UnderrunSilenceBytes"/> and <see cref="DiscardedBytes"/> count the glitches the
/// buffer hides. They are counters rather than events because <see cref="Read"/> usually runs
/// on the audio thread; poll them from elsewhere.
/// </para>
/// </remarks>
public class BufferedWaveProvider : IWaveProvider
{
    private readonly CircularBuffer circularBuffer;
    private readonly WaveFormat waveFormat;
    // Serialises AddSamples against CompleteAdding so no write can land after completion.
    // Only producers take it; Read never does.
    private readonly Lock addLock = new();
    private volatile bool addingCompleted;
    private long underrunSilenceBytes;
    private long discardedBytes;

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
    /// Once <see cref="CompleteAdding"/> has been called, reads are no longer padded, so the
    /// stream can end.
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
    /// The incoming data that does not fit is the data thrown away (the buffered audio is kept),
    /// and it is counted in <see cref="DiscardedBytes"/>.
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
    /// Whether <see cref="CompleteAdding"/> has been called. Once true, no more audio can be
    /// added, and <see cref="Read"/> ends the stream when the buffer has been drained.
    /// </summary>
    public bool IsAddingCompleted => addingCompleted;

    /// <summary>
    /// Total bytes of silence <see cref="Read"/> has inserted because <see cref="ReadFully"/> is
    /// set and the buffer held less than was asked for (an underrun), including reads made
    /// before any audio arrived. Reads after <see cref="CompleteAdding"/> are not padded, so
    /// they add nothing. Thread-safe; poll it rather than reading it on the audio thread.
    /// </summary>
    public long UnderrunSilenceBytes => Interlocked.Read(ref underrunSilenceBytes);

    /// <summary>
    /// Total bytes thrown away by <see cref="AddSamples(ReadOnlySpan{byte})"/> because the
    /// buffer was full and <see cref="DiscardOnBufferOverflow"/> is set. Thread-safe.
    /// </summary>
    public long DiscardedBytes => Interlocked.Read(ref discardedBytes);

    /// <summary>
    /// Resets <see cref="UnderrunSilenceBytes"/> and <see cref="DiscardedBytes"/> to zero.
    /// </summary>
    public void ResetCounters()
    {
        Interlocked.Exchange(ref underrunSilenceBytes, 0);
        Interlocked.Exchange(ref discardedBytes, 0);
    }

    /// <summary>
    /// Adds samples from a span. Takes a copy of the data.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The buffer is full and <see cref="DiscardOnBufferOverflow"/> is false, or
    /// <see cref="CompleteAdding"/> has been called.
    /// </exception>
    public void AddSamples(ReadOnlySpan<byte> data)
    {
        int written;
        lock (addLock)
        {
            if (addingCompleted)
            {
                throw new InvalidOperationException("Cannot add samples after CompleteAdding has been called");
            }
            written = circularBuffer.Write(data);
        }
        if (written < data.Length)
        {
            if (!DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
            Interlocked.Add(ref discardedBytes, data.Length - written);
        }
    }

    /// <summary>
    /// Marks the end of the stream: no more audio will be added. <see cref="Read"/> goes on
    /// returning the buffered audio; once the buffer is drained it returns the remaining data
    /// as a short read, not padded with silence even when <see cref="ReadFully"/> is set, and
    /// then 0. This lets playback raise <c>PlaybackStopped</c> and lets
    /// <c>WaveFileWriter.CreateWaveFile</c> finish. Further calls to <c>AddSamples</c> throw
    /// <see cref="InvalidOperationException"/>. Calling it more than once has no further effect.
    /// </summary>
    /// <remarks>
    /// Completion is permanent: <see cref="ClearBuffer"/> does not reset it. To start a new
    /// stream, create a new <see cref="BufferedWaveProvider"/>.
    /// </remarks>
    public void CompleteAdding()
    {
        lock (addLock)
        {
            addingCompleted = true;
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
    /// Will always return the full span if ReadFully is set, zero-filling if not enough data
    /// available and counting the padding in <see cref="UnderrunSilenceBytes"/>.
    /// After <see cref="CompleteAdding"/>, returns the buffered audio unpadded, so the stream
    /// ends with at most one short read and then 0.
    /// </summary>
    public int Read(Span<byte> buffer)
    {
        // Check completion before reading: AddSamples cannot write after completion, so if it
        // is set now the buffer already holds everything that will ever be added.
        bool completed = addingCompleted;
        int read = circularBuffer.Read(buffer);
        if (ReadFully && !completed && read < buffer.Length)
        {
            buffer.Slice(read).Clear();
            Interlocked.Add(ref underrunSilenceBytes, buffer.Length - read);
            read = buffer.Length;
        }
        return read;
    }

    /// <summary>
    /// Discards all audio from the buffer.
    /// This does not reset <see cref="IsAddingCompleted"/>: after <see cref="CompleteAdding"/>,
    /// clearing the buffer ends the stream at the next <see cref="Read"/>. Nor does it reset
    /// the counters; use <see cref="ResetCounters"/> for that.
    /// </summary>
    public void ClearBuffer()
    {
        circularBuffer.Reset();
    }
}
