
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NAudio.Wave;
using NAudio.MacOS.CoreAudioTypes;

namespace NAudio.Utils;

/// <summary>
/// Utilities relating to macOS API's. <br />
/// Not meant to be used outside of this assembly.
/// </summary>
internal static partial class MacUtils
{
    /// <summary>
    /// Constants in macOS can be sneaky; <br />
    /// they are defined as strings but they are differently translated in little and big endian architectures. <br />
    /// This method strives to have support for both of them.
    /// </summary>
    /// <remarks>This method should only be used in <see langword="static"/> <see langword="readonly"/> constants.</remarks>
    /// <param name="str">The 4 character code to convert to an integer value.</param>
    /// <returns>The converted integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length <paramref name="str"/> is not 4 characters.</exception>
    public static int ConstructIntConstantValueFromString(string str)
    {
        int intLength = sizeof(int);
        if (str is null)
        {
            throw new ArgumentNullException(nameof(str), "String cannot be null.");
        }
        else if (str.Length != intLength)
        {
            throw new ArgumentOutOfRangeException(nameof(str), str, $"String cannot be less or more than {intLength} bytes");
        }
        else
        {
            Span<byte> dest = stackalloc byte[intLength];
            // Copy the string contents to the span, converting the characters to bytes.
            for (int I = 0; I < intLength; I++) { dest[I] = unchecked((byte)str[I]); }
            // Reverse the characters if we are on little-endian
            if (BitConverter.IsLittleEndian) { dest.Reverse(); }
            // GetPinnableReference is faster than ref dest[0]. 
            // If deemed necessary in the future however, this should be changed.
            return Unsafe.ReadUnaligned<int>(ref dest.GetPinnableReference());
        }
    }

    /// <summary>
    /// Constants in macOS can be sneaky; <br />
    /// they are defined as strings but they are differently translated in little and big endian architectures. <br />
    /// This method strives to have support for both of them.
    /// </summary>
    /// <remarks>This method should only be used in <see langword="static"/> <see langword="readonly"/> constants.</remarks>
    /// <param name="str">The 4 character code to convert to an unsigned integer value.</param>
    /// <returns>The converted unsigned integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length <paramref name="str"/> is not 4 characters.</exception>
    public static uint ConstructUIntConstantValueFromString(string str)
    {
        int uintLength = sizeof(uint);
        if (str is null)
        {
            throw new ArgumentNullException(nameof(str), "String cannot be null.");
        }
        else if (str.Length != uintLength)
        {
            throw new ArgumentOutOfRangeException(nameof(str), str, $"String cannot be less or more than {uintLength} bytes");
        }
        else
        {
            Span<byte> dest = stackalloc byte[uintLength];
            // Copy the string contents to the span, converting the characters to bytes.
            for (int I = 0; I < uintLength; I++) { dest[I] = unchecked((byte)str[I]); }
            // Reverse the characters if we are on little-endian
            if (BitConverter.IsLittleEndian) { dest.Reverse(); }
            // GetPinnableReference is faster than ref dest[0]. 
            // If deemed necessary in the future however, this should be changed.
            return Unsafe.ReadUnaligned<uint>(ref dest.GetPinnableReference());
        }
    }

    private static double ConvertFramesToSeconds(SMPTETimeType type, short frames) => frames / (type switch
    {
        SMPTETimeType.kSMPTETimeType24 => 24d,
        SMPTETimeType.kSMPTETimeType25 => 25d,
        SMPTETimeType.kSMPTETimeType30 or
        SMPTETimeType.kSMPTETimeType30Drop => 30d,
        SMPTETimeType.kSMPTETimeType2997 or
            SMPTETimeType.kSMPTETimeType2997Drop => 29.97d,
        SMPTETimeType.kSMPTETimeType60 or
            SMPTETimeType.kSMPTETimeType60Drop => 60d,
        SMPTETimeType.kSMPTETimeType5994 or
            SMPTETimeType.kSMPTETimeType5994Drop => 59.94d,
        SMPTETimeType.kSMPTETimeType50 => 50d,
        SMPTETimeType.kSMPTETimeType2398 => 23.98d,
        _ => throw new ArgumentException("Invalid time type: " + type)
    });

    public static double TimeStampToSeconds(AudioTimeStamp stamp, WaveFormat format)
    {
        if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSampleTimeValid))
        {
            return stamp.mSampleTime / format.SampleRate;
        }
        else if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampHostTimeValid))
        {
            return stamp.mHostTime / MacOS.CoreAudio.CoreAudioFunctions.HostClockFrequency;
        }
        else if (stamp.mFlags.HasFlag(AudioTimeStampFlags.kAudioTimeStampSMPTETimeValid))
        {
            var smpteTime = stamp.mSMPTETime;
            return (smpteTime.mHours * TimeSpan.SecondsPerHour) +
                    (smpteTime.mMinutes * TimeSpan.SecondsPerMinute) +
                    smpteTime.mSeconds +
                    ConvertFramesToSeconds(smpteTime.mType, smpteTime.mFrames);
        }
        else
        {
            throw new ArgumentException("Invalid time stamp flags: " + stamp.mFlags);
        }
    }

    // Note: The below three methods are only valid for PCM, because
    // 1 packet = 1 frame (sample). For other VBR or compressed data, it needs different calculations.

    public static int GetNumberOfPacketsFromBytesAndFormat(int bytes, WaveFormat format) => bytes / format.BlockAlign;

    public static uint GetNumberOfPacketsFromBytesAndFormat(uint bytes, WaveFormat format) => (uint)(bytes / format.BlockAlign);

    public static uint GetNumberOfBytesFromPacketsAndFormat(uint numberOfPackets, WaveFormat format) => (uint)(numberOfPackets * format.BlockAlign);

    [StackTraceHidden]
    [DebuggerStepThrough]
    public static void EnsureDisposableObjectsDisposed(params IDisposable[] disposables)
    {
        List<Exception> exceptions = new(10);
        foreach (var i in disposables)
        {
            try
            {
                i?.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }
}