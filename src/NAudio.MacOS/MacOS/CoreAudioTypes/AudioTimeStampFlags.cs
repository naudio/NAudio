// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

#pragma warning disable IDE0055 // We want the flags to have a consistent view.

using System;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioTimeStampFlags <br />
/// The flags that indicate which fields in an AudioTimeStamp structure are valid.
/// </summary>
[Flags]
internal enum AudioTimeStampFlags : uint
{
    kAudioTimeStampNothingValid         = 0,
    /// <summary>The sample frame time is valid.</summary>
    kAudioTimeStampSampleTimeValid      = (1U << 0),
    /// <summary>The host time is valid.</summary>
    kAudioTimeStampHostTimeValid        = (1U << 1),
    /// <summary>The rate scalar is valid.</summary>
    kAudioTimeStampRateScalarValid      = (1U << 2),
    /// <summary>The word clock time is valid.</summary>
    kAudioTimeStampWordClockTimeValid   = (1U << 3),
    /// <summary>The SMPTE time is valid.</summary>
    kAudioTimeStampSMPTETimeValid       = (1U << 4),
    /// <summary>The sample frame time and the host time are valid.</summary>
    kAudioTimeStampSampleHostTimeValid  = (kAudioTimeStampSampleTimeValid | kAudioTimeStampHostTimeValid)
}