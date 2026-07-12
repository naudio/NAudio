/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

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