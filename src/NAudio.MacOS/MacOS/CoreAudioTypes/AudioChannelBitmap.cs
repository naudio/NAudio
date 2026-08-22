// This interop definition was derived from the file CoreAudioBaseTypes.h of the Core Audio Types Framework.
// See https://developer.apple.com/documentation/coreaudiotypes for more information.

#pragma warning disable IDE0055 // We want the values to have a consistent view.

using System;

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelBitmap <br />
/// These constants are for use in the mChannelBitmap
/// field of an AudioChannelLayout structure.
/// </summary>
[Flags]
internal enum AudioChannelBitmap : uint
{
    kAudioChannelBit_Left                       = (1U<<0),
    kAudioChannelBit_Right                      = (1U<<1),
    kAudioChannelBit_Center                     = (1U<<2),
    kAudioChannelBit_LFEScreen                  = (1U<<3),
    kAudioChannelBit_LeftSurround               = (1U<<4),
    kAudioChannelBit_RightSurround              = (1U<<5),
    kAudioChannelBit_LeftCenter                 = (1U<<6),
    kAudioChannelBit_RightCenter                = (1U<<7),
    kAudioChannelBit_CenterSurround             = (1U<<8),      // WAVE: "Back Center"
    kAudioChannelBit_LeftSurroundDirect         = (1U<<9),
    kAudioChannelBit_RightSurroundDirect        = (1U<<10),
    kAudioChannelBit_TopCenterSurround          = (1U<<11),
    kAudioChannelBit_VerticalHeightLeft         = (1U<<12),     // WAVE: "Top Front Left"
    kAudioChannelBit_VerticalHeightCenter       = (1U<<13),     // WAVE: "Top Front Center"
    kAudioChannelBit_VerticalHeightRight        = (1U<<14),     // WAVE: "Top Front Right"
    kAudioChannelBit_TopBackLeft                = (1U<<15),
    kAudioChannelBit_TopBackCenter              = (1U<<16),
    kAudioChannelBit_TopBackRight               = (1U<<17),
    kAudioChannelBit_LeftTopFront               = kAudioChannelBit_VerticalHeightLeft,
    kAudioChannelBit_CenterTopFront             = kAudioChannelBit_VerticalHeightCenter,
    kAudioChannelBit_RightTopFront              = kAudioChannelBit_VerticalHeightRight,
    kAudioChannelBit_LeftTopMiddle              = (1U<<21),
    kAudioChannelBit_CenterTopMiddle            = kAudioChannelBit_TopCenterSurround,
    kAudioChannelBit_RightTopMiddle             = (1U<<23),
    kAudioChannelBit_LeftTopRear                = (1U<<24),
    kAudioChannelBit_CenterTopRear              = (1U<<25),
    kAudioChannelBit_RightTopRear               = (1U<<26),
}