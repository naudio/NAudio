// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioStereoPanControl Property Selectors
/// AudioObjectPropertySelector values provided by the AudioStereoPanControl class. 
/// </summary>
/// <remarks>
/// The AudioStereoPanControl class is a subclass of the AudioControl class and has
/// the same scope and element structure.
/// </remarks>
internal static class AudioStereoPanControlProperties
{
    /// <summary>
    /// A Float32 where 0.0 is full left, 1.0 is full right, and 0.5 is center.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStereoPanControlPropertyValue             = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("spcv");
    /// <summary>
    /// An array of two UInt32s that indicate which elements of the device the signal is being panned between.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioStereoPanControlPropertyPanningChannels   = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("spcc");
}