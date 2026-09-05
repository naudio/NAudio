// This interop definition was derived from the file AudioHardwareBase.h of the Core Audio Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioBooleanControl Property Selectors <br />
/// AudioObjectPropertySelector values provided by the AudioBooleanControl class.
/// </summary>
/// <remarks>
/// The AudioBooleanControl class is a subclass of the AudioControl class and has
/// the same scope and element structure.
/// </remarks>
internal static class AudioBooleanControlProperties
{
    /// <summary>
    /// A UInt32 where 0 means off/false and non-zero means on/true.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioBooleanControlPropertyValue = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("bcvl");
}