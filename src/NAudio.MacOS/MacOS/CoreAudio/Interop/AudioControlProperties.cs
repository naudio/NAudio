/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

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
    public static readonly AudioObjectPropertySelector kAudioControlPropertyScope      = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("cscp"u8);
    /// <summary>
    /// An AudioServerPlugIn_PropertyElement that indicates which element of the device the control applies to.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioControlPropertyElement    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("celm"u8);
}