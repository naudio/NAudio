// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioTimeStamp <br />
/// A structure that holds different representations of the same point in time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct AudioTimeStamp
{
    /// <summary>The absolute sample frame time.</summary>
    public double mSampleTime;
    /// <summary>The host machine's time base, mach_absolute_time.</summary>
    public UInt64 mHostTime;
    /// <summary>
    /// The ratio of actual host ticks per sample frame 
    /// to the nominal host ticks per sample frame.
    /// </summary>
    public double mRateScalar;
    /// <summary>The word clock time.</summary>
    public UInt64 mWordClockTime;
    /// <summary>The SMPTE time.</summary>
    public SMPTETime mSMPTETime;
    /// <summary>A set of flags indicating which representations of the time are valid.</summary>
    public AudioTimeStampFlags mFlags;
    /// <summary>Pads the structure out to force an even 8 byte alignment.</summary>
    public UInt32 mReserved;
}