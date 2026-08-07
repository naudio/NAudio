// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

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
    public static readonly AudioClassID kAudioObjectClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("aobj");
    public static readonly AudioClassID kAudioSystemObjectClassID               = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("asys");
    public static readonly AudioClassID kAudioDeviceClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("adev");
    public static readonly AudioClassID kAudioClockDeviceClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("aclk");
    public static readonly AudioClassID kAudioStreamClassID                     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("astr");
    public static readonly AudioClassID kAudioControlClassID                    = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("actl");
    public static readonly AudioClassID kAudioSliderControlClassID              = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("sldr");
    public static readonly AudioClassID kAudioLevelControlClassID               = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("levl");
    public static readonly AudioClassID kAudioVolumeControlClassID              = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("vlme");
    public static readonly AudioClassID kAudioLFEVolumeControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("subv");
    public static readonly AudioClassID kAudioBooleanControlClassID             = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("togl");
    public static readonly AudioClassID kAudioMuteControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("mute");
    public static readonly AudioClassID kAudioSoloControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("solo");
    public static readonly AudioClassID kAudioJackControlClassID                = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("jack");
    public static readonly AudioClassID kAudioLFEMuteControlClassID             = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("subm");
    public static readonly AudioClassID kAudioPhantomPowerControlClassID        = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("phan");
    public static readonly AudioClassID kAudioPhaseInvertControlClassID         = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("phsi");
    public static readonly AudioClassID kAudioClipLightControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clip");
    public static readonly AudioClassID kAudioTalkbackControlClassID            = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("talb");
    public static readonly AudioClassID kAudioListenbackControlClassID          = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("lsnb");
    public static readonly AudioClassID kAudioSelectorControlClassID            = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("slct");
    public static readonly AudioClassID kAudioDataSourceControlClassID          = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("dsrc");
    public static readonly AudioClassID kAudioDataDestinationControlClassID     = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("dest");
    public static readonly AudioClassID kAudioClockSourceControlClassID         = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clck");
    public static readonly AudioClassID kAudioLineLevelControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("nlvl");
    public static readonly AudioClassID kAudioHighPassFilterControlClassID      = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("hipf");
    public static readonly AudioClassID kAudioProcessClassID                    = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("clnt");
    public static readonly AudioClassID kAudioStereoPanControlClassID           = (AudioClassID)MacUtils.ConstructUIntConstantValueFromString("span");
}