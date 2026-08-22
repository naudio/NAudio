// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

#pragma warning disable IDE0055 // We want the flags to have a consistent view.

using System;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// SMPTETimeFlags <br />
/// Flags that describe the SMPTE time state.
/// </summary>
[Flags]
internal enum SMPTETimeFlags : uint
{
    kSMPTETimeUnknown   = 0,
    /// <summary>The full time is valid.</summary>
    kSMPTETimeValid     = (1U << 0),
    /// <summary>Time is running.</summary>
    kSMPTETimeRunning   = (1U << 1)
}