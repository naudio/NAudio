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
/// AudioChannelFlags <br /> <br />
/// These constants are used in the mChannelFlags 
/// field of an AudioChannelDescription structure.
/// </summary>
[Flags]
internal enum AudioChannelFlags : uint
{
    kAudioChannelFlags_AllOff                   = 0,
    /// <summary>
    /// The channel is specified by the cartesian coordinates of the speaker
    /// position. This flag is mutally exclusive with
    /// kAudioChannelFlags_SphericalCoordinates.
    /// </summary>
    kAudioChannelFlags_RectangularCoordinates   = (1U<<0),
    /// <summary>
    /// The channel is specified by the spherical coordinates of the speaker
    /// position. This flag is mutally exclusive with
    /// kAudioChannelFlags_RectangularCoordinates.
    /// </summary>
    kAudioChannelFlags_SphericalCoordinates     = (1U<<1),
    /// <summary>
    /// Set to indicate the units are in meters, clear to indicate the units are
    /// relative to the unit cube or unit sphere.
    /// </summary>
    kAudioChannelFlags_Meters                   = (1U<<2)
}