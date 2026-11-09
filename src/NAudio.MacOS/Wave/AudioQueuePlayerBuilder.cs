using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NAudio.MacOS.AudioToolbox;
using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// Configures and creates <see cref="AudioQueuePlayer"/> instances.
/// </summary>
/// <seealso cref="AudioQueuePlayer"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueuePlayerBuilder
{
    private int latencyMilliseconds = 100;
    private int numberOfBuffers = 4;
    private AudioDevice? device;
    private bool levelMetering;
    private TimePitchAlgorithm timePitchAlgorithm = TimePitchAlgorithm.Default;

    /// <summary>Sets the total amount of audio queued ahead of the device. The default is 100 ms.</summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified value is zero or negative.</exception>
    public AudioQueuePlayerBuilder WithLatency(int latencyMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(latencyMs);
        latencyMilliseconds = latencyMs;
        return this;
    }

    /// <summary>Sets the number of buffers that the latency is divided into. The default is 4.</summary>
    /// <param name="bufferCount">The number of buffers.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified value is zero or negative.</exception>
    public AudioQueuePlayerBuilder WithNumberOfBuffers(int bufferCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferCount);
        numberOfBuffers = bufferCount;
        return this;
    }

    /// <summary>Plays to the specified device.</summary>
    /// <param name="device">The device to play to.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    public AudioQueuePlayerBuilder WithDevice(AudioDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        this.device = device;
        return this;
    }

    /// <summary>Follows the default output device of the system. This is the default.</summary>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    public AudioQueuePlayerBuilder WithDefaultDeviceStreamRouting()
    {
        device = null;
        return this;
    }

    /// <summary>Enables level metering on the queue.</summary>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    public AudioQueuePlayerBuilder EnableLevelMetering()
    {
        levelMetering = true;
        return this;
    }

    /// <summary>Sets the time-pitch algorithm of the queue. When not called, the system default is used.</summary>
    /// <param name="a">The algorithm to use. See <see cref="TimePitchAlgorithmConstants"/>.</param>
    /// <returns>This builder instance, so that calls can be chained.</returns>
    public AudioQueuePlayerBuilder WithTimePitchAlgorithm(TimePitchAlgorithm a)
    {
        timePitchAlgorithm = a;
        return this;
    }

    /// <summary>Creates a player with the current settings.</summary>
    /// <returns>A new <see cref="AudioQueuePlayer"/> instance.</returns>
    public AudioQueuePlayer Build() => new(latencyMilliseconds, numberOfBuffers, device, levelMetering, timePitchAlgorithm);

    /// <summary>Creates a player with the current settings without blocking the caller.</summary>
    /// <returns>A task that provides a new <see cref="AudioQueuePlayer"/> instance.</returns>
    public Task<AudioQueuePlayer> BuildAsync() => Task.Run(Build);
}
