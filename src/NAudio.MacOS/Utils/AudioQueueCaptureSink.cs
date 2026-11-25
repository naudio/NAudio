
using System;
using System.Threading.Channels;

using NAudio.Wave;

namespace NAudio.Utils;

/// <summary>
/// Receives the buffers and the end of a capture of a <see cref="AudioQueueRecorder"/> directly from the recorder,
/// without passing through its public events, so that user handlers cannot change what the asynchronous capture sees. <br />
/// Used by <see cref="AudioQueueRecorder.CaptureAsync(System.Threading.CancellationToken)"/>.
/// </summary>
internal sealed class AudioQueueCaptureSink
{
    private readonly Channel<AudioQueueCaptureBuffer> channel;

    public AudioQueueCaptureSink()
    {
        channel = Channel.CreateUnbounded<AudioQueueCaptureBuffer>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = true,
            // The writer is the callback of the queue; never run the reader's continuation on it.
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>The reader side, consumed by the asynchronous capture.</summary>
    public ChannelReader<AudioQueueCaptureBuffer> Reader => channel.Reader;

    /// <summary>
    /// Copies a captured buffer into the sink. Called on the thread of the queue; never throws. <br />
    /// Buffers that arrive after the sink was completed are dropped.
    /// </summary>
    public void Write(ReadOnlySpan<byte> audioData, double currentTimeInSeconds, double firstByteAcquiredFromHALTimeInSeconds)
        => _ = channel.Writer.TryWrite(new(audioData.ToArray(), currentTimeInSeconds, firstByteAcquiredFromHALTimeInSeconds));

    /// <summary>
    /// Marks the capture as finished. The reader drains the buffers it already has,
    /// then ends normally, or throws <paramref name="exception"/> when it is not <see langword="null"/>. <br />
    /// Only the first call has an effect.
    /// </summary>
    public void Complete(Exception? exception) => _ = channel.Writer.TryComplete(exception);
}
