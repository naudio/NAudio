using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Configures and creates <see cref="AudioQueueRecorder"/> instances.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueueRecorderBuilder
{
    private int latencyMilliseconds = 100;
    private int numberOfBuffers = 4;
    private AudioDevice? device;
    private bool levelMetering;

    /// <summary>Sets the total amount of audio queued for capture. The default is 100 ms.</summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    public AudioQueueRecorderBuilder WithLatency(int latencyMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(latencyMs);
        latencyMilliseconds = latencyMs;
        return this;
    }

    /// <summary>Sets the number of buffers that the latency is divided into. The default is 4.</summary>
    /// <param name="bufferCount">The number of buffers.</param>
    public AudioQueueRecorderBuilder WithNumberOfBuffers(int bufferCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferCount);
        numberOfBuffers = bufferCount;
        return this;
    }

    /// <summary>Captures from the specified device.</summary>
    /// <param name="device">The device to capture from.</param>
    public AudioQueueRecorderBuilder WithDevice(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        this.device = device;
        return this;
    }

    /// <summary>Follows the default input device of the system. This is the default.</summary>
    public AudioQueueRecorderBuilder WithDefaultDeviceStreamRouting()
    {
        device = null;
        return this;
    }

    /// <summary>Enables level metering on the queue.</summary>
    public AudioQueueRecorderBuilder EnableLevelMetering()
    {
        levelMetering = true;
        return this;
    }

    /// <summary>Creates a recorder with the current settings.</summary>
    public AudioQueueRecorder Build() => new(latencyMilliseconds, numberOfBuffers, device, levelMetering);

    /// <summary>Creates a recorder with the current settings without blocking the caller.</summary>
    public Task<AudioQueueRecorder> BuildAsync() => Task.Run(Build);
}
