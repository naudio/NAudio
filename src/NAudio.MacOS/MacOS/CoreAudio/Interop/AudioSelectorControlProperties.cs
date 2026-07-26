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
/// AudioSelectorControl Property Selectors
/// AudioObjectPropertySelector values provided by the AudioSelectorControl class.
/// </summary>
/// <remarks>
/// The AudioSelectorControl class is a subclass of the AudioControl class and has
/// the same scope and element structure.
/// </remarks>
internal static class AudioSelectorControlProperties
{
    /// <summary>
    /// An array of UInt32s that are the IDs of the items currently selected.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSelectorControlPropertyCurrentItem    = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("scci");
    /// <summary>
    /// An array of UInt32s that represent the IDs of all the items available.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSelectorControlPropertyAvailableItems = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("scai");
    /// <summary>
    /// This property translates the given item ID into a human readable name. The
    /// qualifier contains the ID of the item to be translated and name is returned
    /// as a CFString as the property data. The caller is responsible for releasing
    /// the returned CFObject.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSelectorControlPropertyItemName       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("scin");
    /// <summary>
    /// This property returns a UInt32 that identifies the kind of selector item the
    /// item ID refers to. The qualifier contains the ID of the item. Note that this
    /// property is optional for selector controls and that the meaning of the value
    /// depends on the specific subclass being queried.
    /// </summary>
    public static readonly AudioObjectPropertySelector kAudioSelectorControlPropertyItemKind       = (AudioObjectPropertySelector)MacUtils.ConstructUIntConstantValueFromString("clkk");
}
