
using System;
using System.Runtime.CompilerServices;

using NAudio.Wave;
using NAudio.Utils;
using NAudio.MacOS.CoreAudioTypes;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// Exposes utility methods, some of them are wrapping native functions.
/// </summary>
public static class CoreAudioFunctions
{
    /// <summary>Gets the current host time.</summary>
    /// <returns>A <see cref="ulong"/> containing the current host time.</returns>
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
    /// Queries the current native virtual audio format of the specified <see cref="AudioStream"/> 
    /// and gets a value whether the currently used HAL virtual format is non-interleaved. <br />
    /// What is a non-interleaved audio format? <br />
    /// All the audio data processed and produced by NAudio are grouped as interleaved samples. <br />
    /// An interleaved sample contains all the sampled values for each channel. <br />
    /// A non-interleaved sample contains a single sampled value for a single channel. <br />
    /// This important distinction is existing in HAL and is used in top-notch professional
    /// audio devices and aggregate devices, and as such when writing a custom <see cref="CoreAudioIOProcedure"/>
    /// this is only way to know about this distinction.
    /// </summary>
    /// <param name="stream">The <see cref="AudioStream"/> whose virtual format is to be queried.</param>
    /// <returns>A value whether the current format used by the stream is non-interleaved.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public static bool IsNonInterleavedStream(AudioStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return stream.VirtualFormatNative.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonInterleaved);
    }

    /// <summary>
    /// Queries the current native virtual audio format of the specified <see cref="AudioStream"/> 
    /// and gets a value whether the currently used HAL virtual format is non-mixable.
    /// </summary>
    /// <param name="stream">The <see cref="AudioStream"/> whose virtual format is to be queried.</param>
    /// <returns>A value whether the current format used by the stream is non-mixable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public static bool IsNonMixableStream(AudioStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return stream.VirtualFormatNative.mFormatFlags.HasFlag(AudioFormatFlags.kAudioFormatFlagIsNonMixable);
    }

    /// <summary>
    /// Gets the current time of the specified audio device, if running.
    /// </summary>
    /// <param name="device">The audio device to get it's time.</param>
    /// <param name="scope">The the audio device time to query for (Input/Output).</param>
    /// <returns>The current time of the audio device, in seconds</returns>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="device"/> and/or <paramref name="intermediateFormat"/> are <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The specified audio device does not have at least one I/O procedure running.</exception>
    public static double GetCurrentDeviceTime(AudioDevice device, WaveFormat intermediateFormat)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(intermediateFormat);
        AudioTimeStamp stamp = new();
        // If possible, query only the sample time.
        stamp.mFlags |= AudioTimeStampFlags.kAudioTimeStampSampleTimeValid;
        CoreAudioException.ThrowIfError(
            NativeMethods.AudioDeviceGetCurrentTime(device.objectId, ref stamp)
        );
        return MacUtils.TimeStampToSeconds(stamp, intermediateFormat);
    }
}