/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the flags to have a consistent view.

using System;
using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// SMPTETime <br />
/// A structure for holding a SMPTE time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SMPTETime
{
    /// <summary>The number of subframes in the full message.</summary>
    public short          mSubframes;
    /// <summary>The number of subframes per frame (typically 80).</summary>
    public short          mSubframeDivisor;
    /// <summary>The total number of messages received.</summary>
    public UInt32         mCounter;
    /// <summary>The kind of SMPTE time using the SMPTE time type constants.</summary>
    public SMPTETimeType  mType;
    /// <summary>A set of flags that indicate the SMPTE state.</summary>
    public SMPTETimeFlags mFlags;
    /// <summary>The number of hours in the full message.</summary>
    public short          mHours;
    /// <summary>The number of minutes in the full message.</summary>
    public short          mMinutes;
    /// <summary>The number of seconds in the full message.</summary>
    public short          mSeconds;
    /// <summary>The number of frames in the full message.</summary>
    public short          mFrames;
}