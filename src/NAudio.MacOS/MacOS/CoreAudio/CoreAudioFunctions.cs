
using System;
using System.Runtime.CompilerServices;
using NAudio.MacOS.CoreAudio.Interop;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Exposes to the public some native functions, appropriately wrapped.
/// </summary>
public static class CoreAudioFunctions
{
    /// <summary>Gets the current host time.</summary>
    /// <returns>A <see cref="double"/> containing the current host time.</returns>
    public static ulong CurrentHostTime => NativeMethods.AudioGetCurrentHostTime();

    /// <summary>Gets the number of ticks per second in the host time base.</summary>
    /// <returns>A <see cref="double"/> containing the number of ticks per second in the host time base.</returns>
    public static double HostClockFrequency => NativeMethods.AudioGetHostClockFrequency();

    /// <summary>Gets the smallest number of ticks that two succeeding host time values will ever differ by.</summary>
    /// <returns>A <see cref="uint"/> containing the smallest number of ticks that two succeeding values will ever differ.</returns>
    public static uint HostClockMinimumTimeDelta => NativeMethods.AudioGetHostClockMinimumTimeDelta();

    /// <summary>
    /// Given a specified host time, it converts the time to an equivalent <see cref="TimeSpan"/> instance.
    /// </summary>
    /// <param name="hostTime">The host time to convert.</param>
    /// <returns>The converted <see cref="TimeSpan"/>, whose value derived from the specified <paramref name="hostTime"/> parameter value.</returns>
    public static TimeSpan ConvertHostTimeToTimeSpan(ulong hostTime) => TimeSpan.FromSeconds(ConvertHostTimeToSeconds(hostTime));

    /// <summary>
    /// Given a specified host time, it converts the time to an equivalent value in seconds.
    /// </summary>
    /// <param name="hostTime">The host time to convert.</param>
    /// <returns>The converted host time value in seconds.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConvertHostTimeToSeconds(ulong hostTime) => hostTime / HostClockFrequency;

    /// <summary>
    /// Gets the current time of the specified audio device, if running.
    /// </summary>
    /// <param name="device">The audio device to get it's time.</param>
    /// <param name="scope">The the audio device time to query for (Input/Output).</param>
    /// <returns>The current time of the audio device, in seconds</returns>
    /// <exception cref="InvalidOperationException">The specified audio device does not have at least one I/O procedure running.</exception>
    public static double GetCurrentDeviceTime(AudioDevice device, AudioObjectPropertyScope scope)
    {
        ArgumentNullException.ThrowIfNull(device);
        AudioTimeStamp stamp = new();
        // If possible, query only the host time.
        stamp.mFlags |= AudioTimeStampFlags.kAudioTimeStampHostTimeValid;
        CoreAudioException.ThrowIfError(
            NativeMethods.AudioDeviceGetCurrentTime(device.objectId, ref stamp)
        );
        if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid))
        {
            // We're lucky - the host time is supported.
            return ConvertHostTimeToSeconds(stamp.mHostTime);
        }
        else
        {
            // We're not lucky - we need to get the device's physical format.
            return MacUtils.TimeStampToSeconds(stamp, device.GetStreams(scope)[0].PhysicalFormat);
        }
    }

    /// <summary>
    /// Gets the current time of the specified audio device, if running.
    /// </summary>
    /// <param name="device">The audio device to get it's time.</param>
    /// <param name="intermediateFormat">The currently used virtual format of the audio device.</param>
    /// <returns>The current time of the audio device, in seconds</returns>
    /// <exception cref="InvalidOperationException">The specified audio device does not have at least one I/O procedure running.</exception>
    public static double GetCurrentDeviceTime(AudioDevice device, WaveFormat intermediateFormat)
    {
        ArgumentNullException.ThrowIfNull(device);
        AudioTimeStamp stamp = new();
        // If possible, query only the sample time.
        stamp.mFlags |= AudioTimeStampFlags.kAudioTimeStampSampleTimeValid;
        CoreAudioException.ThrowIfError(
            NativeMethods.AudioDeviceGetCurrentTime(device.objectId, ref stamp)
        );
        return MacUtils.TimeStampToSeconds(stamp, intermediateFormat);
    }
}