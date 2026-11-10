
using System;

using NAudio.MacOS.AudioToolbox.Interop;

namespace NAudio.MacOS.AudioToolbox;

/// <summary>
/// Provides information about the level meter values of an initialized Audio Queue.
/// </summary>
public sealed class AudioQueueLevelMeterData
{
    private AudioQueueLevelMeterState[] states;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioQueueLevelMeterData"/>.
    /// </summary>
    public AudioQueueLevelMeterData() => states = [];

    internal void UpdateMeterData(AudioQueueLevelMeterState[]? states) => this.states = states ?? [];

    /// <summary>
    /// Gets the number of channels contained in the current level meter data.
    /// </summary>
    public int ChannelCount => states.Length;

    /// <summary>
    /// Gets the average RMS power for the given <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">The channel to query</param>
    /// <returns>The average power of <paramref name="channel"/>.</returns>
    public float GetAveragePower(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, states.Length);
        return states[channel].mAveragePower;
    }

    /// <summary>
    /// Gets the peak RMS power for the given <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">The channel to query</param>
    /// <returns>The peak power of <paramref name="channel"/>.</returns>
    public float GetPeakPower(int channel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, states.Length);
        return states[channel].mPeakPower;
    }

    /// <summary>
    /// Gets the peak RMS power for the given <paramref name="channel"/>.
    /// </summary>
    /// <param name="channel">The channel to query</param>
    /// <returns>The peak power of <paramref name="channel"/>.</returns>
    public float this[int channel] => GetPeakPower(channel);
}
