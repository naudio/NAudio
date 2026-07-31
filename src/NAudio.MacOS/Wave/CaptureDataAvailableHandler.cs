
using System;

namespace NAudio.Wave;

/// <summary>
/// Provides an event handler that provides the audio data captured by a CoreAudioCapture instance.
/// </summary>
/// <param name="audioData">The span of bytes containing the capture data.</param>
/// <param name="currentTimeInSeconds">A HAL timestamp, in seconds, that indicate when the HAL invoked the handler.</param>
/// <param name="firstByteAcquiredFromHALTimeInSeconds">A HAL timestamp, in seconds, that indicate when the first byte of the currently provided audio data was retrieved.</param>
public delegate void CoreAudioCaptureDataAvailableHandler(
    ReadOnlySpan<byte> audioData,
    double currentTimeInSeconds,
    double firstByteAcquiredFromHALTimeInSeconds
);

/// <summary>
/// Provides a buffer containing data captured from a <see cref="CoreAudioRecorder"/> instance.
/// </summary>
public sealed class CoreAudioCaptureBuffer
{
    private readonly byte[] data;
    private readonly double currentTimeInSeconds;
    private readonly double firstByteAcquiredFromHALTimeInSeconds;

    internal CoreAudioCaptureBuffer(
        byte[] data,
        double currentTimeInSeconds,
        double firstByteAcquiredFromHALTimeInSeconds
    )
    {
        this.data = data;
        this.currentTimeInSeconds = currentTimeInSeconds;
        this.firstByteAcquiredFromHALTimeInSeconds = firstByteAcquiredFromHALTimeInSeconds;
    }

    /// <summary>
    /// Gets the captured data.
    /// </summary>
    public byte[] Buffer => data;

    /// <summary>
    /// Provides the time in seconds when the HAL started the I/O cycle
    /// associated with this instance.
    /// </summary>
    public double CurrentTimeInSeconds => currentTimeInSeconds;

    /// <summary>
    /// Provides the time in seconds when the HAL collected the first 
    /// byte from the audio device.
    /// </summary>
    public double FirstByteAcquiredFromHALInSeconds => firstByteAcquiredFromHALTimeInSeconds;
}