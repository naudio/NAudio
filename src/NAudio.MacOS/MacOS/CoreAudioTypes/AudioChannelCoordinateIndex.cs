// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

#pragma warning disable IDE0055 // We want the values to have a consistent view.

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelCoordinateIndex <br />
/// Constants for indexing the mCoordinates array in an AudioChannelDescription structure.
/// </summary>
internal enum AudioChannelCoordinateIndex : uint
{
    /// <summary>
    /// For rectangular coordinates, negative is left and positive is right.
    /// </summary>
    kAudioChannelCoordinates_LeftRight  = 0,
    /// <summary>
    /// For rectangular coordinates, negative is back and positive is front.
    /// </summary>
    kAudioChannelCoordinates_BackFront  = 1,
    /// <summary>
    /// For rectangular coordinates, negative is below ground level, 0 is ground
    /// level, and positive is above ground level.
    /// </summary>
    kAudioChannelCoordinates_DownUp     = 2,
    /// <summary>
    /// For spherical coordinates, 0 is front center, positive is right, negative is
    /// left. This is measured in degrees.
    /// </summary>
    kAudioChannelCoordinates_Azimuth    = 0,
    /// <summary>
    /// For spherical coordinates, +90 is zenith, 0 is horizontal, -90 is nadir.
    /// This is measured in degrees.
    /// </summary>
    kAudioChannelCoordinates_Elevation  = 1,
    /// <summary>
    /// For spherical coordinates, the units are described by flags.
    /// </summary>
    kAudioChannelCoordinates_Distance   = 2
}