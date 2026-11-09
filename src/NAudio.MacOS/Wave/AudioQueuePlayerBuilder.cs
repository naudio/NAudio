using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Configures and creates <see cref="AudioQueuePlayer"/> instances.
/// </summary>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueuePlayerBuilder
{
    private int latencyMilliseconds = 100;
    private int numberOfBuffers = 4;
    private AudioDevice? device;
    private bool levelMetering;
    private TimePitchAlgorithm? timePitchAlgorithm;

    /// <summary>Sets the total amount of audio queued ahead of the device. The default is 100 ms.</summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    public AudioQueuePlayerBuilder WithLatency(int latencyMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(latencyMs);
        latencyMilliseconds = latencyMs;
        return this;
    }

    /// <summary>Sets the number of buffers that the latency is divided into. The default is 4.</summary>
    /// <param name="bufferCount">The number of buffers.</param>
    public AudioQueuePlayerBuilder WithNumberOfBuffers(int bufferCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferCount);
        numberOfBuffers = bufferCount;
        return this;
    }

    /// <summary>Plays to the specified device.</summary>
    /// <param name="device">The device to play to.</param>
    public AudioQueuePlayerBuilder WithDevice(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        this.device = device;
        return this;
    }

    /// <summary>Follows the default output device of the system. This is the default.</summary>
    public AudioQueuePlayerBuilder WithDefaultDeviceStreamRouting()
    {
        device = null;
        return this;
    }

    /// <summary>Enables level metering on the queue.</summary>
    public AudioQueuePlayerBuilder EnableLevelMetering()
    {
        levelMetering = true;
        return this;
    }

    /// <summary>Sets the time-pitch algorithm of the queue. When not called, the system default is used.</summary>
    /// <param name="a">The algorithm to use.</param>
    public AudioQueuePlayerBuilder WithTimePitchAlgorithm(TimePitchAlgorithm a)
    {
        timePitchAlgorithm = a;
        return this;
    }

    /// <summary>Creates a player with the current settings.</summary>
    public AudioQueuePlayer Build() => new(latencyMilliseconds, numberOfBuffers, device, levelMetering, timePitchAlgorithm);

    /// <summary>Creates a player with the current settings without blocking the caller.</summary>
    public Task<AudioQueuePlayer> BuildAsync() => Task.Run(Build);
}
