/*!
	@file		CoreAudioBaseTypes.h
	@framework	CoreAudioTypes.framework
	@copyright	(c) 1985-2021 by Apple, Inc., all rights reserved.
    @abstract   Definition of types common to the Core Audio APIs.
*/

#pragma warning disable IDE0055 // We want the values to have a consistent view.

namespace NAudio.MacOS.CoreAudioTypes;

/// <summary>
/// SMPTE Time Types <br />
/// Constants that describe the type of SMPTE time.
/// </summary>
internal enum SMPTETimeType : uint
{
    /// <summary>24 Frame</summary>
    kSMPTETimeType24        = 0,
    /// <summary>25 Frame</summary>
    kSMPTETimeType25        = 1,
    /// <summary>30 Drop Frame</summary>
    kSMPTETimeType30Drop    = 2,
    /// <summary>30 Frame</summary>
    kSMPTETimeType30        = 3,
    /// <summary>29.97 Frame</summary>
    kSMPTETimeType2997      = 4,
    /// <summary>29.97 Drop Frame</summary>
    kSMPTETimeType2997Drop  = 5,
    /// <summary>60 Frame</summary>
    kSMPTETimeType60        = 6,
    /// <summary>59.94 Frame</summary>
    kSMPTETimeType5994      = 7,
    /// <summary>60 Drop Frame</summary>
    kSMPTETimeType60Drop    = 8,
    /// <summary>59.94 Drop Frame</summary>
    kSMPTETimeType5994Drop  = 9,
    /// <summary>50 Frame</summary>
    kSMPTETimeType50        = 10,
    /// <summary>23.98 Frame</summary>
    kSMPTETimeType2398      = 11
}