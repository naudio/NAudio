// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioSliderControl Property Selectors
/// AudioObjectPropertySelector values provided by the AudioSliderControl class.
/// </summary>
internal static class AudioSliderControlProperties
{
    /// <summary>
    /// A UInt32 that represents the value of the slider control.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSliderControlPropertyValue = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sdrv");
    /// <summary>
    /// An array of two UInt32s that represents the inclusive range of values the slider control can take.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSliderControlPropertyRange = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("sdrr");
}