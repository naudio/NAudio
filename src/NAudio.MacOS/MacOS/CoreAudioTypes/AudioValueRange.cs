/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

using System.Runtime.InteropServices;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// This structure holds a pair of numbers that represent a continuous range of values.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioValueRange
{
    /// <summary>The minimum value.</summary>
    public readonly double mMinimum;
    /// <summary>The maximum value.</summary>
    public readonly double mMaximum;
}

