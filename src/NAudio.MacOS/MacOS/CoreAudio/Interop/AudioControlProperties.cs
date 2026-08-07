// This interop definition was derived from the file AudioHardwareBase.h of the Audio Toolbox Framework.
// See https://developer.apple.com/documentation/coreaudio for more information.

#pragma warning disable IDE0055 // We want the properties to have a consistent view.

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio.Interop;

/// <summary>
/// AudioControl Property Selectors <br />
/// AudioObjectPropertySelector values provided by the AudioControl class
/// </summary>
/// <remarks>
/// The AudioControl class is a subclass of the AudioObject class. The class has
/// just the global scope, kAudioObjectPropertyScopeGlobal, and only a main
/// element.
/// </remarks>
internal static class AudioControlProperties
{
    /// <summary>
    /// An AudioServerPlugIn_PropertyScope that indicates which part of a device the control applies to.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioControlPropertyScope      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("cscp");
    /// <summary>
    /// An AudioServerPlugIn_PropertyElement that indicates which element of the device the control applies to.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioControlPropertyElement    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("celm");
}