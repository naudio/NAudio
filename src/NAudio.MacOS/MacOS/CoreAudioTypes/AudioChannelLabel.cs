/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// AudioChannelLabel Constants <br />
/// These constants are for use in the mChannelLabel field of an
/// AudioChannelDescription structure.
/// </summary>
/// <remarks>
/// These channel labels attempt to list all labels in common use. Due to the
/// ambiguities in channel labeling by various groups, there may be some overlap or
/// duplication in the labels below. Use the label which most clearly describes what
/// you mean. <br /> <br />
/// 
/// WAVE files seem to follow the USB spec for the channel flags. A channel map lets
/// you put these channels in any order, however a WAVE file only supports labels
/// 1-18 and if present, they must be in the order given below. The integer values
/// for the labels below match the bit position of the label in the USB bitmap and
/// thus also the WAVE file bitmap.
/// </remarks>
internal enum AudioChannelLabel : uint
{
    kAudioChannelLabel_Unknown                  = 0xFFFFFFFF,   // < unknown or unspecified other use
    kAudioChannelLabel_Unused                   = 0,            // < channel is present, but has no intended use or destination
    kAudioChannelLabel_UseCoordinates           = 100,          // < channel is described by the mCoordinates fields.

    kAudioChannelLabel_Left                     = 1,
    kAudioChannelLabel_Right                    = 2,
    kAudioChannelLabel_Center                   = 3,
    kAudioChannelLabel_LFEScreen                = 4,
    kAudioChannelLabel_LeftSurround             = 5,
    kAudioChannelLabel_RightSurround            = 6,
    kAudioChannelLabel_LeftCenter               = 7,
    kAudioChannelLabel_RightCenter              = 8,
    kAudioChannelLabel_CenterSurround           = 9,            // < WAVE: "Back Center" or plain "Rear Surround"
    kAudioChannelLabel_LeftSurroundDirect       = 10,
    kAudioChannelLabel_RightSurroundDirect      = 11,
    kAudioChannelLabel_TopCenterSurround        = 12,
    kAudioChannelLabel_VerticalHeightLeft       = 13,           // < WAVE: "Top Front Left"
    kAudioChannelLabel_VerticalHeightCenter     = 14,           // < WAVE: "Top Front Center"
    kAudioChannelLabel_VerticalHeightRight      = 15,           // < WAVE: "Top Front Right"

    kAudioChannelLabel_TopBackLeft              = 16,
    kAudioChannelLabel_TopBackCenter            = 17,
    kAudioChannelLabel_TopBackRight             = 18,

    kAudioChannelLabel_RearSurroundLeft         = 33,
    kAudioChannelLabel_RearSurroundRight        = 34,
    kAudioChannelLabel_LeftWide                 = 35,
    kAudioChannelLabel_RightWide                = 36,
    kAudioChannelLabel_LFE2                     = 37,
    kAudioChannelLabel_LeftTotal                = 38,           // < matrix encoded 4 channels
    kAudioChannelLabel_RightTotal               = 39,           // < matrix encoded 4 channels
    kAudioChannelLabel_HearingImpaired          = 40,
    kAudioChannelLabel_Narration                = 41,
    kAudioChannelLabel_Mono                     = 42,
    kAudioChannelLabel_DialogCentricMix         = 43,

    kAudioChannelLabel_CenterSurroundDirect     = 44,           // < back center, non diffuse
    
    kAudioChannelLabel_Haptic                   = 45,
    
    kAudioChannelLabel_LeftTopFront             = kAudioChannelLabel_VerticalHeightLeft,
    kAudioChannelLabel_CenterTopFront           = kAudioChannelLabel_VerticalHeightCenter,
    kAudioChannelLabel_RightTopFront            = kAudioChannelLabel_VerticalHeightRight,
    kAudioChannelLabel_LeftTopMiddle            = 49,
    kAudioChannelLabel_CenterTopMiddle          = kAudioChannelLabel_TopCenterSurround,
    kAudioChannelLabel_RightTopMiddle           = 51,
    kAudioChannelLabel_LeftTopRear              = 52,
    kAudioChannelLabel_CenterTopRear            = 53,
    kAudioChannelLabel_RightTopRear             = 54,

    kAudioChannelLabel_LeftSideSurround			= 55,
    kAudioChannelLabel_RightSideSurround		= 56,
    kAudioChannelLabel_LeftBottom				= 57,
    kAudioChannelLabel_RightBottom				= 58,
    kAudioChannelLabel_CenterBottom				= 59,
    kAudioChannelLabel_LeftTopSurround			= 60,
    kAudioChannelLabel_RightTopSurround			= 61,
    kAudioChannelLabel_LFE3						= 62,
    kAudioChannelLabel_LeftBackSurround			= 63,
    kAudioChannelLabel_RightBackSurround		= 64,
    kAudioChannelLabel_LeftEdgeOfScreen			= 65,
    kAudioChannelLabel_RightEdgeOfScreen		= 66,
    
    // first order ambisonic channels
    kAudioChannelLabel_Ambisonic_W              = 200,
    kAudioChannelLabel_Ambisonic_X              = 201,
    kAudioChannelLabel_Ambisonic_Y              = 202,
    kAudioChannelLabel_Ambisonic_Z              = 203,

    // Mid/Side Recording
    kAudioChannelLabel_MS_Mid                   = 204,
    kAudioChannelLabel_MS_Side                  = 205,

    // X-Y Recording
    kAudioChannelLabel_XY_X                     = 206,
    kAudioChannelLabel_XY_Y                     = 207,
    
    // Binaural Recording
    kAudioChannelLabel_BinauralLeft             = 208,
    kAudioChannelLabel_BinauralRight            = 209,

    // other
    kAudioChannelLabel_HeadphonesLeft           = 301,
    kAudioChannelLabel_HeadphonesRight          = 302,
    kAudioChannelLabel_ClickTrack               = 304,
    kAudioChannelLabel_ForeignLanguage          = 305,

    // generic discrete channel
    kAudioChannelLabel_Discrete                 = 400,

    // numbered discrete channel
    kAudioChannelLabel_Discrete_0               = (1U<<16) | 0,
    kAudioChannelLabel_Discrete_1               = (1U<<16) | 1,
    kAudioChannelLabel_Discrete_2               = (1U<<16) | 2,
    kAudioChannelLabel_Discrete_3               = (1U<<16) | 3,
    kAudioChannelLabel_Discrete_4               = (1U<<16) | 4,
    kAudioChannelLabel_Discrete_5               = (1U<<16) | 5,
    kAudioChannelLabel_Discrete_6               = (1U<<16) | 6,
    kAudioChannelLabel_Discrete_7               = (1U<<16) | 7,
    kAudioChannelLabel_Discrete_8               = (1U<<16) | 8,
    kAudioChannelLabel_Discrete_9               = (1U<<16) | 9,
    kAudioChannelLabel_Discrete_10              = (1U<<16) | 10,
    kAudioChannelLabel_Discrete_11              = (1U<<16) | 11,
    kAudioChannelLabel_Discrete_12              = (1U<<16) | 12,
    kAudioChannelLabel_Discrete_13              = (1U<<16) | 13,
    kAudioChannelLabel_Discrete_14              = (1U<<16) | 14,
    kAudioChannelLabel_Discrete_15              = (1U<<16) | 15,
    kAudioChannelLabel_Discrete_65535           = (1U<<16) | 65535,
    
    // generic HOA ACN channel
    kAudioChannelLabel_HOA_ACN                  = 500,
    
    // numbered HOA ACN channels, SN3D normalization
    kAudioChannelLabel_HOA_ACN_0                = (2U << 16) | 0,
    kAudioChannelLabel_HOA_ACN_1                = (2U << 16) | 1,
    kAudioChannelLabel_HOA_ACN_2                = (2U << 16) | 2,
    kAudioChannelLabel_HOA_ACN_3                = (2U << 16) | 3,
    kAudioChannelLabel_HOA_ACN_4                = (2U << 16) | 4,
    kAudioChannelLabel_HOA_ACN_5                = (2U << 16) | 5,
    kAudioChannelLabel_HOA_ACN_6                = (2U << 16) | 6,
    kAudioChannelLabel_HOA_ACN_7                = (2U << 16) | 7,
    kAudioChannelLabel_HOA_ACN_8                = (2U << 16) | 8,
    kAudioChannelLabel_HOA_ACN_9                = (2U << 16) | 9,
    kAudioChannelLabel_HOA_ACN_10               = (2U << 16) | 10,
    kAudioChannelLabel_HOA_ACN_11               = (2U << 16) | 11,
    kAudioChannelLabel_HOA_ACN_12               = (2U << 16) | 12,
    kAudioChannelLabel_HOA_ACN_13               = (2U << 16) | 13,
    kAudioChannelLabel_HOA_ACN_14               = (2U << 16) | 14,
    kAudioChannelLabel_HOA_ACN_15               = (2U << 16) | 15,
    kAudioChannelLabel_HOA_ACN_65024            = (2U << 16) | 65024,    // 254th order uses 65025 channels

    // Specific SN3D alias
    kAudioChannelLabel_HOA_SN3D	                = kAudioChannelLabel_HOA_ACN_0, // Needs to be ORed with the channel index, not HOA order

    // HOA N3D
    kAudioChannelLabel_HOA_N3D                  = (3U << 16), // Needs to be ORed with the channel index, not HOA order

    // channel represents an object, ORed with channel index
    kAudioChannelLabel_Object                   = (4U << 16),

    kAudioChannelLabel_BeginReserved            = 0xF0000000,           // Channel label values in this range are reserved for internal use
    kAudioChannelLabel_EndReserved              = 0xFFFFFFFE
}