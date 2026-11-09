using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using NAudio.MacOS.CoreAudio;

namespace NAudio.Wave;

/// <summary>
/// An audio recorder based on Audio Queue Services. Create it with <see cref="AudioQueueRecorderBuilder"/>.
/// </summary>
/// <remarks>This is an API skeleton: the capture members throw <see cref="NotImplementedException"/> for now.</remarks>
/// <seealso cref="AudioQueueRecorderBuilder"/>
[SupportedOSPlatform("ios2.0")]
[SupportedOSPlatform("macos10.5")]
public sealed class AudioQueueRecorder : IDisposable, IAsyncDisposable, IWaveLatency
{
    private const string NotImplementedMessage = "AudioQueueRecorder is an API skeleton.";

    internal AudioQueueRecorder(int latencyMilliseconds, int numberOfBuffers, AudioDevice? device, bool levelMeteringEnabled)
    {
        LatencyMilliseconds = latencyMilliseconds;
        NumberOfBuffers = numberOfBuffers;
        Device = device;
        LevelMeteringEnabled = levelMeteringEnabled;
    }

    /// <summary>The total amount of audio queued for capture, in milliseconds.</summary>
    public int LatencyMilliseconds { get; }

    /// <summary>The number of buffers that the latency is divided into.</summary>
    public int NumberOfBuffers { get; }

    /// <summary>The device to capture from, or <see langword="null"/> when following the default input device.</summary>
    public AudioDevice? Device { get; }

    /// <summary>Whether level metering is enabled.</summary>
    public bool LevelMeteringEnabled { get; }

    /// <summary>The format of the captured audio.</summary>
    public WaveFormat CaptureFormat => throw new NotImplementedException(NotImplementedMessage);

    /// <summary>The current state of the capture.</summary>
    public CaptureState CaptureState => CaptureState.Stopped;

    /// <inheritdoc />
    public TimeSpan AverageLatency => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public TimeSpan CurrentLatency => throw new NotImplementedException(NotImplementedMessage);

#pragma warning disable CS0067 // Raised once the skeleton is implemented.
    /// <summary>Raised when a buffer of captured audio is available.</summary>
    public event CoreAudioCaptureDataAvailableHandler? DataAvailable;

    /// <summary>Raised when the capture format changes.</summary>
    public event EventHandler? CaptureFormatChanged;

    /// <summary>Raised when the capture stops.</summary>
    public event EventHandler<StoppedEventArgs>? RecordingStopped;
#pragma warning restore CS0067

    /// <summary>Starts capturing.</summary>
    public void StartRecording() => throw new NotImplementedException(NotImplementedMessage);

    /// <summary>Stops capturing.</summary>
    public void StopRecording() => throw new NotImplementedException(NotImplementedMessage);

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
