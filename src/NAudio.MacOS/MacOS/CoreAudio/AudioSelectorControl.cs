/*==================================================================================================
     File:       CoreAudio/AudioHardwareBase.h

     Copyright:  (c) 1985-2011 by Apple, Inc., all rights reserved.

     Bugs?:      For bug reports, consult the following page on
                 the World Wide Web:

                     http://developer.apple.com/bugreporter/

==================================================================================================*/

using System;
using NAudio.MacOS.CoreAudio.Interop;

namespace NAudio.MacOS.CoreAudio;

/// <summary>
/// The AudioSelectorControl class is an <see cref="AudioControl"/> class
/// that selection-related controls do inherit from.
/// </summary>
public class AudioSelectorControl : AudioControl
{
    internal AudioSelectorControl(AudioObjectID id) : base(id) { }

    /// <summary>
    /// An array of <see cref="uint"/>s that are the IDs of the items currently selected.
    /// </summary>
    public uint[] CurrentItem
    {
        get => GetArrayOfTPropertyValue<uint>(
            AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSelectorControlProperties.kAudioSelectorControlPropertyCurrentItem
            )
        );
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetArrayOfTPropertyValue(AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
                AudioSelectorControlProperties.kAudioSelectorControlPropertyCurrentItem
            ), value);
        }
    }

    /// <summary>
    /// An array of <see cref="uint"/>s that represent the IDs of all the items available.
    /// </summary>
    public uint[] AvailableItems => GetArrayOfTPropertyValue<uint>(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSelectorControlProperties.kAudioSelectorControlPropertyAvailableItems
        )
    );

    /// <summary>
    /// This method returns a <see cref="uint"/> that identifies the 
    /// kind of selector item the item ID refers to. <br />
    /// The <paramref name="id"/> parameter contains the ID of the item. <br />
    /// Note that this method is optional for selector controls 
    /// and that the meaning of the value depends on the 
    /// specific subclass being queried.
    /// </summary>
    /// <param name="id">The ID of the item to query it's kind.</param>
    public uint GetKind(uint id) => GetUIntPropertyValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSelectorControlProperties.kAudioSelectorControlPropertyItemKind
        ),
        [id]
    );

    /// <summary>
    /// This method translates the given item ID into a human readable name. 
    /// The qualifier contains the ID of the item to be translated and the name 
    /// is returned as a string.
    /// </summary>
    public string GetName(uint id) => GetStringPropertyValue(
        AudioObjectPropertyAddress.CreateWithGlobalScopeAndMainElement(
            AudioSelectorControlProperties.kAudioSelectorControlPropertyItemName
        ),
        [id]
    );

    /// <inheritdoc />
    public override string ToString() => $"AudioSelectorControl {{ Kind = {Kind}, Handle = 0x{GetHashCode():x2}, Scope = {Scope}, Element = {Element} }}";
}


