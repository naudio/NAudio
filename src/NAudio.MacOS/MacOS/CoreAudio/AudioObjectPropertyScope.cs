/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using NAudio.Utils;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// An AudioObjectPropertyScope is a four char code that identifies, along with the
/// AudioObjectPropertySelector and AudioObjectPropertyElement, a specific piece of
/// information about an AudioObject.
/// </summary>
/// <remarks>
/// The scope specifies the section of the object in which to look for the property,
/// such as input, output, global, etc. 
/// Note that each class has a different set of scopes. 
/// A subclass inherits its superclass's set of scopes.
/// </remarks>
public enum AudioObjectPropertyScope : uint
{
    // mdcdi1315: NOTE: The prefix kAudioObjectPropertyScope is omitted for brevity.

    /// <summary>
    /// The wildcard value for AudioObjectPropertyScopes.
    /// </summary>
    Wildcard = 0x2a2a2a2a, // '****'
}

/// <summary>
/// Provides common constants for the <see cref="AudioObjectPropertyScope"/> enumeration.
/// </summary>
public static class AudioObjectPropertyScopeConstants
{
    /// <summary>
    /// The AudioObjectPropertyScope for properties that apply to the object as a whole. 
    /// All objects have a global scope and for most it is their only scope.
    /// </summary>
    public static readonly AudioObjectPropertyScope Global = (AudioObjectPropertyScope)MacUtils.ConstructUIntConstantValueFromString("glob");
    /// <summary>
    /// The AudioObjectPropertyScope for properties that apply to the input side of an object.
    /// </summary>
    public static readonly AudioObjectPropertyScope Input = (AudioObjectPropertyScope)MacUtils.ConstructUIntConstantValueFromString("inpt");
    /// <summary>
    /// The AudioObjectPropertyScope for properties that apply to the output side of an object.
    /// </summary>
    public static readonly AudioObjectPropertyScope Output = (AudioObjectPropertyScope)MacUtils.ConstructUIntConstantValueFromString("outp");
    /// <summary>
    /// The AudioObjectPropertyScope for properties that apply to the play through side of an object.
    /// </summary>
    public static readonly AudioObjectPropertyScope PlayThrough = (AudioObjectPropertyScope)MacUtils.ConstructUIntConstantValueFromString("ptru");
}