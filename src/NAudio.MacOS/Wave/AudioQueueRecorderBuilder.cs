
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;

using NAudio.Utils;
using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Configures and creates <see cref="AudioQueueRecorder"/> instances.
/// </summary>
/// <seealso cref="AudioQueueRecorder"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueueRecorderBuilder
{
    private int bufferLengthMilliseconds = 100;
    private int? numberOfBuffers;
    private AudioDevice? device;

    /// <summary>Sets the total amount of audio queued for capture for each buffer. The default is 100 ms for 4 buffers.</summary>
    /// <param name="lengthMs">The per-buffer length in milliseconds.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified value is zero or negative.</exception>
    public AudioQueueRecorderBuilder WithBufferLength(int lengthMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lengthMs);
        bufferLengthMilliseconds = lengthMs;
        return this;
    }

    /// <summary>Sets the number of buffers that the latency is divided into. The default is 4.</summary>
    /// <param name="bufferCount">The number of buffers.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified value is zero or negative.</exception>
    public AudioQueueRecorderBuilder WithNumberOfBuffers(int bufferCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferCount);
        numberOfBuffers = bufferCount;
        return this;
    }

    /// <summary>Captures from the specified device.</summary>
    /// <param name="device">The device to capture from.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public AudioQueueRecorderBuilder WithDevice(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        this.device = device;
        return this;
    }

    /// <summary>Follows the default input device of the system. This is the default.</summary>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    public AudioQueueRecorderBuilder WithDefaultDeviceStreamRouting()
    {
        device = null;
        return this;
    }

    private int NumberOfBuffers => numberOfBuffers ?? MacUtils.GetNumberOfBuffersFromTotalLatency(bufferLengthMilliseconds);

    /// <summary>Creates a recorder with the current settings.</summary>
    /// <returns>A new <see cref="AudioQueueRecorder"/> instance.</returns>
    public AudioQueueRecorder Build() => new(bufferLengthMilliseconds, NumberOfBuffers, device, SynchronizationContext.Current);

    /// <summary>Creates a recorder with the current settings without blocking the caller.</summary>
    /// <returns>A task that provides a new <see cref="AudioQueueRecorder"/> instance.</returns>
    public Task<AudioQueueRecorder> BuildAsync()
    {
        // The events are raised on the context of the caller, which the thread pool does not have.
        var context = SynchronizationContext.Current;
        return Task.Run(() => new AudioQueueRecorder(bufferLengthMilliseconds, NumberOfBuffers, device, context));
    }
}
