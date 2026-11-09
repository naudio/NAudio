using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// An audio player based on Audio Queue Services. Create it with <see cref="AudioQueuePlayerBuilder"/>.
/// </summary>
/// <remarks>This is an API skeleton: the playback members are not implemented yet.</remarks>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueuePlayer : IWavePlayer, IWaveLatency, IWavePosition, IAsyncDisposable
{
    private const string NotImplementedMessage = "AudioQueuePlayer is an API skeleton.";

    internal AudioQueuePlayer(int latencyMilliseconds, int numberOfBuffers, AudioDevice? device, bool levelMeteringEnabled, TimePitchAlgorithm? timePitchAlgorithm)
    {
        LatencyMilliseconds = latencyMilliseconds;
        NumberOfBuffers = numberOfBuffers;
        Device = device;
        LevelMeteringEnabled = levelMeteringEnabled;
        TimePitchAlgorithm = timePitchAlgorithm;
    }

    /// <summary>The total amount of audio queued ahead of the device, in milliseconds.</summary>
    public int LatencyMilliseconds { get; }

    /// <summary>The number of buffers that the latency is divided into.</summary>
    public int NumberOfBuffers { get; }

    /// <summary>The device to play to, or <see langword="null"/> when following the default output device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>Whether level metering is enabled.</summary>
    public bool LevelMeteringEnabled { get; }

    /// <summary>The time-pitch algorithm, or <see langword="null"/> for the system default.</summary>
    public TimePitchAlgorithm? TimePitchAlgorithm { get; }

    /// <inheritdoc />
    public float Volume
    {
        get => throw new NotImplementedException(NotImplementedMessage);
        set => throw new NotImplementedException(NotImplementedMessage);
    }

    /// <inheritdoc />
    public PlaybackState PlaybackState => PlaybackState.Stopped;

    /// <inheritdoc />
    public WaveFormat OutputWaveFormat => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public TimeSpan AverageLatency => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public TimeSpan CurrentLatency => throw new NotImplementedException(NotImplementedMessage);

#pragma warning disable CS0067 // Raised once the skeleton is implemented.
    /// <inheritdoc />
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;
#pragma warning restore CS0067

    /// <inheritdoc />
    public void Init(IWaveProvider waveProvider) => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public void Play() => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public void Pause() => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public void Stop() => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public long GetPosition() => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
