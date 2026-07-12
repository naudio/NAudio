/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

#pragma warning disable IDE0055 // We want the class IDs to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioClassIDs are used to identify the class of an AudioObject.
/// </summary>
internal enum AudioClassID : uint
{
    /// <summary>
    /// The wildcard value for AudioClassIDs.
    /// </summary>
    kAudioObjectClassIDWildcard = 0x2a2a2a2a, // '****'
}

internal static class AudioClassIDs
{
    public static readonly AudioClassID kAudioObjectClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("aobj"u8);
    public static readonly AudioClassID kAudioSystemObjectClassID               = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("asys"u8);
    public static readonly AudioClassID kAudioDeviceClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("adev"u8);
    public static readonly AudioClassID kAudioClockDeviceClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("aclk"u8);
    public static readonly AudioClassID kAudioStreamClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("astr"u8);
    public static readonly AudioClassID kAudioControlClassID                    = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("actl"u8);
    public static readonly AudioClassID kAudioSliderControlClassID              = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("sldr"u8);
    public static readonly AudioClassID kAudioLevelControlClassID               = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("levl"u8);
    public static readonly AudioClassID kAudioVolumeControlClassID              = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("vlme"u8);
    public static readonly AudioClassID kAudioLFEVolumeControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("subv"u8);
    public static readonly AudioClassID kAudioBooleanControlClassID             = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("togl"u8);
    public static readonly AudioClassID kAudioMuteControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("mute"u8);
    public static readonly AudioClassID kAudioSoloControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("solo"u8);
    public static readonly AudioClassID kAudioJackControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("jack"u8);
    public static readonly AudioClassID kAudioLFEMuteControlClassID             = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("subm"u8);
    public static readonly AudioClassID kAudioPhantomPowerControlClassID        = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("phan"u8);
    public static readonly AudioClassID kAudioPhaseInvertControlClassID         = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("phsi"u8);
    public static readonly AudioClassID kAudioClipLightControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clip"u8);
    public static readonly AudioClassID kAudioTalkbackControlClassID            = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("talb"u8);
    public static readonly AudioClassID kAudioListenbackControlClassID          = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("lsnb"u8);
    public static readonly AudioClassID kAudioSelectorControlClassID            = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("slct"u8);
    public static readonly AudioClassID kAudioDataSourceControlClassID          = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("dsrc"u8);
    public static readonly AudioClassID kAudioDataDestinationControlClassID     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("dest"u8);
    public static readonly AudioClassID kAudioClockSourceControlClassID         = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clck"u8);
    public static readonly AudioClassID kAudioLineLevelControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("nlvl"u8);
    public static readonly AudioClassID kAudioHighPassFilterControlClassID      = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("hipf"u8);
    public static readonly AudioClassID kAudioProcessClassID                    = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clnt"u8);
    public static readonly AudioClassID kAudioStereoPanControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("span"u8);
}