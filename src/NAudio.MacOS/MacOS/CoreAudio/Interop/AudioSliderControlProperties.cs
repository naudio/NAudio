/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

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